namespace Vistora.Domain;

public static class InspectionWorkflow
{
    public static bool CanEdit(InspectionStatus status) => status == InspectionStatus.Draft;

    public static bool CanApprove(InspectionStatus status, bool hasReport) =>
        status == InspectionStatus.Completed && hasReport;
}
