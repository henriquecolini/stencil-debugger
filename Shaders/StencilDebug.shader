Shader "Hidden/Stencil Debug"
{
    Properties
    {
        _StencilRef ("Stencil ID", Float) = 0
        _Scale ("Scale", Float) = 100.0
        _Margin ("Margin", Float) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest Always

        Blend SrcAlpha OneMinusSrcAlpha

        Stencil
        {
            Ref [_StencilRef]
            Comp Equal
        }

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            int _StencilRef;
            float _Scale;
            float _Margin;

            static float4 digit_colors[10] = {
                float4(0.1, 0.1, 0.1, 0.3),
                float4(0.4, 0.7, 0.4, 1.0),
                float4(0.4, 0.4, 0.7, 1.0),
                float4(0.9, 0.9, 0.5, 1.0),
                float4(0.8, 0.5, 0.8, 0.8),
                float4(0.5, 0.8, 0.8, 0.8),
                float4(0.9, 0.6, 0.4, 0.8),
                float4(0.6, 0.4, 0.6, 0.8),
                float4(0.7, 0.4, 0.4, 0.6),
                float4(0.9, 0.9, 0.9, 0.8)
            };

            static uint bits[5] = {
                3959160828,
                2828738996,
                2881485308,
                2853333412,
                3958634981
            };

            // The draw_digit method implementation was written by Freya Holmér.
            // See the code in its original context here:
            // https://gist.github.com/FreyaHolmer/71717be9f3030c1b0990d3ed1ae833e3
            float draw_digit(int2 px, const int digit)
            {
                if (px.x < 0 || px.x > 2 || px.y < 0 || px.y > 4) {
                    return 0;
                }

                const int id = digit == -1 ? 18 : 31 - (3 * digit + px.x);
                return (bits[4 - px.y] & 1 << id) != 0;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                float aspect_ratio = _ScreenParams.y / _ScreenParams.x;

                float scale = _Scale * _ScreenParams.x / 1024.0;
                float margin = _Margin * (1.0 - scale * 0.5) * 0.02f;

                float2 scaled_texcoord = IN.texcoord * scale;
                scaled_texcoord.y *= aspect_ratio;
                scaled_texcoord = frac(scaled_texcoord);

                float2 grid_pos = scaled_texcoord * (1.0 - margin * 2.0) + margin;

                if (grid_pos.x < 0.0 || grid_pos.x > 1.0 || grid_pos.y < 0.0 || grid_pos.y > 1.0) {
                    return half4(0, 0, 0, 0);
                }

                int2 px = int2(floor(grid_pos.x * 3), floor(grid_pos.y * 5));

                float digit = draw_digit(px, _StencilRef);
                float4 color = digit_colors[_StencilRef];

                float alpha = digit * color.a;

                return float4(color.rgb * alpha, alpha);
            }
            ENDHLSL
        }
    }
}