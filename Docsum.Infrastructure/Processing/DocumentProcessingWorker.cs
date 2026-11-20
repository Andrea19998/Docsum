using Docsum.Application.Abstractions;
using Docsum.Domain;
using Docsum.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Docsum.Infrastructure.Processing;

/// <summary>
/// Extracts text (and, when configured, a summary) for newly uploaded documents.
/// Polling keeps uploads fast and survives restarts; a queue can replace it later.
/// </summary>
public class DocumentProcessingWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DocumentProcessingWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Document processing loop failed");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessPendingAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pending = await db.Documents
            .Where(d => d.Status == DocumentStatus.Uploaded)
            .OrderBy(d => d.Id)
            .Take(3)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var extractor = scope.ServiceProvider.GetRequiredService<IDocumentTextExtractor>();
        var summarizer = scope.ServiceProvider.GetRequiredService<IDocumentSummarizer>();

        foreach (var document in pending)
        {
            document.Status = DocumentStatus.Processing;
            await db.SaveChangesAsync(ct);

            try
            {
                await using var stream = await storage.OpenReadAsync(document.StoredFileName, ct);
                if (stream is null)
                    throw new FileNotFoundException("Stored PDF is missing.");

                var text = await extractor.ExtractAsync(stream, ct);
                document.ExtractedText = text;

                try
                {
                    document.Summary = await summarizer.SummarizeAsync(text, ct);
                }
                catch (Exception ex)
                {
                    // Extraction succeeded; a summary failure (quota, network) must not fail the document.
                    logger.LogWarning(ex, "Summary generation failed for document {Id}", document.Id);
                }

                document.Status = DocumentStatus.Done;
                document.ErrorMessage = null;
            }
            catch (Exception ex)
            {
                document.Status = DocumentStatus.Failed;
                document.ErrorMessage =
                    "Could not read this PDF. It may be scanned, encrypted, or corrupted.";
                logger.LogWarning(ex, "Text extraction failed for document {Id}", document.Id);
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
