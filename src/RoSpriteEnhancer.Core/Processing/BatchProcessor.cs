using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using RoSpriteEnhancer.Core.Filtering;
using RoSpriteEnhancer.Core.Upscaling;

namespace RoSpriteEnhancer.Core.Processing
{
    public enum OutputFormat
    {
        Png,
        Bmp_MagentaKey,
        Bmp_Indexed8_Palette
    }

    public enum ScaleAlgorithm
        {
            Scale2x,
            XbrzX2,
            XbrzX4,
            EsrganExternal,
            Waifu2xAnime,
            Waifu4xAnime,
            RealCugan2x,
            RealCugan4x,
            // ── NEU: RealESRGAN ncnn Vulkan (offline KI-Upscale, beste Anime-Qualität) ──
            RealESRGAN2x,
            RealESRGAN4x,
            // ── NEU: GFPGAN Gesicht & Detail-Restaurierung (TASK-1506) ──
            GFPGANRestore1x,   // Nur Detail-Restaurierung, kein Upscale
            GFPGANRestore2x,   // 2× Upscale + Detail-Restaurierung
            // Progressive Multi-Pass: 2x → Shading → 2x → Shading → ... → Final AI 4x
            ProgressiveScale2x_Waifu4x,
            ProgressiveScale2x_RealCugan4x,
            ProgressiveScale2x_Xbrz4x,
            // Pure Progressive (no AI): 2x → Shading → 2x → Shading → 2x → Shading
            ProgressivePure2x,
            ProgressivePure4x,
            // NEW: AI-First + High-Res Shading Pipeline (Best 3D Quality)
            AiFirst_HighResShading
        }

    public class CategoryOverride
    {
        public bool  Outline           { get; set; } = true;
        public float OutlineStrength   { get; set; } = 0.5f;
        public bool  Shadow            { get; set; } = true;
        public float ShadowStrength    { get; set; } = 1.0f;
        public bool  Highlight         { get; set; } = true;
        public float HighlightStrength { get; set; } = 1.0f;
        public bool  Sharpen           { get; set; } = false;
        public float SharpenStrength   { get; set; } = 0.3f;
        public bool  Volumetric3D      { get; set; } = false;
        public float Volumetric3DStrength { get; set; } = 0.5f;

        public EnhancerOptions ToEnhancerOptions(EnhancerOptions global) => new()
        {
            Outline = Outline, OutlineStrength = OutlineStrength,
            Shadow = Shadow, ShadowStrength = ShadowStrength,
            Highlight = Highlight, HighlightStrength = HighlightStrength,
            Sharpen = Sharpen, SharpenStrength = SharpenStrength,
            Volumetric3D = Volumetric3D, Volumetric3DStrength = Volumetric3DStrength,
            OutlineMode = global.OutlineMode,
            ExclusionColor = global.ExclusionColor,
            HueShift = global.HueShift,
            SaturationScale = global.SaturationScale,
            LightnessScale = global.LightnessScale,
        };
    }

    public class ProcessingOptions
    {
        public string InputFolder    { get; set; } = "";
        public string OutputFolder   { get; set; } = "";
        public int    ScalePasses    { get; set; } = 1;
        public ScaleAlgorithm ScaleAlgorithm { get; set; } = ScaleAlgorithm.Scale2x;
        public bool   IncludeBmp     { get; set; } = true;
        public bool   IncludePng     { get; set; } = true;
        public bool   IncludeTga     { get; set; } = true;
        public bool   IncludeSpr     { get; set; } = true;
        public bool   ScaleActAnchors { get; set; } = false;
        public bool   Subfolders     { get; set; } = true;

        public bool HandleMagentaKey { get; set; } = true;
        public int  AlphaThreshold   { get; set; } = 128;

        public OutputFormat OutputFormat { get; set; } = OutputFormat.Png;
        public EnhancerOptions Enhancer  { get; set; } = new();
        public SuperSamplerOptions SuperSampler { get; set; } = new();
        public EsrganOptions Esrgan { get; set; } = new();
        public Waifu2xOptions Waifu2x { get; set; } = new();
        public RealCuganOptions RealCugan { get; set; } = new();
        public AiFirstOptions AiFirst { get; set; } = new();
        public RealESRGANOptions RealESRGAN { get; set; } = new();
        public GFGANOptions GFPGAN { get; set; } = new();

        public Dictionary<string, CategoryOverride>? CategoryOverrides { get; set; } = null;
        public Dictionary<string, SpriteResolutionProfile> ResolutionProfiles { get; set; } = SpriteResolutionProfiles.All;
        public bool IncrementalMode { get; set; } = false;
        public bool AutoSelectEngine { get; set; } = true;
    }

    public class ProcessingResult
    {
        public int Processed { get; set; }
        public int Skipped   { get; set; }
        public int Errors    { get; set; }
        public int SkippedUnchanged { get; set; }
        public List<string> ErrorFiles { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public List<(string File, float ChangeMetric)> OutlierFiles { get; set; } = new();
    }

    public class BatchProcessor
    {
        public static event Action<string>? LogMessage;
        private CancellationTokenSource? _cts;
        private SemaphoreSlim _pauseGate = new SemaphoreSlim(1, 1);
        private volatile bool _isPaused;
        private readonly object _qualityLock = new();
        public List<QualityReport> QualityReports { get; } = new();
        public void Cancel() => _cts?.Cancel();
        public bool IsPaused => _isPaused;

        // --- ETA-Tracking ---
        private System.Diagnostics.Stopwatch _etaStopwatch = new System.Diagnostics.Stopwatch();
        private int _filesProcessedForEta = 0;
        private long _totalMsForEta = 0;

        /// <summary>
        /// Durchschnittliche Verarbeitungszeit pro Datei in Millisekunden.
        /// </summary>
        public double AverageMsPerFile => _filesProcessedForEta > 0 ? (double)_totalMsForEta / _filesProcessedForEta : 0;

        /// <summary>
        /// Durchsatz in Sprites pro Sekunde (basierend auf durchschnittlicher Verarbeitungszeit).
        /// Gibt 0 zurück, wenn noch keine Dateien verarbeitet wurden.
        /// </summary>
        public double ThroughputSpritesPerSecond => AverageMsPerFile > 0 ? 1000.0 / AverageMsPerFile : 0;

        /// <summary>
        /// Setzt die Batch-Verarbeitung auf Pause. Laufende Dateien werden zu Ende verarbeitet,
        /// neue Dateien warten bis Resume() aufgerufen wird.
        /// </summary>
        public void Pause()
        {
            if (_isPaused) return;
            _isPaused = true;
            _pauseGate.Wait(); // Tore schließen (Count 1→0)
            LogMessage?.Invoke("Batch-Verarbeitung pausiert.");
        }

        /// <summary>
        /// Setzt die Batch-Verarbeitung nach einer Pause fort.
        /// </summary>
        public void Resume()
        {
            if (!_isPaused) return;
            _isPaused = false;
            _pauseGate.Release(); // Tore öffnen (Count 0→1)
            LogMessage?.Invoke("Batch-Verarbeitung fortgesetzt.");
        }

        /// <summary>
        /// Prüft den Pause-Zustand und wartet falls nötig. Wird vor jeder Datei aufgerufen.
        /// </summary>
        private async Task CheckPauseAsync(CancellationToken token)
        {
            if (_isPaused)
            {
                await _pauseGate.WaitAsync(token);
                _pauseGate.Release();
            }
        }

        public List<string> GetFiles(ProcessingOptions opts)
        {
            if (string.IsNullOrEmpty(opts.InputFolder) || !Directory.Exists(opts.InputFolder))
                return new List<string>();

            var exts = new List<string>();
            if (opts.IncludeBmp) exts.Add("*.bmp");
            if (opts.IncludePng) exts.Add("*.png");
            if (opts.IncludeTga) exts.Add("*.tga");
            if (opts.IncludeSpr) exts.Add("*.spr");

            var files = new List<string>();
            var searchOpt = opts.Subfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var ext in exts)
                files.AddRange(Directory.GetFiles(opts.InputFolder, ext, searchOpt));

            files.Sort();
            return files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        public async Task<ProcessingResult> ProcessAsync(
            ProcessingOptions opts,
            List<string> files,
            IProgress<(int current, int total, string file, double throughput)>? progress = null,
            CancellationToken ct = default)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            // Pause-Zustand zurücksetzen für neue Verarbeitung
            _pauseGate?.Dispose();
            _pauseGate = new SemaphoreSlim(1, 1);
            _isPaused = false;

            var result = new ProcessingResult();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _etaStopwatch.Restart();

            Directory.CreateDirectory(opts.OutputFolder);

            int processed = 0, skipped = 0, errors = 0, skippedUnchanged = 0;
            int completedCount = 0;
            var errorFiles = new ConcurrentBag<string>();
            var metrics = new ConcurrentBag<(string File, float Metric)>();

            var manifestEntries = new ConcurrentDictionary<string, IncrementalManifest.ManifestEntry>();
            if (opts.IncrementalMode)
            {
                var loaded = IncrementalManifest.Load(opts.OutputFolder);
                foreach (var kv in loaded.Entries)
                    manifestEntries[kv.Key] = kv.Value;
            }

            int maxParallel = Math.Max(1, Math.Min(Environment.ProcessorCount - 1, 8));

            try
            {
                var parallelOpts = new ParallelOptions
                {
                    MaxDegreeOfParallelism = maxParallel,
                    CancellationToken = _cts.Token
                };

                await Parallel.ForEachAsync(files, parallelOpts, async (file, token) =>
                {
                    // Pause-Prüfung: Warten bis Resume() aufgerufen wird oder Abbruch erfolgt
                    await CheckPauseAsync(token);

                    string relative = Path.GetRelativePath(opts.InputFolder, file);
                    string outFile  = Path.Combine(opts.OutputFolder, relative);

                    try
                    {
                        await Task.Run(() =>
                        {
                            string ext = Path.GetExtension(file).ToLowerInvariant();
                            string relPath = relative.Replace('/', '\\');

                            string expectedOutputPath = ext == ".spr"
                                ? outFile : GetOutputPath(outFile, opts.OutputFormat);

                            string? contentHash = null, settingsHash = null;
                            if (opts.IncrementalMode)
                            {
                                contentHash  = IncrementalManifest.ComputeContentHash(file);
                                var enhancerForHash = ResolveEnhancerOptions(relPath, opts);
                                settingsHash = IncrementalManifest.ComputeSettingsHash(opts, enhancerForHash);

                                if (File.Exists(expectedOutputPath)
                                    && manifestEntries.TryGetValue(relative, out var existing)
                                    && existing.ContentHash == contentHash
                                    && existing.SettingsHash == settingsHash)
                                {
                                    Interlocked.Increment(ref skippedUnchanged);
                                    return;
                                }
                            }

                            bool success;

                            if (ext == ".spr")
                            {
                                string? outDir = Path.GetDirectoryName(outFile);
                                if (!string.IsNullOrEmpty(outDir))
                                    Directory.CreateDirectory(outDir);

                                bool ok = ProcessSpr(file, outFile, opts, out float appliedScale, out float changeMetric);
                                success = ok;
                                if (ok)
                                {
                                    Interlocked.Increment(ref processed);
                                    metrics.Add((relative, changeMetric));
                                }
                                else Interlocked.Increment(ref skipped);

                                if (ok && opts.ScaleActAnchors)
                                {
                                    string actIn = Path.ChangeExtension(file, ".act");
                                    if (File.Exists(actIn))
                                    {
                                        string actOut = Path.ChangeExtension(outFile, ".act");
                                        bool actOk = ActFile.TryScaleOffsets(actIn, actOut, appliedScale);
                                        if (!actOk)
                                            LogMessage?.Invoke($"ACT-Anker nicht angepasst: {Path.GetFileName(actIn)}");
                                    }
                                }
                            }
                            else
                            {
                                string outputPath = GetOutputPath(outFile, opts.OutputFormat);
                                string? bmpOutDir = Path.GetDirectoryName(outputPath);
                                if (!string.IsNullOrEmpty(bmpOutDir))
                                    Directory.CreateDirectory(bmpOutDir);

                                using Bitmap? processedBmp = ProcessSingle(file, opts, out float changeMetric);
                                if (processedBmp == null)
                                {
                                    Interlocked.Increment(ref skipped);
                                    success = false;
                                }
                                else
                                {
                                    SaveBitmap(processedBmp, outputPath, opts.OutputFormat);
                                    Interlocked.Increment(ref processed);
                                    metrics.Add((relative, changeMetric));
                                    success = true;
                                }
                            }

                            if (success && opts.IncrementalMode && contentHash != null && settingsHash != null)
                            {
                                manifestEntries[relative] = new IncrementalManifest.ManifestEntry
                                {
                                    ContentHash = contentHash,
                                    SettingsHash = settingsHash
                                };
                            }
                        }, token);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref errors);
                        errorFiles.Add($"{relative}: {ex.Message}");
                        LogMessage?.Invoke($"Fehler: {relative} — {ex.Message}");
                    }

                    int done = Interlocked.Increment(ref completedCount);
                    
                    // ETA-Tracking: Durchschnittliche Zeit pro Datei aktualisieren
                    _etaStopwatch.Stop();
                    double elapsedMs = _etaStopwatch.Elapsed.TotalMilliseconds;
                    Interlocked.Increment(ref _filesProcessedForEta);
                    Interlocked.Add(ref _totalMsForEta, (long)elapsedMs);
                    _etaStopwatch.Restart();

                    progress?.Report((done, files.Count, relative, ThroughputSpritesPerSecond));
                });
            }
            catch (OperationCanceledException) { }
            finally
            {
                sw.Stop();
                result.Processed  = processed;
                result.Skipped    = skipped;
                result.Errors     = errors;
                result.SkippedUnchanged = skippedUnchanged;
                result.ErrorFiles = errorFiles.ToList();
                result.Duration   = sw.Elapsed;
                result.OutlierFiles = new List<(string, float)>();
                _cts.Dispose();
                _cts = null;
                _pauseGate.Dispose();

                if (opts.IncrementalMode)
                {
                    var toSave = new IncrementalManifest();
                    foreach (var kv in manifestEntries)
                        toSave.Entries[kv.Key] = kv.Value;
                    toSave.Save(opts.OutputFolder);
                }
            }

            return result;
        }

        private static Bitmap ScaleSprite(Bitmap src, ProcessingOptions opts, string? categoryId = null)
        {
            // ── TASK-1508 / TASK-2702: Auflösung-Empfehlung pro Kategorie anwenden ──
            if (!string.IsNullOrEmpty(categoryId) && opts.AutoSelectEngine && opts.ResolutionProfiles != null && opts.ResolutionProfiles.TryGetValue(categoryId, out var profile) && profile.IsEnabled)
            {
                opts.ScaleAlgorithm = profile.RecommendedAlgorithm;

                if (opts.ScaleAlgorithm == ScaleAlgorithm.RealESRGAN2x || opts.ScaleAlgorithm == ScaleAlgorithm.RealESRGAN4x ||
                    opts.ScaleAlgorithm == ScaleAlgorithm.GFPGANRestore1x || opts.ScaleAlgorithm == ScaleAlgorithm.GFPGANRestore2x)
                {
                    System.Diagnostics.Debug.WriteLine($"[ScaleSprite] Kategorie '{categoryId}': Profil={profile.DisplayName}, Ziel={profile.TargetWidth}x{profile.TargetHeight}, Algo={profile.RecommendedAlgorithm}, Detail={profile.DetailStrength}");
                }
            }

            if (opts.ScaleAlgorithm == ScaleAlgorithm.Waifu2xAnime || opts.ScaleAlgorithm == ScaleAlgorithm.Waifu4xAnime)
            {
                if (opts.ScaleAlgorithm == ScaleAlgorithm.Waifu4xAnime)
                {
                    // SCHRITTWEISE 2-PASS KASKADEN-SKALIERUNG MIT BILATERAL PRE-FILTER
                    // Pass 1: 1x -> 2x mit Bilateral Pre-Filter
                    using Bitmap smoothed1 = BilateralFilter.Apply(src, 2.0f, 20.0f, preserveDetails: true);
                    var wOptsPass1 = new Waifu2xOptions
                    {
                        ExecutablePath = opts.Waifu2x.ExecutablePath,
                        ModelPath = opts.Waifu2x.ModelPath,
                        NoiseLevel = opts.Waifu2x.NoiseLevel,
                        ScaleFactor = 2,
                        ForceCpuMode = opts.Waifu2x.ForceCpuMode
                    };
                    using Bitmap? pass1 = Waifu2xRunner.Upscale(smoothed1, wOptsPass1);

                    Bitmap inputPass2 = pass1 ?? XbrzScaler.Scale2x(src);

                    // Pass 2: 2x -> 4x mit erneuter Bilateral-Glättung bei doppelter Auflösung
                    using Bitmap smoothed2 = BilateralFilter.Apply(inputPass2, 1.8f, 18.0f, preserveDetails: true);
                    var wOptsPass2 = new Waifu2xOptions
                    {
                        ExecutablePath = opts.Waifu2x.ExecutablePath,
                        ModelPath = opts.Waifu2x.ModelPath,
                        NoiseLevel = -1,
                        ScaleFactor = 2,
                        ForceCpuMode = opts.Waifu2x.ForceCpuMode
                    };

                    Bitmap? pass2 = Waifu2xRunner.Upscale(smoothed2, wOptsPass2);
                    if (pass1 != null) inputPass2.Dispose();

                    if (pass2 != null) return pass2;
                    return XbrzScaler.Scale4x(src);
                }
                else
                {
                    // Standard Waifu2x 2x Pass mit Bilateral Pre-Filter
                    using Bitmap smoothed = BilateralFilter.Apply(src, 2.0f, 20.0f, preserveDetails: true);
                    var wOpts = new Waifu2xOptions
                    {
                        ExecutablePath = opts.Waifu2x.ExecutablePath,
                        ModelPath = opts.Waifu2x.ModelPath,
                        NoiseLevel = opts.Waifu2x.NoiseLevel,
                        ScaleFactor = 2,
                        ForceCpuMode = opts.Waifu2x.ForceCpuMode
                    };

                    Bitmap? result = Waifu2xRunner.Upscale(smoothed, wOpts);
                    if (result != null) return result;
                    return XbrzScaler.Scale2x(src);
                }
            }

            if (opts.ScaleAlgorithm == ScaleAlgorithm.RealCugan2x || opts.ScaleAlgorithm == ScaleAlgorithm.RealCugan4x)
            {
                int targetScale = opts.ScaleAlgorithm == ScaleAlgorithm.RealCugan4x ? 4 : 2;
                var cOpts = new RealCuganOptions
                {
                    ExecutablePath = opts.RealCugan.ExecutablePath,
                    ModelPath = opts.RealCugan.ModelPath,
                    DenoiseLevel = opts.RealCugan.DenoiseLevel,
                    ScaleFactor = targetScale,
                    ForceCpuMode = opts.RealCugan.ForceCpuMode
                };

                using Bitmap smoothed = BilateralFilter.Apply(src, 2.0f, 20.0f, preserveDetails: true);
                Bitmap? result = RealCuganRunner.Upscale(smoothed, cOpts);
                if (result != null) return result;
                return targetScale == 4 ? XbrzScaler.Scale4x(src) : XbrzScaler.Scale2x(src);
            }

            // ── TASK-1502: RealESRGAN ncnn Vulkan (offline KI-Upscale, beste Anime-Qualität) ──
            if (opts.ScaleAlgorithm == ScaleAlgorithm.RealESRGAN2x || opts.ScaleAlgorithm == ScaleAlgorithm.RealESRGAN4x)
            {
                int targetScale = opts.ScaleAlgorithm == ScaleAlgorithm.RealESRGAN4x ? 4 : 2;

                // TASK-1504: Multi-Pass Upscale für beste Qualität
                // Bei 4×: Zwei 2× Passe mit Detail-Injektion dazwischen (besser als ein einzelner 4× Pass)
                // Bei 2×: Ein einzelner Pass mit Detail-Injektion
                Bitmap? result;

                if (targetScale == 4)
                {
                    // Pass 1: 2× Upscale + Detail-Injektion
                    var rOpts1 = new RealESRGANOptions
                    {
                        ExecutablePath = opts.RealESRGAN.ExecutablePath,
                        ModelPath = opts.RealESRGAN.ModelPath,
                        ScaleFactor = 2,
                        TileSize = opts.RealESRGAN.TileSize,
                        TilePad = opts.RealESRGAN.TilePad,
                        PrePad = opts.RealESRGAN.PrePad,
                        UpscaleOnly = opts.RealESRGAN.UpscaleOnly,
                        TimeoutSeconds = opts.RealESRGAN.TimeoutSeconds,
                        UseGpu = opts.RealESRGAN.UseGpu
                    };

                    Bitmap detailMap1 = DetailExtractor.ExtractDetailMap(src);
                    Bitmap? pass1 = RealESRGANRunner.Upscale(src, rOpts1);
                    if (pass1 != null)
                    {
                        Bitmap pass1WithDetail = DetailExtractor.InjectDetail(pass1, detailMap1, 0.35f);
                        detailMap1.Dispose();
                        pass1.Dispose();

                        // Pass 2: 2× Upscale auf dem Ergebnis (insgesamt 4×) + Detail-Injektion
                        var rOpts2 = new RealESRGANOptions
                        {
                            ExecutablePath = opts.RealESRGAN.ExecutablePath,
                            ModelPath = opts.RealESRGAN.ModelPath,
                            ScaleFactor = 2,
                            TileSize = opts.RealESRGAN.TileSize,
                            TilePad = opts.RealESRGAN.TilePad,
                            PrePad = opts.RealESRGAN.PrePad,
                            UpscaleOnly = opts.RealESRGAN.UpscaleOnly,
                            TimeoutSeconds = opts.RealESRGAN.TimeoutSeconds,
                            UseGpu = opts.RealESRGAN.UseGpu
                        };

                        Bitmap detailMap2 = DetailExtractor.ExtractDetailMap(pass1WithDetail);
                        Bitmap? pass2 = RealESRGANRunner.Upscale(pass1WithDetail, rOpts2);
                        if (pass2 != null)
                        {
                            Bitmap pass2WithDetail = DetailExtractor.InjectDetail(pass2, detailMap2, 0.3f);
                            detailMap2.Dispose();
                            pass2.Dispose();
                            pass1WithDetail.Dispose();
                            result = pass2WithDetail;
                        }
                        else
                        {
                            detailMap2.Dispose();
                            pass1WithDetail.Dispose();
                            result = null;
                        }
                    }
                    else
                    {
                        detailMap1.Dispose();
                        result = null;
                    }
                }
                else
                {
                    // 2×: Ein einzelner Pass mit Detail-Injektion
                    var rOpts = new RealESRGANOptions
                    {
                        ExecutablePath = opts.RealESRGAN.ExecutablePath,
                        ModelPath = opts.RealESRGAN.ModelPath,
                        ScaleFactor = targetScale,
                        TileSize = opts.RealESRGAN.TileSize,
                        TilePad = opts.RealESRGAN.TilePad,
                        PrePad = opts.RealESRGAN.PrePad,
                        UpscaleOnly = opts.RealESRGAN.UpscaleOnly,
                        TimeoutSeconds = opts.RealESRGAN.TimeoutSeconds,
                        UseGpu = opts.RealESRGAN.UseGpu
                    };

                    Bitmap detailMap = DetailExtractor.ExtractDetailMap(src);
                    result = RealESRGANRunner.Upscale(src, rOpts);
                    if (result != null)
                    {
                        Bitmap resultWithDetail = DetailExtractor.InjectDetail(result, detailMap, 0.35f);
                        detailMap.Dispose();
                        result.Dispose();
                        result = resultWithDetail;
                    }
                    else
                    {
                        detailMap.Dispose();
                    }
                }

                if (result != null)
                    return result;

                // Fallback auf XBRZ wenn RealESRGAN fehlschlägt
                return targetScale == 4 ? XbrzScaler.Scale4x(src) : XbrzScaler.Scale2x(src);
            }

            // ── TASK-1506: GFPGAN Gesicht & Detail-Restaurierung ──
            if (opts.ScaleAlgorithm == ScaleAlgorithm.GFPGANRestore1x || opts.ScaleAlgorithm == ScaleAlgorithm.GFPGANRestore2x)
            {
                float targetUpscale = opts.ScaleAlgorithm == ScaleAlgorithm.GFPGANRestore2x ? 2.0f : 1.0f;
                var gOpts = new GFGANOptions
                {
                    ExecutablePath = opts.GFPGAN.ExecutablePath,
                    ModelPath = opts.GFPGAN.ModelPath,
                    Upscale = targetUpscale,
                    TileSize = opts.GFPGAN.TileSize,
                    TilePad = opts.GFPGAN.TilePad,
                    TimeoutSeconds = opts.GFPGAN.TimeoutSeconds,
                    UseGpu = opts.GFPGAN.UseGpu,
                    BlendStrength = opts.GFPGAN.BlendStrength
                };

                Bitmap? result = GFPGANRunner.Restore(src, gOpts);
                if (result != null)
                    return result;

                // Fallback
                return targetUpscale == 2.0f ? XbrzScaler.Scale2x(src) : src;
            }

            // ── Progressive Pure Upscale (ohne KI, nur skalieren + schärfen) ──
            if (opts.ScaleAlgorithm == ScaleAlgorithm.ProgressivePure2x || opts.ScaleAlgorithm == ScaleAlgorithm.ProgressivePure4x)
            {
                int targetScale = opts.ScaleAlgorithm == ScaleAlgorithm.ProgressivePure4x ? 4 : 2;
                int passes = targetScale / 2; // 2× → 1 pass, 4× → 2 passes

                Bitmap current = src;
                for (int i = 0; i < passes; i++)
                {
                    Bitmap next = Scale2x.Scale(current, 1);
                    if (i > 0) current.Dispose();
                    current = next;
                }
                return current;
            }

            // ── Progressive Scale 2× → AI 4x (Waifu2x / RealCUGAN / XBRZ) ──
            if (opts.ScaleAlgorithm == ScaleAlgorithm.ProgressiveScale2x_Waifu4x ||
                opts.ScaleAlgorithm == ScaleAlgorithm.ProgressiveScale2x_RealCugan4x ||
                opts.ScaleAlgorithm == ScaleAlgorithm.ProgressiveScale2x_Xbrz4x)
            {
                // Pass 1: 2× XBRZ (schnell, sauber)
                Bitmap pass1 = XbrzScaler.Scale2x(src);

                // Pass 2: AI 2× auf dem 2× Ergebnis (insgesamt 4×)
                Bitmap? pass2 = null;
                if (opts.ScaleAlgorithm == ScaleAlgorithm.ProgressiveScale2x_Waifu4x)
                {
                    var wOpts = new Waifu2xOptions
                    {
                        ExecutablePath = opts.Waifu2x.ExecutablePath,
                        ModelPath = opts.Waifu2x.ModelPath,
                        ScaleFactor = 2,
                        NoiseLevel = opts.Waifu2x.NoiseLevel,
                        TimeoutSeconds = opts.Waifu2x.TimeoutSeconds,
                        ForceCpuMode = opts.Waifu2x.ForceCpuMode
                    };
                    pass2 = Waifu2xRunner.Upscale(pass1, wOpts);
                }
                else if (opts.ScaleAlgorithm == ScaleAlgorithm.ProgressiveScale2x_RealCugan4x)
                {
                    var cOpts = new RealCuganOptions
                    {
                        ExecutablePath = opts.RealCugan.ExecutablePath,
                        ModelPath = opts.RealCugan.ModelPath,
                        DenoiseLevel = opts.RealCugan.DenoiseLevel,
                        ScaleFactor = 2,
                        ForceCpuMode = opts.RealCugan.ForceCpuMode
                    };
                    pass2 = RealCuganRunner.Upscale(pass1, cOpts);
                }
                else // ProgressiveScale2x_Xbrz4x
                {
                    pass2 = XbrzScaler.Scale2x(pass1);
                }

                if (pass2 != null)
                {
                    pass1.Dispose();
                    return pass2;
                }

                pass1.Dispose();
                return XbrzScaler.Scale4x(src);
            }

            return opts.ScaleAlgorithm switch
            {
                ScaleAlgorithm.XbrzX2 => XbrzScaler.Scale2x(src),
                ScaleAlgorithm.XbrzX4 => XbrzScaler.Scale4x(src),
                ScaleAlgorithm.AiFirst_HighResShading => ProcessAiFirst_HighResShading(src, opts),
                _                     => Scale2x.Scale(src, opts.ScalePasses),
            };
        }

        /// <summary>
        /// AI-First + High-Res Shading Pipeline (Option A - Best 3D Quality)
        /// 1. Single best AI upscale (4x Waifu2x/RealCUGAN)
        /// 2. Apply Volumetric3D + Shading on FULL high resolution
        /// 3. Optional final light polish
        /// </summary>
        private static Bitmap ProcessAiFirst_HighResShading(Bitmap src, ProcessingOptions opts)
        {
            var aiOpts = opts.AiFirst;
            var logger = new Action<string>(msg => {
                if (aiOpts.EnableAiLogging) 
                    Console.WriteLine($"[AI-First] {msg}");
            });

            Bitmap aiUpscaled;

            // Progressive Mode: Single 4x AI upscale then enhancements on high-res (Optimized)
            // Previous: 3-pass (2x -> shade -> 2x) - slower, more artifacts
            // Optimized: 1-pass 4x AI -> High-Res Enhancements (faster, cleaner)
            if (aiOpts.ProgressiveMode)
            {
                logger("Progressive Mode (Optimized): Single 4x AI Upscale + High-Res Enhancement");
                
                int targetScale = 4;
                if (aiOpts.TargetWidth > 0 && aiOpts.TargetHeight > 0)
                {
                    targetScale = Math.Max(aiOpts.TargetWidth / src.Width, aiOpts.TargetHeight / src.Height);
                    targetScale = Math.Clamp(targetScale, 1, 8);
                }
                
                aiUpscaled = UpscaleWithBestAI(src, opts, targetScale, logger);
                
                // Apply enhancements on full high resolution (4x)
                logger("High-Res Enhancement on 4x resolution");
                aiUpscaled = ApplyEnhancementsOnHighRes(aiUpscaled, opts.Enhancer, logger);
            }
            else
            {
                // Standard: Single 4x AI upscale
                int targetScale = 4;
                if (aiOpts.TargetWidth > 0 && aiOpts.TargetHeight > 0)
                {
                    targetScale = Math.Max(aiOpts.TargetWidth / src.Width, aiOpts.TargetHeight / src.Height);
                    targetScale = Math.Clamp(targetScale, 1, 8);
                }
                logger($"Standard Mode: AI {targetScale}x Upscale");
                aiUpscaled = UpscaleWithBestAI(src, opts, targetScale, logger);

                try
                {
                    // Final enhancements on full high resolution
                    logger("Final High-Res Enhancement");
                    aiUpscaled = ApplyEnhancementsOnHighRes(aiUpscaled, opts.Enhancer, logger);
                    return aiUpscaled;
                }
                catch
                {
                    aiUpscaled?.Dispose();
                    throw;
                }
            }

            return aiUpscaled;
        }

        /// <summary>
        /// Selects and executes the best available AI upscaler based on AiFirstOptions
        /// </summary>
        private static Bitmap UpscaleWithBestAI(Bitmap src, ProcessingOptions opts, int scaleFactor, Action<string> logger)
        {
            var aiOpts = opts.AiFirst;
            var modelsToTry = GetModelPriorityList(aiOpts);

            foreach (var model in modelsToTry)
            {
                Bitmap? result = TryUpscaleWithModel(src, opts, model, scaleFactor, logger);
                if (result != null)
                {
                    logger($"Success with {model}");
                    return result;
                }
                logger($"Failed with {model}, trying fallback...");
            }

            // Ultimate fallback: XBRZ
            logger("All AI models failed, falling back to XBRZ");
            return scaleFactor >= 4 ? XbrzScaler.Scale4x(src) : XbrzScaler.Scale2x(src);
        }

        private static List<AiFirstModel> GetModelPriorityList(AiFirstOptions aiOpts)
        {
            var list = new List<AiFirstModel>();
            
            if (aiOpts.PrimaryModel != AiFirstModel.Auto)
            {
                list.Add(aiOpts.PrimaryModel);
                if (aiOpts.FallbackModel != aiOpts.PrimaryModel)
                    list.Add(aiOpts.FallbackModel);
            }
            else
            {
                // Auto-detect: prefer Waifu2x for anime, then RealCUGAN, then XBRZ
                list.Add(AiFirstModel.Waifu2x);
                list.Add(AiFirstModel.RealCugan);
                list.Add(AiFirstModel.Xbrz);
            }
            
            return list;
        }

        private static Bitmap? TryUpscaleWithModel(Bitmap src, ProcessingOptions opts, AiFirstModel model, int scaleFactor, Action<string> logger)
        {
            try
            {
                // Apply SuperSampler pre-filter if enabled (sharper edges for AI upscale)
                Bitmap preFiltered;
                if (opts.AiFirst.EnableSuperSampler)
                {
                    logger("Applying SuperSampler pre-filter (Lanczos3)");
                    preFiltered = SuperSampler.Process(src, opts.Enhancer, SuperSampler.DownscaleFilter.Lanczos3, SuperSampler.OutputSize.TwoX);
                    // SuperSampler.Process outputs 2x, we need to downscale back to original for AI input
                    // But wait - SuperSampler is meant as pre-filter before AI, so we should use it at 1x with enhanced quality
                    // Let's use a simpler approach: bilateral filter at 1x for edge preservation
                    preFiltered.Dispose();
                    preFiltered = BilateralFilter.Apply(src, 1.5f, 15.0f, preserveDetails: true);
                }
                else
                {
                    preFiltered = BilateralFilter.Apply(src, 2.0f, 20.0f, preserveDetails: true);
                }

                switch (model)
                {
                    case AiFirstModel.Waifu2x:
                        if (!string.IsNullOrWhiteSpace(opts.Waifu2x.ExecutablePath) && File.Exists(opts.Waifu2x.ExecutablePath))
                        {
                            using var smoothed = BilateralFilter.Apply(preFiltered, 2.0f, 20.0f, preserveDetails: true);
                            var wOpts = new Waifu2xOptions
                            {
                                ExecutablePath = opts.Waifu2x.ExecutablePath,
                                ModelPath = opts.Waifu2x.ModelPath,
                                NoiseLevel = opts.Waifu2x.NoiseLevel,
                                ScaleFactor = scaleFactor,
                                ForceCpuMode = opts.Waifu2x.ForceCpuMode
                            };
                            // TODO: ModelPath selection based on Waifu2xModelVariant
                            var result = Waifu2xRunner.Upscale(smoothed, wOpts);
                            preFiltered.Dispose();
                            return result;
                        }
                        break;

                    case AiFirstModel.RealCugan:
                        if (!string.IsNullOrWhiteSpace(opts.RealCugan.ExecutablePath) && File.Exists(opts.RealCugan.ExecutablePath))
                        {
                            using var smoothed = BilateralFilter.Apply(preFiltered, 2.0f, 20.0f, preserveDetails: true);
                            var cOpts = new RealCuganOptions
                            {
                                ExecutablePath = opts.RealCugan.ExecutablePath,
                                ModelPath = opts.RealCugan.ModelPath,
                                DenoiseLevel = opts.RealCugan.DenoiseLevel, // Could use AiFirst.RealCuganDenoise
                                ScaleFactor = scaleFactor,
                                ForceCpuMode = opts.RealCugan.ForceCpuMode
                            };
                            var result = RealCuganRunner.Upscale(smoothed, cOpts);
                            preFiltered.Dispose();
                            return result;
                        }
                        break;

                    case AiFirstModel.Xbrz:
                        preFiltered.Dispose();
                        return scaleFactor >= 4 ? XbrzScaler.Scale4x(src) : XbrzScaler.Scale2x(src);
                }
            }
            catch (Exception ex)
            {
                logger($"Exception with {model}: {ex.Message}");
            }
            return null;
        }

        private static Bitmap ApplyEnhancementsOnHighRes(Bitmap highRes, EnhancerOptions enhancerOpts, Action<string> logger)
        {
            // Create a copy for enhancement processing
            Bitmap enhanced = new Bitmap(highRes.Width, highRes.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(enhanced))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(highRes, 0, 0, highRes.Width, highRes.Height);
            }
            highRes.Dispose();

            // Apply all pixel enhancements on high-res
            enhanced = PixelEnhancer.Apply(enhanced, enhancerOpts);

            // Final polish for ultra-smooth 3D transitions
            if (enhancerOpts.Volumetric3D || enhancerOpts.Shadow || enhancerOpts.Highlight)
            {
                logger("Applying final bilateral polish");
                using Bitmap polished = BilateralFilter.Apply(enhanced, 1.2f, 12.0f, preserveDetails: true);
                enhanced.Dispose();
                return polished;
            }

            return enhanced;
        }

        public static EnhancerOptions ResolveEnhancerOptions(string relativePath, ProcessingOptions opts)
        {
            if (opts.CategoryOverrides == null || opts.CategoryOverrides.Count == 0)
                return opts.Enhancer;

            foreach (var cat in SpriteFilter.AllCategories)
            {
                if (!opts.CategoryOverrides.TryGetValue(cat.Id, out var ov)) continue;
                if (SpriteFilter.MatchesCategory(relativePath, cat))
                    return ov.ToEnhancerOptions(opts.Enhancer);
            }
            return opts.Enhancer;
        }

        /// <summary>
        /// TASK-1508: Bestimmt die Sprite-Kategorie für einen relativen Pfad.
        /// Gibt die erste matchende Kategorie-ID zurück, oder null wenn keine Kategorie matcht.
        /// </summary>
        private static string? ResolveCategoryId(string relativePath)
        {
            foreach (var cat in SpriteFilter.AllCategories)
            {
                if (SpriteFilter.MatchesCategory(relativePath, cat))
                    return cat.Id;
            }
            return null;
        }

        /// <summary>
        /// Bestimmt die Kategorie, erst über den Pfad, dann über den Bildinhalt als Fallback.
        /// </summary>
        private static string ResolveCategoryIdWithContentFallback(string relativePath, Bitmap? bmp)
        {
            string? pathBased = ResolveCategoryId(relativePath);
            if (pathBased != null) return pathBased;

            // TASK-1902: Inhalt-basierte Kategorie-Erkennung als Fallback
            var contentResult = SpriteContentClassifier.Classify(bmp);
            if (contentResult.Confidence >= 0.5f)
            {
                System.Diagnostics.Debug.WriteLine($"[BatchProcessor] Content-Fallback: '{relativePath}' → {contentResult.PredictedCategoryName} (Confidence: {contentResult.Confidence:P0})");
                return contentResult.PredictedCategoryId;
            }

            return "other";
        }

        public static Bitmap ProcessBitmapPreview(Bitmap src, ProcessingOptions opts, string? categoryId = null, EnhancerOptions? enhancerOverride = null)
        {
            EnhancerOptions enhancerOpts = enhancerOverride ?? opts.Enhancer;

            Bitmap argb = EnsureArgb(src);

            // TASK-1402: Detail-Karte VOR jeglicher Verarbeitung extrahieren (hochfrequente Kanten & Texturen sichern)
            Bitmap detailMap = DetailExtractor.ExtractDetailMap(argb);

            Bitmap preScaled;

            if (opts.HandleMagentaKey && MagentaHandler.ContainsMagenta(argb))
            {
                preScaled = MagentaHandler.MagentaToAlpha(argb);
                argb.Dispose();
            }
            else { preScaled = argb; }

            Bitmap scaled;
            if (opts.SuperSampler.Enabled)
            {
                scaled = SuperSampler.Process(preScaled, enhancerOpts, opts.SuperSampler.Filter, opts.SuperSampler.TargetSize);
                preScaled.Dispose();
            }
            else
            {
                scaled = ScaleSprite(preScaled, opts, categoryId);
                preScaled.Dispose();
            }

            Bitmap enhanced = PixelEnhancer.Apply(scaled, enhancerOpts);
            scaled.Dispose(); // FIX: scaled wird von PixelEnhancer.Apply kopiert, kann entsorgt werden
            Bitmap colorLocked = ColorMatchLock.Apply(src, enhanced);
            Bitmap cleaned  = AlphaBleedCleaner.Apply(colorLocked);

            if (!ReferenceEquals(scaled, enhanced)) enhanced.Dispose();
            if (!ReferenceEquals(enhanced, colorLocked)) colorLocked.Dispose();

            // TASK-1403: Detail-Karte nach dem Downscaling zurückinjizieren (moderate Stärke 0.35)
            Bitmap result = DetailExtractor.InjectDetail(cleaned, detailMap, 0.35f);
            detailMap.Dispose();
            cleaned.Dispose();

            return result;
        }

        public static float ComputeAverageColorDistance(Bitmap before, Bitmap after)
        {
            var (br, bg, bb) = AverageVisibleColor(before);
            var (ar, ag, ab) = AverageVisibleColor(after);
            float dr = ar - br, dg = ag - bg, db = ab - bb;
            return (float)Math.Sqrt(dr * dr + dg * dg + db * db);
        }

        private static (float r, float g, float b) AverageVisibleColor(Bitmap bmp)
        {
            int w = bmp.Width, h = bmp.Height;
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] pixels = new int[w * h];
            try   { Marshal.Copy(data.Scan0, pixels, 0, pixels.Length); }
            finally { bmp.UnlockBits(data); }

            long sr = 0, sg = 0, sb = 0;
            int cnt = 0;
            foreach (int p in pixels)
            {
                if (((p >> 24) & 0xFF) < 10) continue;
                sr += (p >> 16) & 0xFF;
                sg += (p >>  8) & 0xFF;
                sb +=  p        & 0xFF;
                cnt++;
            }
            if (cnt == 0) return (0f, 0f, 0f);
            return ((float)sr / cnt, (float)sg / cnt, (float)sb / cnt);
        }

        public Bitmap? ProcessSingle(string filePath, ProcessingOptions opts)
            => ProcessSingle(filePath, opts, out _);

        public Bitmap? ProcessSingle(string filePath, ProcessingOptions opts, out float changeMetric)
        {
            changeMetric = 0f;
            try
            {
                Bitmap? raw = LoadBitmap(filePath);
                if (raw == null) return null;

                string relPath = Path.GetRelativePath(opts.InputFolder, filePath).Replace('/', '\\');
                EnhancerOptions enhancerOverride = ResolveEnhancerOptions(relPath, opts);

                // TASK-1508: Kategorie für Auflösung-Empfehlung bestimmen
                string? categoryId = ResolveCategoryIdWithContentFallback(relPath, raw);

                Bitmap enhanced = ProcessBitmapPreview(raw, opts, categoryId, enhancerOverride);
                changeMetric = ComputeAverageColorDistance(raw, enhanced);

                // TASK-1806: Automatischer Qualitäts-Check nach Verarbeitung
                var qualityReport = QualityChecker.CheckQuality(raw, enhanced, Path.GetFileName(filePath));
                lock (_qualityLock)
                {
                    QualityReports.Add(qualityReport);
                }

                raw.Dispose();

                if (opts.OutputFormat == OutputFormat.Bmp_MagentaKey ||
                    opts.OutputFormat == OutputFormat.Bmp_Indexed8_Palette)
                {
                    Bitmap withMagenta = MagentaHandler.AlphaToMagenta(enhanced, opts.AlphaThreshold);
                    enhanced.Dispose();
                    enhanced = withMagenta;
                }

                if (opts.OutputFormat == OutputFormat.Bmp_Indexed8_Palette)
                {
                    Bitmap indexed = PaletteQuantizer.Quantize(enhanced, opts.AlphaThreshold);
                    enhanced.Dispose();
                    return indexed;
                }

                return enhanced;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Pipeline-Fehler: {ex.Message}");
                return null;
            }
        }

        public static bool ProcessSpr(string inputPath, string outputPath, ProcessingOptions opts, out float appliedScale, out float changeMetric)
        {
            appliedScale = 1f;
            changeMetric = 0f;
            try
            {
                using var sprFile = SprFile.Load(inputPath);
                if (sprFile.TotalFrameCount == 0) return false;

                string relPath = Path.GetRelativePath(opts.InputFolder, inputPath).Replace('/', '\\');
                EnhancerOptions enhancerOverride = ResolveEnhancerOptions(relPath, opts);

                // TASK-1508 + TASK-1902: Kategorie für Auflösung-Empfehlung bestimmen (Pfad + Inhalt-Fallback)
                var firstFrame = sprFile.Frames.FirstOrDefault();
                string? categoryId = ResolveCategoryIdWithContentFallback(relPath, firstFrame?.Image);

                var frames = sprFile.Frames.OrderBy(f => f.Index).ToList();
                var enhancedFrames = new List<Bitmap>(frames.Count);
                bool scaleMeasured = false;
                float metricSum = 0f;
                int metricCount = 0;

                try
                {
                    foreach (var frame in frames)
                    {
                        if (frame.Width == 0 || frame.Height == 0)
                        {
                            enhancedFrames.Add(new Bitmap(1, 1, PixelFormat.Format32bppArgb));
                            continue;
                        }

                        int origWidth = frame.Image.Width;
                        Bitmap enhanced = ProcessBitmapPreview(frame.Image, opts, categoryId, enhancerOverride);

                        if (!scaleMeasured && origWidth > 0)
                        {
                            appliedScale = (float)enhanced.Width / origWidth;
                            scaleMeasured = true;
                        }

                        metricSum += ComputeAverageColorDistance(frame.Image, enhanced);
                        metricCount++;

                        enhancedFrames.Add(enhanced);
                    }

                    if (metricCount > 0) changeMetric = metricSum / metricCount;

                    

                    int targetIndexedCount, targetRgbaCount;

                    if (opts.OutputFormat == OutputFormat.Png)
                    {
                        targetIndexedCount = 0;
                        targetRgbaCount = enhancedFrames.Count;
                    }
                    else
                    {
                        targetIndexedCount = sprFile.IndexedFrameCount;
                        targetRgbaCount = sprFile.RgbaFrameCount;
                    }

                    SprWriter.Write(outputPath, enhancedFrames, targetIndexedCount, targetRgbaCount);
                    return true;
                }
                finally
                {
                    foreach (var bmp in enhancedFrames) bmp.Dispose();
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"ProcessSpr Fehler: {ex.Message}");
                return false;
            }
        }

        private static void SaveBitmap(Bitmap bmp, string path, OutputFormat fmt)
        {
            switch (fmt)
            {
                case OutputFormat.Png:
                    bmp.Save(path, ImageFormat.Png);
                    break;
                case OutputFormat.Bmp_MagentaKey:
                case OutputFormat.Bmp_Indexed8_Palette:
                    bmp.Save(path, ImageFormat.Bmp);
                    break;
            }
        }

        private static string GetOutputPath(string basePath, OutputFormat fmt) =>
            fmt switch
            {
                OutputFormat.Bmp_MagentaKey       => Path.ChangeExtension(basePath, ".bmp"),
                OutputFormat.Bmp_Indexed8_Palette => Path.ChangeExtension(basePath, ".bmp"),
                _                                 => Path.ChangeExtension(basePath, ".png")
            };

        private static Bitmap? LoadBitmap(string path)
        {
            try
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".tga") return LoadTga(path);
                if (ext == ".spr") return null;
                return new Bitmap(path);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"LoadBitmap Fehler: {ex.Message}");
                return null;
            }
        }

        private static Bitmap EnsureArgb(Bitmap src)
        {
            if (src.PixelFormat == PixelFormat.Format32bppArgb) return new Bitmap(src);

            Bitmap dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using Graphics g = Graphics.FromImage(dst);
            g.Clear(Color.Transparent);
            g.DrawImage(src, 0, 0, src.Width, src.Height);
            return dst;
        }

        private static Bitmap LoadTga(string path)
        {
            using BinaryReader br = new BinaryReader(File.OpenRead(path));
            byte idLength     = br.ReadByte();
            br.ReadByte();
            byte imageType    = br.ReadByte();
            br.ReadBytes(5);
            br.ReadInt16(); br.ReadInt16();
            short width   = br.ReadInt16();
            short height  = br.ReadInt16();
            byte bpp      = br.ReadByte();
            byte desc     = br.ReadByte();
            br.ReadBytes(idLength);

            if (imageType != 2 && imageType != 10)
                throw new NotSupportedException($"TGA imageType {imageType} nicht unterstützt.");

            if (bpp != 24 && bpp != 32)
                throw new NotSupportedException($"TGA {bpp}bpp nicht unterstützt.");

            bool flipY    = (desc & 0x20) == 0;
            int pixelSize = bpp / 8;
            int totalPixels = width * height;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                if (imageType == 2)
                {
                    byte[] raw = br.ReadBytes(totalPixels * pixelSize);
                    unsafe
                    {
                        for (int y = 0; y < height; y++)
                        {
                            int destY = flipY ? (height - 1 - y) : y;
                            int* row  = (int*)((byte*)bmpData.Scan0 + destY * bmpData.Stride);
                            for (int x = 0; x < width; x++)
                            {
                                int s = (y * width + x) * pixelSize;
                                byte b = raw[s]; byte g = raw[s+1]; byte r = raw[s+2];
                                byte a = pixelSize >= 4 ? raw[s+3] : (byte)255;
                                row[x] = (a << 24) | (r << 16) | (g << 8) | b;
                            }
                        }
                    }
                }
                else
                {
                    int[] decoded = new int[totalPixels];
                    int idx = 0;
                    while (idx < totalPixels)
                    {
                        byte header = br.ReadByte();
                        int count   = (header & 0x7F) + 1;

                        if ((header & 0x80) != 0)
                        {
                            byte b = br.ReadByte(); byte g = br.ReadByte(); byte r = br.ReadByte();
                            byte a = pixelSize >= 4 ? br.ReadByte() : (byte)255;
                            int packed = (a << 24) | (r << 16) | (g << 8) | b;
                            for (int i = 0; i < count && idx < totalPixels; i++)
                                decoded[idx++] = packed;
                        }
                        else
                        {
                            for (int i = 0; i < count && idx < totalPixels; i++)
                            {
                                byte b = br.ReadByte(); byte g = br.ReadByte(); byte r = br.ReadByte();
                                byte a = pixelSize >= 4 ? br.ReadByte() : (byte)255;
                                decoded[idx++] = (a << 24) | (r << 16) | (g << 8) | b;
                            }
                        }
                    }

                    unsafe
                    {
                        for (int y = 0; y < height; y++)
                        {
                            int destY = flipY ? (height - 1 - y) : y;
                            int* row  = (int*)((byte*)bmpData.Scan0 + destY * bmpData.Stride);
                            for (int x = 0; x < width; x++)
                                row[x] = decoded[y * width + x];
                        }
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }

            return bmp;
        }
    }
}

