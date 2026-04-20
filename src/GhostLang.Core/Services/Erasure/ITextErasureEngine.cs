namespace GhostLang.Core.Services.Erasure;

public interface ITextErasureEngine
{
    Task<byte[]> EraseTextAsync(byte[] imagePatch);
}
