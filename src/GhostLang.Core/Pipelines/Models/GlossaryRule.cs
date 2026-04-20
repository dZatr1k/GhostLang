namespace GhostLang.Core.Pipelines.Models;

public class GlossaryRule
{
    public string SourceTerm { get; set; } = string.Empty;

    public string TargetTerm { get; set; } = string.Empty;

    public List<string> SourceVariants { get; set; } = [];
}
