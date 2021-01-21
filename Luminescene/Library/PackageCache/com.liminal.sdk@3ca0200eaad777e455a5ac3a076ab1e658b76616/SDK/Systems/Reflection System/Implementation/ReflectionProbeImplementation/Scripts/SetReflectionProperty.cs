using UnityEngine;

public class SetReflectionProperty : MonoBehaviour
{
    private Material _mat;
    public Transform Origin;

    public float RippleStrength = 0.5f;
    public float RippleSpeed = 1;
    public float ReflectionStrength = 1;
    public float Saturation = 1;
    public float FadeDistance = 0;
    public float FadeScaleX = 0;

    public float X;
    public float Y;
    public float Z;

    public float M = 1;
    public float YM = 1;

    public Transform Cam;
    public Vector3 Delta;

    private void Awake()
    {
        var renderer = GetComponent<Renderer>();
        _mat = renderer.material;

        _mat.SetFloat("_RippleStrength", RippleStrength);
        _mat.SetFloat("_RippleSpeed", RippleSpeed);
        _mat.SetFloat("_ReflectionStrength", ReflectionStrength);
        _mat.SetFloat("_Saturation", Saturation);
        _mat.SetFloat("_FadeDistance", FadeDistance);
        _mat.SetFloat("_FadeScaleX", FadeScaleX);
    }

    private void LateUpdate()
    {
        if (Cam == null)
            Cam = Camera.main.transform;

        Delta = Cam.transform.position - Origin.position;

        X = Delta.x * M;
        Y = Delta.y * YM;

        _mat.SetFloat("_X", X);
        _mat.SetFloat("_Y", Y);
        _mat.SetFloat("_Z", Z);
    }
}