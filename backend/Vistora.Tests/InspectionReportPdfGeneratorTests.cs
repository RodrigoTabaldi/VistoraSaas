using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PdfSharp.Pdf.IO;
using Vistora.Application.Persistence;
using Vistora.Application.Storage;
using Vistora.Domain;
using Vistora.Infrastructure.Persistence.PostgreSql;
using Vistora.Infrastructure.Reporting;
using Xunit;

namespace Vistora.Tests;

public sealed class InspectionReportPdfGeneratorTests
{
    [Fact]
    public async Task CompletedInspectionProducesAReadableReportWithMatchingHash()
    {
        var (db, inspection) = await CreateInspectionAsync();
        await using (db)
        {
            var report = await new InspectionReportPdfGenerator(db, new FakeStorage()).GenerateAsync(inspection.Id);

            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(report.Content, 0, 4));
            Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(report.Content)), report.Sha256Hex);
            using var input = new MemoryStream(report.Content);
            using var document = PdfReader.Open(input, PdfDocumentOpenMode.Import);
            Assert.True(document.PageCount > 0);
        }
    }

    [Fact]
    public async Task ChangedEvidenceCannotBeIncludedInAReport()
    {
        var (db, inspection) = await CreateInspectionAsync();
        await using (db)
        {
            var item = Assert.Single(Assert.Single(inspection.Rooms).Items);
            db.Evidence.Add(new Evidence
            {
                Id = Guid.NewGuid(), OrganizationId = inspection.OrganizationId,
                InspectionItemId = item.Id, FileName = "foto.jpg", ContentType = "image/jpeg",
                ObjectKey = "evidence/photo.jpg", SizeBytes = 3,
                Sha256 = Convert.ToHexStringLower(SHA256.HashData([1, 2, 3]))
            });
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                new InspectionReportPdfGenerator(db, new FakeStorage([4, 5, 6])).GenerateAsync(inspection.Id));
        }
    }

    private static async Task<(VistoraDbContext Db, Inspection Inspection)> CreateInspectionAsync()
    {
        var organizationId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<VistoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new VistoraDbContext(options, new TestTenantContext(organizationId));
        var property = new Property
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId,
            Name = "Apartamento São João", Address = "Rua das Flores, 10"
        };
        var unit = new Unit
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId,
            PropertyId = property.Id, Identifier = "101"
        };
        var inspection = new Inspection
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, UnitId = unit.Id,
            Type = InspectionType.MoveIn, Status = InspectionStatus.Completed,
            CompletedAtUtc = DateTimeOffset.UtcNow
        };
        var room = new InspectionRoom
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId,
            InspectionId = inspection.Id, Name = "Sala", Position = 0
        };
        room.Items.Add(new InspectionItem
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId,
            InspectionRoomId = room.Id, Description = "Paredes", Response = "Boas",
            Notes = "Sem danos", Position = 0
        });
        inspection.Rooms.Add(room);
        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Inspections.Add(inspection);
        await db.SaveChangesAsync();
        return (db, inspection);
    }

    private sealed class TestTenantContext(Guid organizationId) : ITenantContext
    {
        public Guid? OrganizationId => organizationId;
    }

    private sealed class FakeStorage(byte[]? content = null) : IPrivateObjectStorage
    {
        public Task UploadAsync(StorageUpload upload, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<byte[]> DownloadAsync(string objectKey, long maxBytes, CancellationToken cancellationToken = default) =>
            Task.FromResult(content ?? []);
        public Uri CreateDownloadUrl(string objectKey, TimeSpan lifetime) => throw new NotSupportedException();
    }
}
