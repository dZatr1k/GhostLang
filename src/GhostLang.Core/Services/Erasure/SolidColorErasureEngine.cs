using GhostLang.Core.Settings.Erasure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GhostLang.Core.Services.Erasure;

public class SolidColorErasureEngine(SolidColorErasureOptions options) : ITextErasureEngine
{
    public async Task<byte[]> EraseTextAsync(byte[] imagePatch)
    {
        if (imagePatch.Length == 0)
            return imagePatch;

        using var msIn = new MemoryStream(imagePatch);
        using var image = await Image.LoadAsync<Rgba32>(msIn);

        var color = Color.ParseHex(string.IsNullOrWhiteSpace(options.ColorHex) ? "#000000" : options.ColorHex);

        image.Mutate(ctx => ctx.Clear(color));

        using var msOut = new MemoryStream();
        await image.SaveAsPngAsync(msOut);

        return msOut.ToArray();
    }
}
