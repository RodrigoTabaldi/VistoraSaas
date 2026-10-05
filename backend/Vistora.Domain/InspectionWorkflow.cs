namespace Vistora.Domain;

public static class InspectionWorkflow
{
    public static bool IsAnswered(string? response) => response is "Conforme" or "Atenção" or "Não conforme";
    public static bool IsValidResponse(string? response) => string.IsNullOrEmpty(response) || response == "Não verificado" || IsAnswered(response);
    public static bool CanComplete(int totalItems, int answeredItems) => totalItems > 0 && totalItems == answeredItems;

    public static bool CanEdit(InspectionStatus status) => status == InspectionStatus.Draft;

    public static bool CanApprove(InspectionStatus status, bool hasReport) =>
        status == InspectionStatus.Completed && hasReport;

    public static bool CanApprove(InspectionStatus status, bool hasReport, bool hasAcceptance) =>
        CanApprove(status, hasReport) && hasAcceptance;
}
