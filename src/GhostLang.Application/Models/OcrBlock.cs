using System.Drawing;
using System.Windows.Media;

namespace GhostLang.Application.Models;

public class OcrBlock
{
    public string Text { get; set; } = string.Empty;
    
    public Rectangle Bounds { get; set; } 
    
    public float Confidence { get; set; }
    
    public ImageSource? BlockBackground { get; set; }
}