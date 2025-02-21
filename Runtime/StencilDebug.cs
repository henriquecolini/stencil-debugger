using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StencilDebugger
{
    [ExcludeFromPreset]
    [DisallowMultipleRendererFeature("Stencil Debug")]
#if UNITY_6000_0_OR_NEWER
    [SupportedOnRenderer(typeof(UniversalRendererData))]
#endif
    [Tooltip("Stencil Debug visualizes the stencil buffer in your scene view.")]
    [HelpURL("https://github.com/alexanderameye/stencil-debugger")]
    public class StencilDebug : ScriptableRendererFeature
    {
        private class StencilDebugPass : ScriptableRenderPass
        {
            private ComputeShader debug;
            private int debugKernel;
            private float scale, margin;
            private readonly ProfilingSampler debugSampler = new(nameof(StencilDebugPass));

            private static int DivRoundUp(int x, int y) => (x + y - 1) / y;

            public void Setup(ComputeShader debugShader, float debugScale, float debugMargin)
            {
                debug = debugShader;
                scale = debugScale;
                margin = debugMargin;

                debugKernel = debug.FindKernel("StencilDebug");
            }
            
#if UNITY_6000_0_OR_NEWER
            private class PassData
            {

            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                
                TextureHandle colorHandle;
                TextureHandle stencilHandle;
                TextureHandle debugHandle;

                // 1. Generate.
                // -> Generate stencil texture.
                using (var builder = renderGraph.AddComputePass<PassData>(ShaderPassName.Generate, out _, profilingSampler))
                {
                    colorHandle = resourceData.activeColorTexture;
                    stencilHandle = resourceData.activeDepthTexture;

                    var desc = new TextureDesc(cameraData.cameraTargetDescriptor)
                    {
                        name = Buffer.StencilDebug,
                        colorFormat = cameraData.cameraTargetDescriptor.graphicsFormat,
                        enableRandomWrite = true
                    };
                    debugHandle = renderGraph.CreateTexture(new TextureDesc(desc));

                    builder.UseTexture(colorHandle);
                    builder.UseTexture(stencilHandle);
                    builder.UseTexture(debugHandle, AccessFlags.ReadWrite);

                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(false);

                    builder.SetRenderFunc((PassData _, ComputeGraphContext context) =>
                    {
                        var cmd = context.cmd;

                        cmd.SetComputeFloatParam(debug, ShaderPropertyId.Scale, scale);
                        cmd.SetComputeFloatParam(debug, ShaderPropertyId.Margin, margin);

                        cmd.SetComputeTextureParam(debug, debugKernel, Buffer.CameraColor, colorHandle, 0);
                        cmd.SetComputeTextureParam(debug, debugKernel, Buffer.Stencil, stencilHandle, 0, RenderTextureSubElement.Stencil);
                        cmd.SetComputeTextureParam(debug, debugKernel, Buffer.StencilDebug, debugHandle);

                        cmd.DispatchCompute(debug, debugKernel, DivRoundUp(cameraData.scaledWidth, 8), DivRoundUp(cameraData.scaledHeight, 8), 1);
                    });
                }

                // 2. Compose.
                // -> Compose stencil texture with scene.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(ShaderPassName.Compose, out _, profilingSampler))
                {
                    builder.UseTexture(debugHandle);
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(false);

                    builder.SetRenderFunc((PassData _, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, debugHandle, new Vector4(1, 1, 0, 0), 0, false);
                    });
                }
            }
#endif
            private RTHandle cameraDepthRTHandle;
            private RTHandle debugRTHandle;

            #pragma warning disable 618, 672
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.msaaSamples = 1;
                desc.depthStencilFormat = GraphicsFormat.None;
                desc.enableRandomWrite = true;

                RenderingUtils.ReAllocateHandleIfNeeded(ref debugRTHandle, desc);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get();

                using (new ProfilingScope(cmd, debugSampler))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                
                    var colorHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
                    var stencilHandle = cameraDepthRTHandle;
                    var debugHandle = debugRTHandle;

                    cmd.SetComputeFloatParam(debug, ShaderPropertyId.Scale, scale);
                    cmd.SetComputeFloatParam(debug, ShaderPropertyId.Margin, margin);

                    cmd.SetComputeTextureParam(debug, debugKernel, Buffer.CameraColor, colorHandle, 0);
                    cmd.SetComputeTextureParam(debug, debugKernel, Buffer.Stencil, stencilHandle, 0, RenderTextureSubElement.Stencil);
                    cmd.SetComputeTextureParam(debug, debugKernel, Buffer.StencilDebug, debugHandle);

                    cmd.DispatchCompute(debug, debugKernel, DivRoundUp(renderingData.cameraData.cameraTargetDescriptor.width, 8), DivRoundUp(renderingData.cameraData.cameraTargetDescriptor.height, 8), 1);

                    Blitter.BlitTexture(cmd, debugHandle, new Vector4(1, 1, 0, 0), 0, false);
                }
                
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
            #pragma warning restore 618, 672
            
            public void SetTarget(RTHandle depth)
            {
                cameraDepthRTHandle = depth;
            }
            
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (cmd == null)
                {
                    throw new ArgumentNullException(nameof(cmd));
                }

                cameraDepthRTHandle = null;
            }

            public void Dispose()
            {
                debugRTHandle?.Release();
            }
        }

        private ComputeShader shader;
        [SerializeField] private RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingOpaques;
        [SerializeField] private bool showInSceneView = true;
        [SerializeField] [Range(0.0f, 100.0f)] private float scale = 40.0f;
        [SerializeField] [Range(0.0f, 1.0f)] private float margin = 0.2f;
        private StencilDebugPass stencilDebugPass;

        /// <summary>
        /// Called
        /// - When the Scriptable Renderer Feature loads the first time.
        /// - When you enable or disable the Scriptable Renderer Feature.
        /// - When you change a property in the Inspector window of the Renderer Feature.
        /// </summary>
        public override void Create()
        {
#if UNITY_EDITOR
            var shaderPath = AssetDatabase.GUIDToAssetPath(ShaderPath.DebugGuid);
            shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(shaderPath);
#else
            shader = null;
#endif
            stencilDebugPass ??= new StencilDebugPass();
        }

        /// <summary>
        /// Called
        /// - Every frame, once for each camera.
        /// </summary>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // Don't render for some views.
            if (renderingData.cameraData.cameraType == CameraType.Preview
                || renderingData.cameraData.cameraType == CameraType.Reflection
                || renderingData.cameraData.cameraType == CameraType.SceneView && !showInSceneView
#if UNITY_6000_0_OR_NEWER
                || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
#else
                )
#endif
                return;

            if (shader == null)
            {
                Debug.LogWarning("A required compute shader could not be loaded. Stencil Debug will not render.");
                return;
            }

            stencilDebugPass.Setup(shader, scale, margin);
            stencilDebugPass.renderPassEvent = injectionPoint;
            renderer.EnqueuePass(stencilDebugPass);
        }
        
        #pragma warning disable 618, 672
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            stencilDebugPass.ConfigureInput(ScriptableRenderPassInput.Color);
            stencilDebugPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            stencilDebugPass.SetTarget(renderer.cameraDepthTargetHandle);
        }
        #pragma warning restore 618, 672

        /// <summary>
        /// Clean up resources allocated to the Scriptable Renderer Feature such as materials.
        /// </summary>
        override protected void Dispose(bool disposing)
        {
            stencilDebugPass?.Dispose();
            stencilDebugPass = null;
        }

        private void OnDestroy()
        {
            stencilDebugPass?.Dispose();
        }
    }
}