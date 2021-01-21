using Liminal.Systems;

namespace App
{
    using UnityEngine;
    using System.Collections;

    // This is in fact just the Water script from Pro Standard Assets,
    // just with refraction stuff removed.

    public class MirrorReflection : MonoBehaviour
    {
        public Renderer m_Renderer;
        public bool m_DisablePixelLights = true;
        public int m_TextureSize = 256;
        public float m_ClipPlaneOffset = 0.07f;

        public Vector3 Offset;

        public LayerMask m_ReflectLayers = -1;

        private Hashtable m_ReflectionCameras = new Hashtable(); // Camera -> Camera table

        private RenderTexture m_ReflectionTexture = null;
        private int m_OldReflectionTextureSize = 0;

        private static bool s_InsideRendering = false;
        private static readonly int s_offsetEnabled = Shader.PropertyToID("_OffsetEnabled");

#if SMOOTH_CAM
        public Camera Cam => GameObject.Find("SmoothCam").GetComponent<Camera>();
#else
        public Camera Cam => Camera.main;
#endif

        public ReflectionOffsetModel Ipd58OffsetModel = new ReflectionOffsetModel(1.194927f, -0.186721f, 0.8499745f);
        public ReflectionOffsetModel Ipd63OffsetModel = new ReflectionOffsetModel(1.077431f, -0.07495025f, 0.9323733f);
        public ReflectionOffsetModel Ipd68OffsetModel = new ReflectionOffsetModel(1.044061f, -0.006401608f, 1.043458f, -0.03977372f);

        public static ReflectionOffsetModel IpdModel = null;

        private void Awake()
        {
            if (IpdModel != null)
                return;

            if (OVRPlugin.ipd >= 0.055f && OVRPlugin.ipd < 0.062f)
                IpdModel = Ipd58OffsetModel;

            if (OVRPlugin.ipd >= 0.062f && OVRPlugin.ipd < 0.067f)
                IpdModel = Ipd63OffsetModel;

            if (OVRPlugin.ipd >= 0.067f)
                IpdModel = Ipd68OffsetModel;
        }

        private void Start()
        {
            m_Renderer = GetComponent<Renderer>();

            m_Renderer.material.SetFloat("_Quest", 0);
            m_Renderer.material.SetFloat("_Rift", 0);
            m_Renderer.material.SetFloat("_RiftS", 0);
            m_Renderer.material.SetFloat("_Vive", 0);
            m_Renderer.material.SetFloat("_VivePro", 0);

#if UNITY_ANDROID
            m_Renderer.material.SetFloat(s_offsetEnabled, OVRUtils.IsOculusQuest ? 1 : 0);
            m_Renderer.material.SetFloat("_Quest", OVRUtils.IsOculusQuest ? 1 : 0);
#endif
            var model = XRDeviceUtils.GetDeviceModelType();

#if UNITY_STANDALONE
            m_Renderer.material.SetFloat(s_offsetEnabled, 1);

            switch (model)
            {
                case EDeviceModelType.Rift:
                    m_Renderer.material.SetFloat("_Rift", 1);
                    break;
                case EDeviceModelType.RiftS:
                    m_Renderer.material.SetFloat("_RiftS", 1);
                    break;
                case EDeviceModelType.HtcVive:
                    m_Renderer.material.SetFloat("_Vive", 1);
                    break;
                case EDeviceModelType.HtcVivePro:
                    m_Renderer.material.SetFloat("_VivePro", 1);
                    break;
                case EDeviceModelType.Quest:
                    m_Renderer.material.SetFloat("_Quest", 1);
                    break;
            }
#endif

            if (model == EDeviceModelType.Quest2)
            {
                m_Renderer.material.SetFloat("_Quest", 0);
                m_Renderer.material.SetFloat(s_offsetEnabled, 1);
                m_Renderer.material.SetFloat("_Debug", 1);

                SetMaterial(IpdModel);
            }

            void SetMaterial(ReflectionOffsetModel m)
            {
                m_Renderer.material.SetFloat("_OffsetRX", m.RX);
                m_Renderer.material.SetFloat("_OffsetRY", m.RY);
                m_Renderer.material.SetFloat("_OffsetRZ", m.RZ);
                m_Renderer.material.SetFloat("_OffsetRW", m.RW);
                m_Renderer.material.SetFloat("_OffsetX", m.LOffset);

                m_Renderer.material.SetFloat("_UseL", m.UseL ? 1 : 0);
                m_Renderer.material.SetFloat("_OffsetLX", m.LX);
                m_Renderer.material.SetFloat("_OffsetLZ", m.LZ);
            }
        }

        // This is called when it's known that the object will be rendered by some
        // camera. We render reflections and do other updates here.
        // Because the script executes in edit mode, reflections for the scene view
        // camera will just work!
        public void OnWillRenderObject()
        {
            if(m_Renderer == null)
                return;

            if (!enabled || !m_Renderer || !m_Renderer.sharedMaterial || !m_Renderer.enabled)
                return;

            Camera cam = Cam;
            if (!cam)
                return;

            // Safeguard from recursive reflections.
            if (s_InsideRendering)
                return;
            s_InsideRendering = true;

            Camera reflectionCamera;
            CreateMirrorObjects(cam, out reflectionCamera);

            // find out the reflection plane: position and normal in world space
            Vector3 pos = transform.position;
            Vector3 normal = transform.up + Offset;

            // Optionally disable pixel lights for reflection
            int oldPixelLightCount = QualitySettings.pixelLightCount;
            if (m_DisablePixelLights)
                QualitySettings.pixelLightCount = 0;

            UpdateCameraModes(cam, reflectionCamera);

            // Render reflection
            // Reflect camera around reflection plane
            float d = -Vector3.Dot(normal, pos) - m_ClipPlaneOffset;
            Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

            Matrix4x4 reflection = Matrix4x4.zero;
            CalculateReflectionMatrix(ref reflection, reflectionPlane);
            Vector3 oldpos = cam.transform.position;
            Vector3 newpos = reflection.MultiplyPoint(oldpos);
            reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;

            // Setup oblique projection matrix so that near plane is our reflection
            // plane. This way we clip everything below/above it for free.
            Vector4 clipPlane = CameraSpacePlane(reflectionCamera, pos, normal, 1.0f);
            //Matrix4x4 projection = cam.projectionMatrix;
            Matrix4x4 projection = cam.CalculateObliqueMatrix(clipPlane);
            reflectionCamera.projectionMatrix = projection;

            reflectionCamera.cullingMask = ~(1 << 4) & m_ReflectLayers.value; // never render water layer
            reflectionCamera.targetTexture = m_ReflectionTexture;
            GL.invertCulling = true;
            reflectionCamera.transform.position = newpos;
            Vector3 euler = cam.transform.eulerAngles;
            reflectionCamera.transform.eulerAngles = new Vector3(0, euler.y, euler.z);
            reflectionCamera.Render();
            reflectionCamera.transform.position = oldpos;
            GL.invertCulling = false;
            Material[] materials = m_Renderer.sharedMaterials;
            foreach (Material mat in materials)
            {
                if (mat.HasProperty("_ReflectionTex"))
                    mat.SetTexture("_ReflectionTex", m_ReflectionTexture);
            }

            // Restore pixel light count
            if (m_DisablePixelLights)
                QualitySettings.pixelLightCount = oldPixelLightCount;

            s_InsideRendering = false;
        }


        // Cleanup all the objects we possibly have created
        void OnDisable()
        {
            if (m_ReflectionTexture)
            {
                DestroyImmediate(m_ReflectionTexture);
                m_ReflectionTexture = null;
            }

            try
            {
                foreach (DictionaryEntry kvp in m_ReflectionCameras)
                    DestroyImmediate(((Camera) kvp.Value).gameObject);
            }
            catch 
            { 
                Debug.Log("Caught reflection camera not destroying properly");
            }

            m_ReflectionCameras.Clear();

        }


        private void UpdateCameraModes(Camera src, Camera dest)
        {
            if (dest == null)
                return;
            // set camera to clear the same way as current camera
            dest.clearFlags = src.clearFlags;
            dest.backgroundColor = src.backgroundColor;
            if (src.clearFlags == CameraClearFlags.Skybox)
            {
                Skybox sky = src.GetComponent(typeof(Skybox)) as Skybox;
                Skybox mysky = dest.GetComponent(typeof(Skybox)) as Skybox;
                if (!sky || !sky.material)
                {
                    mysky.enabled = false;
                }
                else
                {
                    mysky.enabled = true;
                    mysky.material = sky.material;
                }
            }
            // update other values to match current camera.
            // even if we are supplying custom camera&projection matrices,
            // some of values are used elsewhere (e.g. skybox uses far plane)
            dest.farClipPlane = src.farClipPlane;
            dest.nearClipPlane = src.nearClipPlane;
            dest.orthographic = src.orthographic;
            dest.fieldOfView = src.fieldOfView;
            dest.aspect = src.aspect;
            dest.orthographicSize = src.orthographicSize;
        }

        // On-demand create any objects we need
        private void CreateMirrorObjects(Camera currentCamera, out Camera reflectionCamera)
        {
            reflectionCamera = null;

            // Reflection render texture
            if (!m_ReflectionTexture || m_OldReflectionTextureSize != m_TextureSize)
            {
                if (m_ReflectionTexture)
                    DestroyImmediate(m_ReflectionTexture);
                m_ReflectionTexture = new RenderTexture(m_TextureSize, m_TextureSize, 16);
                m_ReflectionTexture.name = "__MirrorReflection" + GetInstanceID();
                m_ReflectionTexture.isPowerOfTwo = true;
                m_ReflectionTexture.hideFlags = HideFlags.DontSave;
                m_OldReflectionTextureSize = m_TextureSize;
            }

            // Camera for reflection
            reflectionCamera = m_ReflectionCameras[currentCamera] as Camera;
            if (!reflectionCamera) // catch both not-in-dictionary and in-dictionary-but-deleted-GO
            {
                GameObject go = new GameObject("Mirror Refl Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(Camera), typeof(Skybox));
                reflectionCamera = go.GetComponent<Camera>();
                reflectionCamera.enabled = false;
                reflectionCamera.transform.position = transform.position;
                reflectionCamera.transform.rotation = transform.rotation;
                reflectionCamera.gameObject.AddComponent<FlareLayer>();
                go.hideFlags = HideFlags.HideAndDontSave;
                m_ReflectionCameras[currentCamera] = reflectionCamera;
            }
        }

        // Extended sign: returns -1, 0 or 1 based on sign of a
        private static float sgn(float a)
        {
            if (a > 0.0f) return 1.0f;
            if (a < 0.0f) return -1.0f;
            return 0.0f;
        }

        // Given position/normal of the plane, calculates plane in camera space.
        private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
        {
            Vector3 offsetPos = pos + normal * m_ClipPlaneOffset;
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3 cpos = m.MultiplyPoint(offsetPos);
            Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }

        // Calculates reflection matrix around the given plane
        private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
        {
            reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
            reflectionMat.m01 = (-2F * plane[0] * plane[1]);
            reflectionMat.m02 = (-2F * plane[0] * plane[2]);
            reflectionMat.m03 = (-2F * plane[3] * plane[0]);

            reflectionMat.m10 = (-2F * plane[1] * plane[0]);
            reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
            reflectionMat.m12 = (-2F * plane[1] * plane[2]);
            reflectionMat.m13 = (-2F * plane[3] * plane[1]);

            reflectionMat.m20 = (-2F * plane[2] * plane[0]);
            reflectionMat.m21 = (-2F * plane[2] * plane[1]);
            reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
            reflectionMat.m23 = (-2F * plane[3] * plane[2]);

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;
        }
    }
}