using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using EnterpriseDocumentIntelligence.BuildingBlocks.Security;

namespace EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.Configure<ServiceBusOptions>(configuration.GetSection("ServiceBus"));
        services.AddSingleton<IMessagePublisher, ServiceBusPublisher>();

        var connection = configuration.GetConnectionString("SqlServer");
        if (!string.IsNullOrWhiteSpace(connection))
            services.AddDbContext<DocumentDbContext>(o => o.UseSqlServer(connection, sql => sql.EnableRetryOnFailure(5)));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration, "AzureAd");
        services.AddAuthorization(options =>
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().Build());

        services.AddOpenTelemetry()
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter());

        return services;
    }

    public static void UseBuildingBlocks(this WebApplication app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
    }
}