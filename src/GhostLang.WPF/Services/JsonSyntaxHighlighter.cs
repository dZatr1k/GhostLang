using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace GhostLang.WPF.Services;

public static class JsonSyntaxHighlighter
{
    private enum TokenType { Default, Key, String, Number, Bool, Null, Punct }

    public static FlowDocument Build(string json)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            PagePadding = new Thickness(0)
        };

        var para = new Paragraph { Margin = new Thickness(0), LineHeight = 1 };

        var keyBrush = GetBrush("JsonKeyBrush") ?? GetBrush("PrimaryBrush") ?? Brushes.DodgerBlue;
        var stringBrush = GetBrush("JsonStringBrush") ?? GetBrush("SuccessBrush") ?? Brushes.SeaGreen;
        var numberBrush = GetBrush("JsonNumberBrush") ?? Brushes.DarkOrange;
        var literalBrush = GetBrush("JsonLiteralBrush") ?? GetBrush("DangerBrush") ?? Brushes.OrangeRed;
        var punctBrush = GetBrush("ThirdlyTextBrush") ?? Brushes.Gray;
        var defaultBrush = GetBrush("PrimaryTextBrush") ?? Brushes.Black;

        Tokenize(json, (text, type) =>
        {
            var brush = type switch
            {
                TokenType.Key => keyBrush,
                TokenType.String => stringBrush,
                TokenType.Number => numberBrush,
                TokenType.Bool => literalBrush,
                TokenType.Null => literalBrush,
                TokenType.Punct => punctBrush,
                _ => defaultBrush
            };
            para.Inlines.Add(new Run(text) { Foreground = brush });
        });

        doc.Blocks.Add(para);
        return doc;
    }

    private static Brush? GetBrush(string key)
    {
        var resource = Application.Current?.TryFindResource(key);
        return resource as Brush;
    }

    private static void Tokenize(string json, Action<string, TokenType> emit)
    {
        var i = 0;
        while (i < json.Length)
        {
            var c = json[i];

            if (c == '"')
            {
                var start = i;
                i++;
                while (i < json.Length)
                {
                    if (json[i] == '\\' && i + 1 < json.Length) { i += 2; continue; }
                    if (json[i] == '"') { i++; break; }
                    i++;
                }

                var j = i;
                while (j < json.Length && char.IsWhiteSpace(json[j])) j++;
                var isKey = j < json.Length && json[j] == ':';

                emit(json.Substring(start, i - start), isKey ? TokenType.Key : TokenType.String);
            }
            else if (c == '-' || char.IsDigit(c))
            {
                var start = i;
                i++;
                while (i < json.Length &&
                       (char.IsDigit(json[i]) || json[i] == '.' || json[i] == '+' || json[i] == '-' ||
                        json[i] == 'e' || json[i] == 'E'))
                    i++;
                emit(json.Substring(start, i - start), TokenType.Number);
            }
            else if (char.IsLetter(c))
            {
                var start = i;
                while (i < json.Length && char.IsLetter(json[i])) i++;
                var word = json.Substring(start, i - start);
                var type = word switch
                {
                    "true" or "false" => TokenType.Bool,
                    "null" => TokenType.Null,
                    _ => TokenType.Default
                };
                emit(word, type);
            }
            else if (c is '{' or '}' or '[' or ']' or ',' or ':')
            {
                emit(c.ToString(), TokenType.Punct);
                i++;
            }
            else
            {
                var start = i;
                while (i < json.Length)
                {
                    var ch = json[i];
                    if (ch == '"' || ch == '-' || char.IsDigit(ch) || char.IsLetter(ch) ||
                        ch is '{' or '}' or '[' or ']' or ',' or ':')
                        break;
                    i++;
                }
                if (i > start)
                    emit(json.Substring(start, i - start), TokenType.Default);
                else
                    i++;
            }
        }
    }
}
