using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using Vistora.Application.Persistence;
using Vistora.Application.Reporting;
using Vistora.Application.Storage;

namespace Vistora.Infrastructure.Reporting;

public sealed class InspectionReportPdfGenerator(
    IVistoraDbContext db,
    IPrivateObjectStorage storage) : IReportPdfGenerator
{
    private const string FontFamily = "Vistora Sans";
    private static readonly object FontLock = new();

    public async Task<GeneratedReportPdf> GenerateAsync(Guid inspectionId, CancellationToken cancellationToken = default)
    {
        var inspection = await db.Inspections.AsNoTracking()
            .Include(x => x.Unit).ThenInclude(x => x!.Property)
            .Include(x => x.Rooms).ThenInclude(x => x.Items).ThenInclude(x => x.Evidence)
            .SingleOrDefaultAsync(x => x.Id == inspectionId, cancellationToken)
            ?? throw new InvalidOperationException($"Inspection '{inspectionId}' was not found.");

        EnsureFontResolver();
        var document = new Document();
        document.Info.Title = $"Vistoria {inspection.Id}";
        document.Styles[StyleNames.Normal]!.Font.Name = FontFamily;
        document.Styles[StyleNames.Normal]!.Font.Size = 10;

        var section = document.AddSection();
        section.PageSetup.TopMargin = Unit.FromCentimeter(2);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(2);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(2);
        section.PageSetup.RightMargin = Unit.FromCentimeter(2);

        var title = section.AddParagraph("Relatório de vistoria");
        title.Format.Font.Size = 18;
        title.Format.Font.Bold = true;
        title.Format.SpaceAfter = Unit.FromCentimeter(0.5);

        section.AddParagraph($"Imóvel: {inspection.Unit?.Property?.Name ?? "Não informado"}");
        section.AddParagraph($"Endereço: {inspection.Unit?.Property?.Address ?? "Não informado"}");
        section.AddParagraph($"Unidade: {inspection.Unit?.Identifier ?? "Não informada"}");
        section.AddParagraph($"Tipo: {(inspection.Type == Domain.InspectionType.MoveIn ? "Entrada" : "Saída")}");
        section.AddParagraph($"Vistoria: {inspection.Id}");
        section.AddParagraph($"Concluída em UTC: {inspection.CompletedAtUtc:yyyy-MM-dd HH:mm}");

        foreach (var room in inspection.Rooms.OrderBy(x => x.Position))
        {
            var heading = section.AddParagraph(room.Name);
            heading.Format.SpaceBefore = Unit.FromCentimeter(0.6);
            heading.Format.Font.Bold = true;
            heading.Format.Font.Size = 13;

            foreach (var item in room.Items.OrderBy(x => x.Position))
            {
                var itemParagraph = section.AddParagraph();
                itemParagraph.Format.SpaceBefore = Unit.FromCentimeter(0.3);
                itemParagraph.AddFormattedText(item.Description, TextFormat.Bold);
                section.AddParagraph($"Resposta: {item.Response ?? "Não respondido"}");
                if (!string.IsNullOrWhiteSpace(item.Notes))
                    section.AddParagraph($"Observações: {item.Notes}");

                foreach (var evidence in item.Evidence.OrderBy(x => x.CreatedAtUtc))
                {
                    section.AddParagraph($"Evidência: {evidence.FileName} | SHA-256: {evidence.Sha256}");
                    var content = await storage.DownloadAsync(evidence.ObjectKey, 25 * 1024 * 1024, cancellationToken);
                    if (!SHA256.HashData(content).AsSpan().SequenceEqual(Convert.FromHexString(evidence.Sha256)))
                        throw new InvalidDataException($"Evidence '{evidence.Id}' failed integrity verification.");

                    if (evidence.ContentType is "image/jpeg" or "image/png")
                    {
                        var image = section.AddImage($"base64:{Convert.ToBase64String(content)}");
                        image.Width = Unit.FromCentimeter(12);
                        image.LockAspectRatio = true;
                    }
                    else
                    {
                        section.AddParagraph("Imagem WebP disponível no sistema de evidências.");
                    }
                }
            }
        }

        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.AddText("Vistora • página ");
        footer.AddPageField();

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var output = new MemoryStream();
        renderer.PdfDocument.Save(output, false);
        var bytes = output.ToArray();
        return new GeneratedReportPdf(bytes, Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }

    private static void EnsureFontResolver()
    {
        lock (FontLock)
        {
            GlobalFontSettings.FontResolver ??= new ReportFontResolver();
        }
    }

    private sealed class ReportFontResolver : IFontResolver
    {
        private static readonly string FontDirectory = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts")
            : "/usr/share/fonts/truetype/dejavu";

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
            new(isBold ? "vistora-bold" : "vistora-regular");

        public byte[] GetFont(string faceName)
        {
            var name = OperatingSystem.IsWindows()
                ? faceName == "vistora-bold" ? "arialbd.ttf" : "arial.ttf"
                : faceName == "vistora-bold" ? "DejaVuSans-Bold.ttf" : "DejaVuSans.ttf";
            return File.ReadAllBytes(Path.Combine(FontDirectory, name));
        }
    }
}
