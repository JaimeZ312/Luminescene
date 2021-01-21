Shader "FX/MirrorReflection"
{
    Properties
    {
        [HideInInspector] _ReflectionTex("", 2D) = "white" {}

        _RampTex("Ramp", 2D) = "white" {}
        _Color("Color", Color) = (0.5, 0.5, 0.5, 1)
        _ColorHorizon("Horizon Color", Color) = (0.5, 0.5, 0.5, 1)
        [Normal] _RippleTex("Ripple", 2D) = "white" {}
        _RippleStrength("Ripple Strength", Float) = 0.5
        _RippleSpeed("Ripple Speed", Float) = 0
        _ReflectionStrength("Reflection Strength", Float) = 0.5
        _FadeDistance("Fade Distance", Float) = 0
        _FadeScaleX("Fade Scale X", Float) = 4

         MySrcMode("SrcMode", Float) = 1
         MyDstMode("DstMode", Float) = 1

        [MaterialToggle] _EnableTint("_EnableTint", Float) = 0
        [MaterialToggle] _EnableRampAlpha("_EnableRampAlpha", Float) = 1

        [MaterialToggle] _OffsetEnabled("Offset Enabled", Float) = 0

        [MaterialToggle] _Quest("Quest", Float) = 0
        [MaterialToggle] _Rift("Rift", Float) = 0
        [MaterialToggle] _Debug("Debug", Float) = 0
        [MaterialToggle] _RiftS("RiftS", Float) = 0
        [MaterialToggle] _Vive("Vive", Float) = 0
        [MaterialToggle] _VivePro("VivePro", Float) = 0

         _OffsetRX("RX", Float) = 1.17
         _OffsetRY("RY", Float) = 1
         _OffsetRZ("RZ", Float) = -0.173
         _OffsetRW("RW", Float) = 0
         _OffsetX("Offset", Float) =  0.85

         [MaterialToggle] _UseL("UseL", Float) = 0
         _OffsetLX("LX", Float) = 1.043458
         _OffsetLZ("LZ", Float) = -0.03977372
    }
        SubShader
        {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Blend[MySrcMode][MyDstMode]
        Cull Off
        ZWrite Off

            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"
                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 refl : TEXCOORD1;
                    float2 uvRipple : TEXCOORD2;
                    float4 screenPos : TEXCOORD3;
                    float4 worldPos : TEXCOORD4;
                    float4 pos : SV_POSITION;
                    float2 uvRamp : TEXCOORD5;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;

                sampler2D _ReflectionTex;

                sampler2D _RampTex;
                float4 _RampTex_ST;

                sampler2D _RippleTex;
                float4 _RippleTex_ST;

                fixed4 _Color;
                fixed4 _ColorHorizon;

                fixed _RippleSpeed;
                fixed _RippleStrength;
                fixed _ReflectionStrength;
                fixed _FadeDistance;
                fixed _FadeScaleX;

                float _OffsetRX;
                float _OffsetRY;
                float _OffsetRZ;
                float _OffsetRW;
                float _OffsetLX;
                float _OffsetLZ;

                float _OffsetX;
                float _OffsetEnabled;

                float _Rift;
                float _Quest;
                float _Debug;
                float _RiftS;
                float _Vive;
                float _VivePro;
                float _UseL;

                float _EnableTint;
                float _EnableRampAlpha;

                struct appdata
                {
                    float4 pos : POSITION;
                    float2 uv : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                v2f vert(appdata v)
                {
                    float4 pos = v.pos;
                    float2 uv = v.uv;

                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_OUTPUT(v2f, o);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                    o.pos = UnityObjectToClipPos(pos);
                    o.uv = TRANSFORM_TEX(uv, _MainTex);
                    o.uvRipple = TRANSFORM_TEX(uv, _RippleTex);
                    o.refl = ComputeScreenPos(o.pos);
                    o.uvRamp = TRANSFORM_TEX(uv, _RampTex);

                    o.screenPos = ComputeNonStereoScreenPos(o.pos);
                    o.worldPos = mul(unity_ObjectToWorld, pos);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                    fixed4 main = _Color;
                    fixed4 ramp = tex2D(_RampTex, i.uvRamp);

                    // Ripple
                    float2 uvr = i.uvRipple + float2(0, _Time.x * _RippleSpeed);
                    fixed3 nrm = UnpackNormal(tex2D(_RippleTex, uvr));
                    i.screenPos.xy += nrm.r * _RippleStrength;

                    float2 uvRefl = i.screenPos.xy / i.screenPos.w;

					// Remove this on PC
					if(_OffsetEnabled > 0)
                    {
                        if (unity_StereoEyeIndex == 0) {
                            if(_Debug)
                            {
                                if (_UseL) 
                                {
                                    float4 scaleOffset = float4(_OffsetLX, _OffsetRY, _OffsetLZ, _OffsetRW);
                                    uvRefl = (uvRefl - scaleOffset.zw) / scaleOffset.xy;
                                }
                                else 
                                {
                                    uvRefl.x *= _OffsetX;
                                }
                            }
                            else
                            {
                                if(_Rift == 1)
                                {
                                    float4 scaleOffset = float4(1.15, _OffsetRY, 0, _OffsetRW);
                                    uvRefl = (uvRefl - scaleOffset.zw) / scaleOffset.xy;
                                }

                                if(_Quest == 1)
                                {
                                    uvRefl.x *= 0.85;
                                }

                                if(_RiftS)
                                {
                                    uvRefl.x *= 1.03;
                                }

                                if (_Vive)
                                {
                                    uvRefl.x *= 0.94;
                                }

                                if (_VivePro)
                                {
                                    uvRefl.x *= 0.94;
                                }
                            }
                        }
                        else
                        {
                            if(_Debug)
                            {
                                    float4 scaleOffset = float4(_OffsetRX, _OffsetRY, _OffsetRZ, _OffsetRW);
                                    uvRefl = (uvRefl - scaleOffset.zw) / scaleOffset.xy;
                            }else{
                                if(_Rift == 1)
                                {
                                    float4 scaleOffset = float4(1.15, _OffsetRY, -0.155, _OffsetRW);
                                    uvRefl = (uvRefl - scaleOffset.zw) / scaleOffset.xy;
                                }

                                if(_Quest == 1)
                                {
                                    float4 scaleOffset = float4(_OffsetRX, _OffsetRY, _OffsetRZ, _OffsetRW);
                                    uvRefl = (uvRefl - scaleOffset.zw) / scaleOffset.xy;
                                }

                                if(_RiftS)
                                {
                                    float4 scaleOffset = float4(1, _OffsetRY, 0.02, _OffsetRW);
                                    uvRefl = (uvRefl - scaleOffset.zw) / scaleOffset.xy;
                                }

                                if (_Vive)
                                {
                                    float4 scaleOffset = float4(1, 1, -0.03, 0);
                                    uvRefl = (uvRefl - scaleOffset.zw) / scaleOffset.xy;
                                }

                                if (_VivePro)
                                {
                                    float4 scaleOffset = float4(1.05, 1, -0.056, 0);
                                    uvRefl = (uvRefl - scaleOffset.zw) / scaleOffset.xy;
                                }
                            }
                        }
                    }
					
                    fixed4 refl = tex2D(_ReflectionTex, uvRefl)* _ReflectionStrength;

                    // Fade reflection over distance
                    fixed2 delta = _WorldSpaceCameraPos.xz - i.worldPos.xz;
                    fixed dist = length(fixed2(delta.x * _FadeScaleX, delta.y));
                    refl *= saturate(pow(dist / _FadeDistance, 4));
                    refl = saturate(refl - Luminance(main) / 6);

                    fixed4 col = saturate(main + refl);

                    // Apply ramp to fade toward horizon color
                    col = lerp(col, _ColorHorizon, 1 - ramp);
                    
                    if(_EnableTint)
                        col *= _Color;

                    if(_EnableRampAlpha)
                        col.a = lerp(1,0, 1 - ramp);
                    
                    return col;
                }
                ENDCG
            }
        }
}