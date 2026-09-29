using Vistora.Domain;
using Xunit;

namespace Vistora.Tests;

public sealed class InspectionWorkflowTests
{
    [Theory]
    [InlineData(InspectionStatus.Draft, true)]
    [InlineData(InspectionStatus.Completed, false)]
    [InlineData(InspectionStatus.Approved, false)]
    public void OnlyDraftInspectionsCanBeEdited(InspectionStatus status, bool expected)
    {
        Assert.Equal(expected, InspectionWorkflow.CanEdit(status));
    }

    [Theory]
    [InlineData(InspectionStatus.Draft, false, false)]
    [InlineData(InspectionStatus.Draft, true, false)]
    [InlineData(InspectionStatus.Completed, false, false)]
    [InlineData(InspectionStatus.Completed, true, true)]
    [InlineData(InspectionStatus.Approved, true, false)]
    public void ApprovalRequiresACompletedInspectionAndReport(InspectionStatus status, bool hasReport, bool expected)
    {
        Assert.Equal(expected, InspectionWorkflow.CanApprove(status, hasReport));
    }
}
