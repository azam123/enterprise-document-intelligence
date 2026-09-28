using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;

/// <summary>
/// Registers shared platform infrastructure used by the document intelligence services.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Registers shared persistence, messaging, security and telemetry services.
    /// </summary>
    /// <param name="services">Application dependency injection container.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="serviceName">Logical service name used for telemetry resource metadata.</param>
    /// <returns>The supplied service collection.</returns>
    public static IServiceCollection AddBuildingBlocks(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException(
                "Service name is required.",
                nameof(serviceName));
        }

        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUser, CurrentUser>();

        services
            .AddOptions<ServiceBusOptions>()
            .Bind(configuration.GetSection("ServiceBus"))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.ConnectionString) ||
                    !string.IsNullOrWhiteSpace(options.FullyQualifiedNamespace),
                "Either ServiceBus:ConnectionString or ServiceBus:FullyQualifiedNamespace must be configured.")
            .Validate(
                options => options.MaxConcurrentCalls is > 0 and <= 1000,
                "ServiceBus:MaxConcurrentCalls must be between 1 and 1000.")
            .ValidateOnStart();

        services.AddSingleton<IMessagePublisher, ServiceBusPublisher>();

        var sqlConnectionString = configuration.GetConnectionString("SqlServer");

        if (!string.IsNullOrWhiteSpace(sqlConnectionString))
        {
            services.AddDbContext<DocumentDbContext>(
                options =>
                    options.UseSqlServer(
                        sqlConnectionString,
                        sqlOptions => sqlOptions.EnableRetryOnFailure(5)));
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(
                configuration,
                "AzureAd");

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy =
                new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
        });

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService(serviceName.Trim()))
            .WithTracing(tracing =>
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter());

        return services;
    }

    /// <summary>
    /// Adds shared middleware and authentication/authorization components to the HTTP pipeline.
    /// </summary>
    /// <param name="application">ASP.NET Core application.</param>
    public static void UseBuildingBlocks(this WebApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        application.UseMiddleware<ExceptionMiddleware>();
        application.UseAuthentication();
        application.UseAuthorization();
    }
}