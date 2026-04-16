namespace GhostLang.Core.Settings.Erasure;

public class OpenCvErasureOptions : ErasureEngineOptions
{
    public int InpaintRadius { get; set; } = 3;

    public bool UseTeleaAlgorithm { get; set; } = true;

    public int DilationIterations { get; set; } = 3;

    public int AdaptiveBlockSize { get; set; } = 15;

    public double AdaptiveConstant { get; set; } = 10;
}