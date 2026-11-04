using System;

namespace RoSpriteEnhancer.Core.Upscaling
{
    /// <summary>
    /// Configuration options for AI-First + High-Res Shading pipeline
    /// </summary>
    public class AiFirstOptions
    {
        /// <summary>Primary AI model to use for 4x upscale</summary>
        public AiFirstModel PrimaryModel { get; set; } = AiFirstModel.Auto;

        /// <summary>Fallback model if primary fails</summary>
        public AiFirstModel FallbackModel { get; set; } = AiFirstModel.Xbrz;

        /// <summary>Enable progressive mode: AI 2x → Shade → AI 2x (for extreme upscales)</summary>
        public bool ProgressiveMode { get; set; } = false;

        /// <summary>Target output width (0 = auto 4x)</summary>
        public int TargetWidth { get; set; } = 0;

        /// <summary>Target output height (0 = auto 4x)</summary>
        public int TargetHeight { get; set; } = 0;

        /// <summary>Waifu2x model variant</summary>
        public Waifu2xModelVariant Waifu2xModel { get; set; } = Waifu2xModelVariant.Anime;

        /// <summary>RealCUGAN denoise level</summary>
        public int RealCuganDenoise { get; set; } = 0;

        /// <summary>Enable structured logging for AI tool execution</summary>
        public bool EnableAiLogging { get; set; } = true;

        /// <summary>Enable SuperSampler pre-filter before AI upscale for sharper edges</summary>
        public bool EnableSuperSampler { get; set; } = true;
    }

    public enum AiFirstModel
    {
        Auto,           // Auto-detect best available
        Waifu2x,        // Waifu2x-ncnn-vulkan
        RealCugan,      // RealCUGAN-ncnn-vulkan
        Xbrz            // Algorithmic fallback
    }

    public enum Waifu2xModelVariant
    {
        Anime,      // Best for anime/sprites (default)
        Photo,      // General photo model
        General     // Balanced
    }
}
