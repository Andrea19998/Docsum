using Docsum.Application.Abstractions;
using Docsum.Infrastructure.Ai;
using Docsum.Infrastructure.Persistence;
using Docsum.Infrastructure.Processing;
using Docsum.Infrastructure.Security;
using Docsum.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Docsum.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string storagePath,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IFolderRepository, FolderRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddSingleton<IFileStorage>(new LocalFileStorage(storagePath));

        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        var ai = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
        services.AddHttpClient<IChatCompletion, OpenAiChatCompletion>(
            c => c.Timeout = TimeSpan.FromSeconds(ai.TimeoutSeconds > 0 ? ai.TimeoutSeconds : 120));

        services.AddSingleton<IDocumentTextExtractor, PdfPigTextExtractor>();
        services.AddTransient<IDocumentSummarizer, AiDocumentSummarizer>();
        services.AddHostedService<DocumentProcessingWorker>();

        return services;
    }
}
