using System.Security.Cryptography;
using System.Text;
using Vistora.Application.Reporting;

namespace Vistora.Infrastructure.Reporting;

public sealed class PlaceholderReportPdfGenerator : IReportPdfGenerator
{
    public Task<GeneratedReportPdf> GenerateAsync(Guid inspectionId, CancellationToken cancellationToken = default)
    {
        var text = $"Vistora Inspection Report Placeholder - Inspection: {inspectionId} - Generated: {DateTimeOffset.UtcNow:O}";
        var safeText = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

        var streamContent = $"BT\n/F1 12 Tf\n50 750 Td\n({safeText}) Tj\nET";
        var streamLength = Encoding.ASCII.GetByteCount(streamContent);

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");

        var obj1Offset = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        var obj2Offset = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

        var obj3Offset = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n");

        var obj4Offset = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");

        var obj5Offset = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append($"5 0 obj\n<< /Length {streamLength} >>\nstream\n");
        sb.Append(streamContent);
        sb.Append("\nendstream\nendobj\n");

        var xrefOffset = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append("xref\n");
        sb.Append("0 6\n");
        sb.Append("0000000000 65535 f \n");
        sb.Append($"{obj1Offset:D10} 00000 n \n");
        sb.Append($"{obj2Offset:D10} 00000 n \n");
        sb.Append($"{obj3Offset:D10} 00000 n \n");
        sb.Append($"{obj4Offset:D10} 00000 n \n");
        sb.Append($"{obj5Offset:D10} 00000 n \n");

        sb.Append("trailer\n");
        sb.Append("<< /Size 6 /Root 1 0 R >>\n");
        sb.Append("startxref\n");
        sb.Append($"{xrefOffset}\n");
        sb.Append("%%EOF\n");

        var pdfBytes = Encoding.ASCII.GetBytes(sb.ToString());
        var sha256Bytes = SHA256.HashData(pdfBytes);
        var sha256Hex = Convert.ToHexStringLower(sha256Bytes);

        return Task.FromResult(new GeneratedReportPdf(pdfBytes, sha256Hex));
    }
}
