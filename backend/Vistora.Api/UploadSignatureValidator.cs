namespace Vistora.Api;

public static class UploadSignatureValidator
{
    public static async Task<bool> MatchesAsync(Stream stream, string contentType, CancellationToken cancellationToken)
    {
        var header = new byte[12];
        var count = 0;
        while (count < header.Length)
        {
            var read = await stream.ReadAsync(header.AsMemory(count), cancellationToken);
            if (read == 0) break;
            count += read;
        }

        return contentType switch
        {
            "image/jpeg" => count >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => count >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => count >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            "application/pdf" => count >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            _ => false
        };
    }
}
