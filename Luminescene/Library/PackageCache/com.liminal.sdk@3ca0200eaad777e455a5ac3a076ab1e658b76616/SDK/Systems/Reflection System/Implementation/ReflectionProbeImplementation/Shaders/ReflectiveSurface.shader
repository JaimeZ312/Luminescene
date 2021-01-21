// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Liminal/ReflectiveSurface"
{
    Properties{
        // normal map texture on the material,
        // default to dummy "flat surface" normalmap
        [HideInInspector]_BumpMap("Texture2D", 2D) = "bump" {}

        _Color("Color", Color) = (0.5, 0.5, 0.5, 1)
        _ColorHorizon("Horizon Color", Color) = (0.5, 0.5, 0.5, 1)

        _RampTex("Ramp", 2D) = "white" {}
        [Normal] _RippleTex("Ripple", 2D) = "white" {}
        _RippleStrength("Ripple Strength", Float) = 0.5
        _RippleSpeed("Ripple Speed", Float) = 1

        _ReflectionStrength("Reflection Strength", Float) = 1
        _Saturation("Saturation", Float) = 1

       _FadeDistance("Fade Distance", Float) = 0
        _FadeScaleX("Fade Scale X", Float) = 4


        _X("X", Float) = 0
        _Y("Y", Float) = 0
        _Z("Z", Float) = 0

    }
        SubShader
        {
        Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
        LOD 100

        Blend One One
        Cull Off
        ZWrite Off

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct v2f {
                    float3 worldPos : TEXCOORD0;
                    // these three vectors will hold a 3x3 rotation matrix
                    // that transforms from tangent to world space
                    half3 tspace0 : TEXCOORD1; // tangent.x, bitangent.x, normal.x
                    half3 tspace1 : TEXCOORD2; // tangent.y, bitangent.y, normal.y
                    half3 tspace2 : TEXCOORD3; // tangent.z, bitangent.z, normal.z
                    // texture coordinate for the normal map
                    float2 uv : TEXCOORD4;
                    float4 pos : SV_POSITION;

                    float2 uvRipple : TEXCOORD5;
                    float2 uvRamp : TEXCOORD6;

                };


        fixed4 _Color;
        fixed4 _ColorHorizon;

        // normal map texture from shader properties
        sampler2D _BumpMap, _RippleTex, _RampTex;

        float4 _RippleTex_ST, _RampTex_ST;
        float _ReflectionStrength, _Saturation, _FadeScaleX, _FadeDistance, _RippleStrength, _RippleSpeed;
        float _X, _Y, _Z;

                // vertex shader now also needs a per-vertex tangent vector.
                // in Unity tangents are 4D vectors, with the .w component used to
                // indicate direction of the bitangent vector.
                // we also need the texture coordinate.
                v2f vert(float4 vertex : POSITION, float3 normal : NORMAL, float4 tangent : TANGENT, float2 uv : TEXCOORD0)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(vertex);
                    o.worldPos = mul(unity_ObjectToWorld, vertex).xyz;
                    
                    half3 wNormal = UnityObjectToWorldNormal(normal);
                    half3 wTangent = UnityObjectToWorldDir(tangent.xyz);
                    // compute bitangent from cross product of normal and tangent
                    half tangentSign = tangent.w * unity_WorldTransformParams.w;
                    half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
                    // output the tangent space matrix
                    o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
                    o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
                    o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);

                    o.uv = uv;
                    
                    o.uvRipple = TRANSFORM_TEX(uv, _RippleTex);

                    return o;
                }



            fixed4 frag(v2f i) : SV_Target
            {
            // sample the normal map, and decode from the Unity encoding
            half3 tnormal = UnpackNormal(tex2D(_BumpMap, i.uv));

            // transform normal from tangent to world space
            half3 worldNormal;
            worldNormal.x = dot(i.tspace0, tnormal);
            worldNormal.y = dot(i.tspace1, tnormal);
            worldNormal.z = dot(i.tspace2, tnormal);

            // rest the same as in previous shader
            half3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
            
            worldViewDir.x += _X;
            worldViewDir.y += _Y;
            worldViewDir.z += _Z;

            // Ripple
            float2 uvr = i.uvRipple + float2(0, _Time.x * _RippleSpeed);
            fixed3 nrm = UnpackNormal(tex2D(_RippleTex, uvr));
            worldViewDir.xy += nrm.r * _RippleStrength;
            
            half3 worldRefl = reflect(-worldViewDir, worldNormal);

            half4 skyData = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, worldRefl);

            half3 skyColor = DecodeHDR(skyData, unity_SpecCube0_HDR);
            fixed4 c = float4(1, 1, 1, 1);
            c.rgb = skyColor.rgb;
            
            fixed4 main = _Color;
            fixed4 refl = c * _ReflectionStrength;

            // Fade reflection over distance
            fixed2 delta = _WorldSpaceCameraPos.xz - i.worldPos.xz;
            fixed dist = length(fixed2(delta.x * _FadeScaleX, delta.y));
            refl *= saturate(pow(dist / _FadeDistance, 4));
            refl = saturate(refl - Luminance(main) / 6);



            fixed4 col = saturate(main + refl);

            fixed4 ramp = tex2D(_RampTex, i.uv);
            col = lerp(col, _ColorHorizon, 1 - ramp);


            return col;


            //c.rgb *= _Saturation;
            //c.rgb = lerp(_Color.rgb, c.rgb, _ReflectionStrength);
            //c *= _Color;

            // Fade reflection over distance
            //fixed2 delta = _WorldSpaceCameraPos.xz - i.worldPos.xz;
            //fixed dist = length(fixed2(delta.x * _FadeScaleX, delta.y));
            //c.rgb *= saturate(pow(dist / _FadeDistance, 4));



            return c;
        }
        ENDCG
    }
        }
}