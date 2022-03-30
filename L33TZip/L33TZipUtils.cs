namespace ProjectCelesteBarUtilitiesLibrary.L33TZip;
internal static class L33TZipUtils
{
    private const string _l33tHeader = "l33t";
    private const string _l66tHeader = "l66t";
    #region Check
    public static bool IsL33TZipFile(string fileName)
    {
        using var fileStream = File.Open(fileName, FileMode.Open);
        return StreamIsL33TZip(fileStream);
    }
    private static bool StreamIsL33TZip(Stream stream)
    {
        using var reader = new BinaryReader(stream);
        try
        {
            var fileHeader = new string(reader.ReadChars(4));
            return fileHeader == _l33tHeader || fileHeader == _l66tHeader;
        }
        catch (Exception)
        {
            return false;
        }
    }
    #endregion
    #region Compressing
    public static async Task<byte[]> ReadL33TZipFileAsync(string inputFileName)
    {
        using var sourceFileStream = File.Open(inputFileName, FileMode.Open, FileAccess.Read, FileShare.None);
        using var outputStream = new MemoryStream();
        using var outputStreamWriter = new BinaryWriter(outputStream);
        using var compressedStream = new DeflateStream(outputStream, CompressionLevel.Optimal);
        WriteFileHeaders(outputStreamWriter, sourceFileStream.Length);
        await WriteCompressedStreamAsync(compressedStream, sourceFileStream, outputStreamWriter);
        return outputStream.ToArray();
    }
    public static async Task CompressFileAsL33TZipAsync(string inputFileName, string outputFileName,
        CancellationToken ct = default,
        IProgress<double>? progress = null)
    {
        try
        {
            using var fileStream = File.Open(inputFileName, FileMode.Open, FileAccess.Read, FileShare.None);
            using var fileStreamFinal = File.Open(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None);
            using var outputFileStreamWriter = new BinaryWriter(fileStreamFinal);
            using var compressedStream = new DeflateStream(fileStreamFinal, CompressionLevel.Optimal);
            WriteFileHeaders(outputFileStreamWriter, fileStream.Length);
            await WriteCompressedStreamAsync(compressedStream, fileStream, outputFileStreamWriter, ct, progress);
        }
        catch (Exception)
        {
            if (File.Exists(outputFileName))
                File.Delete(outputFileName);

            throw;
        }
    }
    private static void WriteFileHeaders(BinaryWriter writer, long fileLength)
    {
        writer.BaseStream.Position = 0L;
        //Write L33T Header & File Length
        if (fileLength > int.MaxValue)
        {
            writer.Write(_l66tHeader.ToCharArray());
            writer.Write(fileLength);
        }
        else
        {
            writer.Write(_l33tHeader.ToCharArray());
            writer.Write(Convert.ToInt32(fileLength));
        }
        //Write Deflate specification (2 Byte)
        writer.Write(new byte[] { 0x78, 0x9C });
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1835:Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'", Justification = "<Pending>")]
    private static async Task WriteCompressedStreamAsync(
        DeflateStream compressedStream,
        FileStream sourceFileStream,
        BinaryWriter final,
        CancellationToken ct = default,
        IProgress<double>? progress = null)
    {
        var buffer = new byte[4096];
        var totalBytesRead = 0L;
        int bytesRead;
        var fileLength = sourceFileStream.Length;
        var lastProgress = 0d;
        while ((bytesRead = await sourceFileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            ct.ThrowIfCancellationRequested();
            if (bytesRead > fileLength)
            {
                totalBytesRead += fileLength;
                await compressedStream.WriteAsync(buffer, 0, Convert.ToInt32(fileLength));
            }
            else if (totalBytesRead + bytesRead <= fileLength)
            {
                totalBytesRead += bytesRead;
                await compressedStream.WriteAsync(buffer, 0, bytesRead);
            }
            else if (totalBytesRead + bytesRead > fileLength)
            {
                var leftToRead = fileLength - totalBytesRead;
                totalBytesRead += leftToRead;
                final.Write(buffer, 0, Convert.ToInt32(leftToRead));
            }
            var newProgress = (double)totalBytesRead / fileLength * 100;
            if (newProgress - lastProgress > 1)
            {
                progress?.Report(newProgress);
                lastProgress = newProgress;
            }
            if (totalBytesRead >= fileLength)
            {
                break;
            }
        }
    }
    #endregion
    #region Extracting
    public static async Task ExtractL33TZipFileAsync(string inputFileName, string outputFileName)
    {
        try
        {
            using var fileStream = File.Open(inputFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var fileStreamReader = new BinaryReader(fileStream);
            using var compressedStream = new DeflateStream(fileStreamReader.BaseStream, CompressionMode.Decompress);
            using var sourceStream = File.Open(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None);
            using var sourceStreamWriter = new BinaryWriter(sourceStream);
            long fileLength = ReadFileLengthFromCompressedFile(fileStreamReader);
            await ReadCompressedStreamAsync(compressedStream, sourceStreamWriter, fileLength);
        }
        catch
        {
            if (File.Exists(outputFileName))
            {
                File.Delete(outputFileName);
            }
            throw;
        }
    }
    public static async Task<byte[]> ExtractL33TZipFileAsync(string zipFileName)
    {
        using var fileStream = File.Open(zipFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var fileStreamReader = new BinaryReader(fileStream);
        return await ExtractL33TZipStreamAsync(fileStreamReader);
    }
    public static async Task<byte[]> ExtractL33TZippedBytesAsync(byte[] zipData)
    {
        using var fileStream = new MemoryStream(zipData, false);
        using var fileStreamReader = new BinaryReader(fileStream);
        return await ExtractL33TZipStreamAsync(fileStreamReader);
    }
    public static async Task<byte[]> ExtractL33TZipStreamAsync(BinaryReader zipReader)
    {
        zipReader.BaseStream.Seek(0, SeekOrigin.Begin);
        long fileLength = ReadFileLengthFromCompressedFile(zipReader);
        using var compressedStream = new DeflateStream(zipReader.BaseStream, CompressionMode.Decompress);
        using var outputMemoryStream = new MemoryStream();
        using var outputWriter = new BinaryWriter(outputMemoryStream);
        await ReadCompressedStreamAsync(compressedStream, outputWriter, fileLength);
        return outputMemoryStream.ToArray();
    }
    public static async Task ExtractL33TZipFileAsync(string fileName, string outputFileName,
        CancellationToken ct = default,
        IProgress<double>? progress = null)
    {
        try
        {
            using var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var fileStreamReader = new BinaryReader(fileStream);
            using var compressedStream = new DeflateStream(fileStreamReader.BaseStream, CompressionMode.Decompress);
            using var outputStream = File.Open(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None);
            using var outputStreamWriter = new BinaryWriter(outputStream);
            long length = ReadFileLengthFromCompressedFile(fileStreamReader);
            ct.ThrowIfCancellationRequested();
            await ReadCompressedStreamAsync(compressedStream, outputStreamWriter, length, ct, progress);
        }
        catch (Exception)
        {
            if (File.Exists(outputFileName))
            {
                File.Delete(outputFileName);
            }
            throw;
        }
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1835:Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'", Justification = "<Pending>")]
    private static async Task ReadCompressedStreamAsync(
        DeflateStream compressedStream,
        BinaryWriter targetStream,
        long fileLength,
        CancellationToken ct = default,
        IProgress<double>? progress = null)
    {
        var buffer = new byte[4096];
        int bytesRead;
        var totalBytesRead = 0L;
        var lastProgress = 0d;
        while ((bytesRead = await compressedStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            ct.ThrowIfCancellationRequested();

            if (bytesRead > fileLength)
            {
                totalBytesRead += fileLength;
                targetStream.Write(buffer, 0, (int)fileLength);
            }
            else if (totalBytesRead + bytesRead <= fileLength)
            {
                totalBytesRead += bytesRead;
                targetStream.Write(buffer, 0, bytesRead);
            }
            else if (totalBytesRead + bytesRead > fileLength)
            {
                var leftToRead = fileLength - totalBytesRead;
                totalBytesRead += leftToRead;
                targetStream.Write(buffer, 0, (int)leftToRead);
            }
            var newProgress = (double)totalBytesRead / fileLength * 100;
            if (newProgress - lastProgress > 1)
            {
                progress?.Report(newProgress);
                lastProgress = newProgress;
            }
            if (totalBytesRead >= fileLength)
            {
                break;
            }
        }
    }
    private static long ReadFileLengthFromCompressedFile(BinaryReader reader)
    {
        var fileHeader = new string(reader.ReadChars(4));
        long length;
        switch (fileHeader.ToLower())
        {
            case _l33tHeader:
                length = reader.ReadInt32();
                //Skip deflate specification (2 Byte)
                reader.BaseStream.Position = 10L;
                break;
            case _l66tHeader:
                length = reader.ReadInt64();
                //Skip deflate specification (2 Byte)
                reader.BaseStream.Position = 14L;
                break;
            default:
                throw new FileLoadException($"Header '{fileHeader}' is not recognized as a valid type");
        }
        return length;
    }
    #endregion
}