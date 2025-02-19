using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif
using UnityEngine.Rendering.Universal;

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
            private Material debug;
            private float scale, margin;
            private static Material[] _mats;
            private readonly ProfilingSampler debugSampler = new(nameof(StencilDebugPass));

            public void Setup(ref Material debugMaterial, float debugScale, float debugMargin)
            {
                debug = debugMaterial;
                scale = debugScale;
                margin = debugMargin;

                debug.SetFloat(ShaderPropertyId.Scale, scale);
                debug.SetFloat(ShaderPropertyId.Margin, margin);

                if (_mats is not {Length: 10})
                {
                    _mats = new Material[10];
                }

                for (var i = 0; i < 10; i++)
                {
                    DestroyImmediate(_mats[i]);
                    _mats[i] = Instantiate(debug);
                    var overrideMaterial = _mats[i];
                    overrideMaterial.CopyPropertiesFromMaterial(debug);
                    overrideMaterial.SetFloat(ShaderPropertyId.Scale, scale);
                    overrideMaterial.SetFloat(ShaderPropertyId.Margin, margin);
                    overrideMaterial.SetFloat(ShaderPropertyId.StencilRef, i);
                }
            }
            
#if UNITY_6000_0_OR_NEWER
            private class PassData
            {

            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var mpb = new MaterialPropertyBlock();

                var resourceData = frameData.Get<UniversalResourceData>();

                if (_mats.Length != 10) return;

                using var builder = renderGraph.AddRasterRenderPass<PassData>("Debug Stencil", out _, profilingSampler);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);

                builder.SetRenderFunc((PassData _, RasterGraphContext context) =>
                {
                    for (var stencilValue = 0; stencilValue < 10; stencilValue++)
                    {
                        mpb.Clear();
                        mpb.SetVector(Shader.PropertyToID("_BlitScaleBias"), new Vector4(1, 1, 0, 0));

                        context.cmd.DrawProcedural(Matrix4x4.identity, _mats[stencilValue], 0, MeshTopology.Triangles, 3, 1, mpb);
                    }
                });
            }
#endif
            private RTHandle cameraDepthRTHandle;
            
            #pragma warning disable 618, 672
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var mpb = new MaterialPropertyBlock();
                
                if (_mats.Length != 10) return;
                
                var cmd = CommandBufferPool.Get();

                using (new ProfilingScope(cmd, debugSampler))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    
                    CoreUtils.SetRenderTarget(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, cameraDepthRTHandle); 
                    
                    for (var stencilValue = 0; stencilValue < 10; stencilValue++)
                    {
                        mpb.Clear();
                        mpb.SetVector(Shader.PropertyToID("_BlitScaleBias"), new Vector4(1, 1, 0, 0));

                        cmd.DrawProcedural(Matrix4x4.identity, _mats[stencilValue], 0, MeshTopology.Triangles, 3, 1, mpb);
                    }
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

            }
        }

        [SerializeField] private ShaderResources shaders;
        private Material debugMaterial;
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
            shaders = new ShaderResources().Load();
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

            if (!CreateMaterials())
            {
                Debug.LogWarning("Not all required materials could be created. Stencil Debug will not render.");
                return;
            }

            stencilDebugPass.Setup(ref debugMaterial, scale, margin);
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
            DestroyMaterials();
        }

        private void OnDestroy()
        {
            stencilDebugPass?.Dispose();
        }

        private void DestroyMaterials()
        {
            CoreUtils.Destroy(debugMaterial);
        }

        private bool CreateMaterials()
        {
            if (debugMaterial == null)
            {
                debugMaterial = CoreUtils.CreateEngineMaterial(shaders.debug);
            }

            return debugMaterial != null;
        }
    }
}