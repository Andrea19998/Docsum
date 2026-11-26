using Docsum.Application.Abstractions;

namespace Docsum.Infrastructure.Ai;

public class AiDocumentSummarizer(IChatCompletion chat) : IDocumentSummarizer
{
    private const int MaxChars = 15000;

    public bool IsConfigured => chat.IsConfigured;

    public async Task<string?> SummarizeAsync(string text, CancellationToken ct)
    {
        if (!chat.IsConfigured || string.IsNullOrWhiteSpace(text)) return null;

        var truncated = text.Length <= MaxChars ? text : text[..MaxChars];
        const string system =
            "You summarize German bureaucratic letters for a private person who is not a lawyer. " +
            "Write a short plain-language summary in English covering: (1) what this letter is, " +
            "(2) what action the recipient must take, if any, (3) any deadline, amount to pay, or " +
            "documents to send. Use at most 5 short bullet points. Do not invent facts that are not " +
            "in the letter. Do not give legal advice.";

        var answer = await chat.CompleteAsync(
            system, [new ChatTurn("user", truncated)], ct);

        return string.IsNullOrWhiteSpace(answer) ? null : answer.Trim();
    }
}
