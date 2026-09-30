using System.Buffers.Binary;

namespace Vistora.Api;

public static class SignaturePngValidator
{
    private const string DataUrlPrefix = "data:image/png;base64,";
    private const int MaximumBytes = 256 * 1024;
    private static readonly byte[] PngHeader = [137, 80, 78, 71, 13, 10, 26, 10];

    public static bool TryDecode(string? dataUrl, out byte[] png)
    {
        png = [];
        if (string.IsNullOrWhiteSpace(dataUrl) || !dataUrl.StartsWith(DataUrlPrefix, StringComparison.Ordinal))
            return false;

        var encoded = dataUrl[DataUrlPrefix.Length..];
        if (encoded.Length == 0 || encoded.Length > ((MaximumBytes + 2) / 3 * 4)) return false;
        try
        {
            var decoded = Convert.FromBase64String(encoded);
            if (!IsWellFormedPng(decoded)) return false;
            png = decoded;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    // Bounds the decoded image and walks its chunks before private storage receives it.
    private static bool IsWellFormedPng(byte[] bytes)
    {
        if (bytes.Length is < 45 or > MaximumBytes || !bytes.AsSpan().StartsWith(PngHeader)) return false;

        var offset = PngHeader.Length;
        var isFirstChunk = true;
        var hasImageData = false;
        while (offset <= bytes.Length - 12)
        {
            var chunkLength = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            if (chunkLength > bytes.Length - offset - 12) return false;
            var chunkType = bytes.AsSpan(offset + 4, 4);
            var chunkData = bytes.AsSpan(offset + 8, checked((int)chunkLength));

            if (isFirstChunk)
            {
                if (chunkLength != 13 || !chunkType.SequenceEqual("IHDR"u8)) return false;
                var width = BinaryPrimitives.ReadUInt32BigEndian(chunkData[..4]);
                var height = BinaryPrimitives.ReadUInt32BigEndian(chunkData.Slice(4, 4));
                if (width == 0 || height == 0 || width > 4096 || height > 4096 || (ulong)width * height > 16_000_000)
                    return false;
            }
            if (!isFirstChunk && chunkType.SequenceEqual("IHDR"u8)) return false;
            if (chunkType.SequenceEqual("IDAT"u8) && chunkLength > 0) hasImageData = true;
            if (chunkType.SequenceEqual("IEND"u8))
                return chunkLength == 0 && hasImageData && offset + 12 == bytes.Length;

            isFirstChunk = false;
            offset += checked((int)chunkLength) + 12;
        }

        return false;
    }
}
