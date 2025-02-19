using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace StencilDebugger
{
    [ExcludeFromPreset]
    [DisallowMultipleRendererFeature("Stencil Debug")]
#if UNITY_6000_0_OR_NEWER
    [SupportedOnRenderer(typeof(UniversalRendererData))]
#endif
    [Tooltip("Stencil Debug visualizes the stencil buffer in your scene view.")]
    [HelpURL("https://ameye.dev")]
    public class StencilDebug : ScriptableRendererFeature
    {
        private StencilDebugPass stencilDebugPass;

        private class StencilDebugPass : ScriptableRenderPass
        {
            private class PassData
            {

            }

            private Material debug;
            private float scale, margin;
            private static Material[] _mats;

            public StencilDebugPass()
            {
                profilingSampler = new ProfilingSampler(nameof(StencilDebugPass));
            }

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

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var mpb = new MaterialPropertyBlock();

                var resourcesData = frameData.Get<UniversalResourceData>();

                if (_mats.Length != 10) return;

                using var builder = renderGraph.AddRasterRenderPass<PassData>("Debug Stencil", out _, profilingSampler);
                builder.SetRenderAttachment(resourcesData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourcesData.activeDepthTexture);

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

            public void Dispose()
            {

            }
        }

        [SerializeField] private ShaderResources shaders;
        private Material debugMaterial;
        [SerializeField] private RenderPassEvent injectionPoint;
        [SerializeField] private bool showInSceneView = true;
        [SerializeField] [Range(0.0f, 100.0f)] private float scale = 100.0f;
        [SerializeField] [Range(0.0f, 1.0f)] private float margin = 1.0f;

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