using System.Windows;

namespace GhostLang.WPF.ViewModels;

public class TranslationOverlayViewModel : ViewModelBase
{
    private double _posX;
    private double _posY;
    private double _width;
    private double _height;
    private string _translatedText;
    private Visibility _overlayVisibility = Visibility.Collapsed;

    public double PosX { get => _posX; set { _posX = value; OnPropertyChanged(); } }
    public double PosY { get => _posY; set { _posY = value; OnPropertyChanged(); } }
    public double Width { get => _width; set { _width = value; OnPropertyChanged(); } }
    public double Height { get => _height; set { _height = value; OnPropertyChanged(); } }
    
    public string TranslatedText 
    { 
        get => _translatedText; 
        set { _translatedText = value; OnPropertyChanged(); } 
    }

    public Visibility OverlayVisibility 
    { 
        get => _overlayVisibility; 
        set { _overlayVisibility = value; OnPropertyChanged(); } 
    }

    public void ShowTranslation(Rect boundingBox, string text)
    {
        PosX = boundingBox.Left;
        PosY = boundingBox.Top;
        Width = boundingBox.Width;
        Height = boundingBox.Height;
        TranslatedText = text;
        OverlayVisibility = Visibility.Visible;
    }

    public void HideTranslation()
    {
        OverlayVisibility = Visibility.Collapsed;
    }
    
}