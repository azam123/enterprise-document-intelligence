using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using EnterpriseDocumentIntelligence.IngestionService.Application;
using EnterpriseDocumentIntelligence.IngestionService.Domain;
namespace EnterpriseDocumentIntelligence.IngestionService.Infrastructure;

public sealed class InMemoryIngestionJobStore : IIngestionJobStore { private readonly ConcurrentDictionary<Guid,IngestionJob> _jobs=new(); public Task<IngestionJob?> GetAsync(Guid id,CancellationToken ct=default)=>Task.FromResult(_jobs.TryGetValue(id,out var job)?job:null); public Task SaveAsync(IngestionJob job,CancellationToken ct=default){_jobs[job.Id]=job;return Task.CompletedTask;} }
public static class IngestionServiceInfrastructure { public static IServiceCollection AddIngestionApplication(this IServiceCollection services){ services.AddSingleton<IIngestionJobStore,InMemoryIngestionJobStore>(); services.AddScoped<IngestionApplication>(); return services; } }