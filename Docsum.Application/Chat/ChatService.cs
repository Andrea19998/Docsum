using Docsum.Application.Abstractions;
using Docsum.Domain;

namespace Docsum.Application.Chat;

public class ChatService(
    IDocumentRepository documents,
    IChatMessageRepository messages,
    IChatCompletion completion)
{
    private const int MaxQuestionLength = 2000;
    private const int MaxContextChars = 15000;
    private const int HistoryTurns = 10;

    public async Task<List<ChatMessageDto>?> HistoryAsync(
        int userId, int documentId, CancellationToken ct)
    {
        if (await documents.GetAsync(userId, documentId, ct) is null) return null;

        return (await messages.ListAsync(documentId, ct))
            .Select(m => new ChatMessageDto(
                m.Id,
                m.Role == ChatRole.User ? "user" : "assistant",
                m.Content,
                m.CreatedAt))
            .ToList();
    }

    public async Task<ChatOutcome> AskAsync(
        int userId, int documentId, AskRequest req, CancellationToken ct)
    {
        var document = await documents.GetAsync(userId, documentId, ct);
        if (document is null) return ChatOutcome.NotFound();

        var question = req.Question?.Trim() ?? string.Empty;
        if (question.Length == 0) return ChatOutcome.Invalid("Question is empty.");
        if (question.Length > MaxQuestionLength)
            return ChatOutcome.Invalid($"Question must be at most {MaxQuestionLength} characters.");

        if (document.Status != DocumentStatus.Done ||
            string.IsNullOrWhiteSpace(document.ExtractedText))
            return ChatOutcome.NotReady("This document is still being processed. Try again shortly.");

        if (!completion.IsConfigured)
            return ChatOutcome.AiUnavailable(
                "AI is not configured. Set Ai:BaseUrl, Ai:Model and Ai:ApiKey in user-secrets.");

        var history = await messages.ListAsync(documentId, ct);
        var turns = history
            .TakeLast(HistoryTurns)
            .Select(m => new ChatTurn(
                m.Role == ChatRole.User ? "user" : "assistant", m.Content))
            .Append(new ChatTurn("user", question))
            .ToList();

        string answer;
        try
        {
            answer = await completion.CompleteAsync(BuildSystemPrompt(document), turns, ct);
        }
        catch (Exception ex)
        {
            return ChatOutcome.AiUnavailable($"AI request failed: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(answer))
            return ChatOutcome.AiUnavailable("The AI returned an empty answer.");

        var userMessage = new ChatMessage
        {
            DocumentId = document.Id,
            Role = ChatRole.User,
            Content = question
        };
        var assistantMessage = new ChatMessage
        {
            DocumentId = document.Id,
            Role = ChatRole.Assistant,
            Content = answer.Trim()
        };
        await messages.AddAsync(userMessage, ct);
        await messages.AddAsync(assistantMessage, ct);

        return ChatOutcome.Ok(new ChatExchangeDto(
            new ChatMessageDto(userMessage.Id, "user", userMessage.Content, userMessage.CreatedAt),
            new ChatMessageDto(
                assistantMessage.Id, "assistant", assistantMessage.Content,
                assistantMessage.CreatedAt)));
    }

    internal static string BuildSystemPrompt(Document document)
    {
        var text = document.ExtractedText ?? string.Empty;
        if (text.Length > MaxContextChars)
            text = text[..MaxContextChars] + "\n[... text truncated ...]";

        var summary = string.IsNullOrWhiteSpace(document.Summary)
            ? "(no summary available)"
            : document.Summary;

        return "You are Docsum, an assistant that helps people understand German bureaucratic letters.\n" +
               "Answer questions using only the letter below. If the answer is not in the letter, say so\n" +
               "clearly instead of guessing. Be concise and practical. This is not legal advice.\n\n" +
               $"Document: {document.FileName}\n" +
               $"Summary: {summary}\n\n" +
               "Letter text:\n" + text;
    }
}
