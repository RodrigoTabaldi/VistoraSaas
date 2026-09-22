namespace Vistora.Application.Reporting;

public interface IReportPdfGenerator
{
    /// <summary>Returns the PDF bytes and its SHA-256 hash (lowercase hex, 64 chars) together, so callers never compute the hash from a different byte array than what was actually uploaded.</summary>
    Task<GeneratedReportPdf> GenerateAsync(Guid inspectionId, CancellationToken cancellationToken = default);
}

public sealed record GeneratedReportPdf(byte[] Content, string Sha256Hex);
