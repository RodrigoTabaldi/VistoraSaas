using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Vistora.Application.Persistence;
using Vistora.Application.Reporting;
using Vistora.Application.Storage;
using Vistora.Domain;
using Vistora.Infrastructure.Persistence.PostgreSql;
using Xunit;

namespace Vistora.Tests;

public sealed class ReportJobProcessorTests
{
    [Theory]
    [InlineData(ReportJobStatus.Processing)]
    [InlineData(ReportJobStatus.Completed)]
    [InlineData(ReportJobStatus.Failed)]
    public async Task NonPendingJobsCannotGenerateDuplicateReports(ReportJobStatus status)
    {
        var (db, job) = await CreateJobAsync(status);
        await using (db)
        {
            var generator = new FakeGenerator();
            var storage = new FakeStorage();
            await new ReportJobProcessor(db, generator, storage,
                NullLogger<ReportJobProcessor>.Instance).ProcessAsync(job.Id);

            Assert.Equal(0, generator.Calls);
            Assert.Equal(0, storage.Uploads);
            Assert.Empty(await db.Reports.ToListAsync());
        }
    }

    [Fact]
    public async Task PendingJobCreatesOneVersionEvenWhenQueueMessageIsRepeated()
    {
        var (db, job) = await CreateJobAsync(ReportJobStatus.Pending);
        await using (db)
        {
            var generator = new FakeGenerator();
            var storage = new FakeStorage();
            var processor = new ReportJobProcessor(db, generator, storage,
                NullLogger<ReportJobProcessor>.Instance);
            await processor.ProcessAsync(job.Id);
            await processor.ProcessAsync(job.Id);

            Assert.Equal(1, generator.Calls);
            Assert.Equal(1, storage.Uploads);
            Assert.Equal(ReportJobStatus.Completed, job.Status);
            Assert.Equal(1, Assert.Single(await db.Reports.ToListAsync()).Version);
        }
    }

    private static async Task<(VistoraDbContext Db, ReportJob Job)> CreateJobAsync(ReportJobStatus status)
    {
        var organizationId = Guid.NewGuid();
        var db = new VistoraDbContext(new DbContextOptionsBuilder<VistoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new TestTenantContext(organizationId));
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = organizationId,
            PropertyId = Guid.NewGuid(), Identifier = "101" };
        var inspection = new Inspection { Id = Guid.NewGuid(), OrganizationId = organizationId,
            UnitId = unit.Id, Status = InspectionStatus.Completed, Type = InspectionType.MoveIn };
        var job = new ReportJob { Id = Guid.NewGuid(), OrganizationId = organizationId,
            InspectionId = inspection.Id, IdempotencyKey = Guid.NewGuid().ToString(),
            Status = status, MaxAttempts = 3 };
        db.Units.Add(unit);
        db.Inspections.Add(inspection);
        db.ReportJobs.Add(job);
        await db.SaveChangesAsync();
        return (db, job);
    }

    private sealed class TestTenantContext(Guid organizationId) : ITenantContext
    {
        public Guid? OrganizationId => organizationId;
    }

    private sealed class FakeGenerator : IReportPdfGenerator
    {
        public int Calls { get; private set; }
        public Task<GeneratedReportPdf> GenerateAsync(Guid inspectionId, CancellationToken cancellationToken = default)
        {
            Calls++;
            byte[] bytes = [1, 2, 3];
            return Task.FromResult(new GeneratedReportPdf(bytes, Convert.ToHexStringLower(SHA256.HashData(bytes))));
        }
    }

    private sealed class FakeStorage : IPrivateObjectStorage
    {
        public int Uploads { get; private set; }
        public Task UploadAsync(StorageUpload upload, CancellationToken cancellationToken = default)
        {
            Uploads++;
            return Task.CompletedTask;
        }
        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<byte[]> DownloadAsync(string objectKey, long maxBytes, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Uri CreateDownloadUrl(string objectKey, TimeSpan lifetime) => throw new NotSupportedException();
    }
}
