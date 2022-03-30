namespace Celeste.GameFiles.Tools.Extensions;
internal static class FileStreamExtensions
{
    private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1835:Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'", Justification = "<Pending>")]
    public static async Task BufferedCopyToAsync(this FileStream source, FileStream destination)
    {
        var buffer = _bufferPool.Rent(81920);
        try
        {
            int bytesRead;

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead);
            }
        }
        finally
        {
            _bufferPool.Return(buffer);
        }
    }
}