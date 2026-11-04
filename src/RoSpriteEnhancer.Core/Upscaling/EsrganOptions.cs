namespace RoSpriteEnhancer.Core.Upscaling
{
    /// <summary>
    /// EsrganOptions – Konfiguration für ESRGAN-basiertes Upscaling.
    ///
    /// Hinweis: Der externe EsrganRunner wird aktuell nicht im BatchProcessor aufgerufen.
    /// Diese Options-Klasse bleibt für zukünftige Integration erhalten.
    /// </summary>
    public class EsrganOptions
    {
        public string ExecutablePath { get; set; } = "";
        public string ModelName { get; set; } = "realesrgan-x4plus-anime";
        public string ModelPath { get; set; } = "esrgan";
        public int ScaleFactor { get; set; } = 4;
        public int TileSize { get; set; } = 0;
        public int TilePad { get; set; } = 4;
        public int TimeoutSeconds { get; set; } = 120;
        public bool UseGpu { get; set; } = true;
    }
}
