namespace GhostLang.Core.Settings.Asr;

public class AzureAsrOptions : AsrEngineOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string Region { get; set; } = "westeurope";
}