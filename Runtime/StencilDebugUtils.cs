using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace StencilDebugger
{
    [Serializable]
    public sealed class ShaderResources
    {
        public Shader debug;

        public ShaderResources Load()
        {
            debug = Shader.Find(ShaderPath.Debug);
            return this;
        }
    }
    
    static class ShaderPath
    {
        public const string Debug = "Hidden/Stencil Debug";
    }
    
    static class ShaderPass
    {
        public const int Mask = 0;
        public const int Silhouette = 0;
        public const int Information = 0;
        public const int FloodInit = 1;
        public const int FloodJump = 2;
        public const int Outline = 3;
    }
    
    static class ShaderPassName
    {
        public const string Mask = "Mask (Wide Outline)";
        public const string Silhouette = "Silhouette (Wide Outline)";
        public const string Information = "Information (Wide Outline)";
        public const string Flood = "Flood (Wide Outline)";
        public const string Outline = "Outline (Wide Outline)";
    }
    
    static class ShaderPropertyId
    {
        public static readonly int Scale = Shader.PropertyToID("_Scale");
        public static readonly int Margin = Shader.PropertyToID("_Margin");
        public static readonly int StencilRef = Shader.PropertyToID("_StencilRef");
    }
    
    static class ShaderFeature
    {
        public const string AlphaCutout = "ALPHA_CUTOUT";
        public const string CustomDepth = "CUSTOM_DEPTH";
        public const string InformationBuffer = "INFORMATION_BUFFER";
    }
    
    static class Keyword
    {
        public static readonly GlobalKeyword OutlineColor = GlobalKeyword.Create("_OUTLINE_COLOR");
    }
    
    static class Buffer
    {
        public const string Silhouette = "_SilhouetteBuffer";
        public const string SilhouetteDepth = "_SilhouetteDepthBuffer";
        public const string Information = "_InformationBuffer";
        public const string Ping = "_PingBuffer";
        public const string Pong = "_PongBuffer";
    }

    public enum WideOutlineOcclusion
    {
        Always,
        WhenOccluded,
        WhenNotOccluded,
        AsMask
    }
}