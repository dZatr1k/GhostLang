using System.Windows.Media.Imaging;

namespace GhostLang.Application.Models;

public class TranslationResultArgs
{
    public BitmapSource Image { get; set; }
    public string TranslatedText { get; set; }
    public List<OcrBlock> Blocks { get; set; }
}