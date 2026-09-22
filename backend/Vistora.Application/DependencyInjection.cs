using Microsoft.Extensions.DependencyInjection;
using Vistora.Application.Reporting;
using Vistora.Application.UseCases;

namespace Vistora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddVistoraApplication(this IServiceCollection services)
    {
        services.AddScoped<CompleteInspectionUseCase>();
        services.AddScoped<ReportJobProcessor>();
        services.AddScoped<CreateChecklistTemplateUseCase>();
        services.AddScoped<CreateInspectionFromTemplateUseCase>();
        return services;
    }
}
