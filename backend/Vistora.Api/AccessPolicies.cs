using Microsoft.AspNetCore.Authorization;

namespace Vistora.Api;

public static class AccessPolicies
{
    public const string ManageOrganization = "manage-organization";
    public const string EditInspection = "edit-inspection";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(ManageOrganization, policy => policy.RequireRole("Admin"));
        options.AddPolicy(EditInspection, policy => policy.RequireRole("Admin", "Vistoriador"));
    }
}
