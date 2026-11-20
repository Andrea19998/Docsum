using System.Text;
using Docsum.Application.Abstractions;
using UglyToad.PdfPig;

namespace Docsum.Infrastructure.Processing;

public class PdfPigTextExtractor : IDocumentTextExtractor
{
    public Task<string> ExtractAsync(Stream pdf, CancellationToken ct)
    {
        var sb = new StringBuilder();

        using var document = PdfDocument.Open(pdf);
        foreach (var page in document.GetPages())
        {
            ct.ThrowIfCancellationRequested();
            sb.AppendLine(page.Text);
        }

        return Task.FromResult(sb.ToString().Trim());
    }
}
