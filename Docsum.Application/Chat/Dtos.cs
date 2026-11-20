namespace Docsum.Application.Chat;

public record ChatMessageDto(int Id, string Role, string Content, DateTime CreatedAt);
public record AskRequest(string Question);
public record ChatExchangeDto(ChatMessageDto User, ChatMessageDto Assistant);

public enum ChatError { None, NotFound, Invalid, NotReady, AiUnavailable }

public record ChatOutcome(ChatError Error, string? Message, ChatExchangeDto? Value)
{
    public bool Success => Error == ChatError.None;

    public static ChatOutcome Ok(ChatExchangeDto value) => new(ChatError.None, null, value);
    public static ChatOutcome NotFound() => new(ChatError.NotFound, "Document not found.", null);
    public static ChatOutcome Invalid(string message) => new(ChatError.Invalid, message, null);
    public static ChatOutcome NotReady(string message) => new(ChatError.NotReady, message, null);
    public static ChatOutcome AiUnavailable(string message) =>
        new(ChatError.AiUnavailable, message, null);
}
