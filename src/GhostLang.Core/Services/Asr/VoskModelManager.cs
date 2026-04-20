namespace GhostLang.Core.Services.Asr;

public class VoskModelManager : IVoskModelManager
{

    private static readonly string[] RequiredRelativeFiles =
    {
        Path.Combine("am", "final.mdl"),
        Path.Combine("conf", "model.conf")
    };

    public IReadOnlyList<VoskModelInfo> DiscoverModels(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            return Array.Empty<VoskModelInfo>();

        var results = new List<VoskModelInfo>();
        foreach (var dir in Directory.EnumerateDirectories(rootPath))
        {
            var name = Path.GetFileName(dir);
            var (valid, reason) = ProbeDirectory(dir);
            results.Add(new VoskModelInfo(name, dir, valid, reason));
        }

        return results
            .OrderByDescending(m => m.IsValid)
            .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool IsValidModelDirectory(string modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath) || !Directory.Exists(modelPath))
            return false;
        return ProbeDirectory(modelPath).valid;
    }

    private static (bool valid, string? reason) ProbeDirectory(string dir)
    {
        foreach (var relative in RequiredRelativeFiles)
        {
            var full = Path.Combine(dir, relative);
            if (!File.Exists(full))
                return (false, $"Missing: {relative}");
        }
        return (true, null);
    }
}
