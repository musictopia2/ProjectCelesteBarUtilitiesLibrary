namespace ProjectCelesteBarUtilitiesLibrary.Extensions;
internal static class BinaryReaderExtensions
{
    private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
    public static string ReadString(this BinaryReader binaryReader, int length, Encoding encoding)
    {
        var buffer = _bufferPool.Rent(length);
        try
        {
            var bytesRead = binaryReader.Read(buffer, 0, length);
            if (bytesRead != length)
            {
                throw new InvalidOperationException($"Excepted to read {length} bytes, but read {bytesRead} from data stream");
            }
            return encoding.GetString(buffer, 0, length);
        }
        finally
        {
            _bufferPool.Return(buffer);
        }
    }
}