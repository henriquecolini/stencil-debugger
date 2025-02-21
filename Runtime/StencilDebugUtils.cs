using UnityEngine;

namespace StencilDebugger
{
    static class ShaderPath
    {
        public const string DebugGuid = "1f6212cbaa876c447b940eda42123a9b";
    }
    
    static class ShaderPropertyId
    {
        public static readonly int Scale = Shader.PropertyToID("_Scale");
        public static readonly int Margin = Shader.PropertyToID("_Margin");
    }
    
    static class ShaderPassName
    {
        public const string Generate = "Generate (Debug Stencil)";
        public const string Compose = "Compose (Debug Stencil)";
    }
    
    static class Buffer
    {
        public const string StencilDebug = "_StencilDebugTexture";
        public const string Stencil = "_StencilTexture";
        public const string CameraColor = "_CameraColorTexture";
    }
}