using Docsum.Application.Chat;
using Docsum.Application.Tests.Fakes;
using Docsum.Domain;

namespace Docsum.Application.Tests.Chat;

public class ChatServiceTests
{
    private readonly FakeDocumentRepository _documents = new();
    private readonly FakeChatMessageRepository _messages = new();
    private readonly FakeChatCompletion _completion = new();
    private readonly ChatService _sut;

    private const int UserId = 1;
    private const int OtherUserId = 2;

    public ChatServiceTests()
    {
        _sut = new ChatService(_documents, _messages, _completion);
    }

    private Document AddDocument(int userId = UserId, DocumentStatus status = DocumentStatus.Done) =>
        AddDocumentWith(new Document
        {
            Id = _documents.Items.Count + 1,
            UserId = userId,
            FileName = "letter.pdf",
            StoredFileName = "stored.pdf",
            ExtractedText = "Zahlungsfrist: 30.11.2026",
            Status = status
        });

    private Document AddDocumentWith(Document document)
    {
        _documents.Items.Add(document);
        return document;
    }

    [Fact]
    public async Task Ask_WithAnswer_SavesBothMessages()
    {
        var doc = AddDocument();

        var outcome = await _sut.AskAsync(
            UserId, doc.Id, new AskRequest("What is the deadline?"), CancellationToken.None);

        Assert.True(outcome.Success);
        Assert.Equal("The deadline is 30.11.2026.", outcome.Value!.Assistant.Content);
        Assert.Equal(2, _messages.Items.Count);
        Assert.Equal(ChatRole.User, _messages.Items[0].Role);
        Assert.Equal(ChatRole.Assistant, _messages.Items[1].Role);
    }

    [Fact]
    public async Task Ask_PassesLetterTextToTheModel()
    {
        var doc = AddDocument();

        await _sut.AskAsync(
            UserId, doc.Id, new AskRequest("What is the deadline?"), CancellationToken.None);

        var call = Assert.Single(_completion.Calls);
        Assert.Contains("Zahlungsfrist", call.SystemPrompt);
        Assert.Equal("What is the deadline?", call.Messages[^1].Content);
    }

    [Fact]
    public async Task Ask_WhenAiNotConfigured_ReturnsUnavailable_AndSavesNothing()
    {
        _completion.IsConfigured = false;
        var doc = AddDocument();

        var outcome = await _sut.AskAsync(
            UserId, doc.Id, new AskRequest("Question?"), CancellationToken.None);

        Assert.Equal(ChatError.AiUnavailable, outcome.Error);
        Assert.Empty(_messages.Items);
    }

    [Fact]
    public async Task Ask_WhileProcessing_ReturnsNotReady()
    {
        var doc = AddDocument(status: DocumentStatus.Processing);

        var outcome = await _sut.AskAsync(
            UserId, doc.Id, new AskRequest("Question?"), CancellationToken.None);

        Assert.Equal(ChatError.NotReady, outcome.Error);
    }

    [Fact]
    public async Task Ask_EmptyQuestion_ReturnsInvalid()
    {
        var doc = AddDocument();

        var outcome = await _sut.AskAsync(
            UserId, doc.Id, new AskRequest("   "), CancellationToken.None);

        Assert.Equal(ChatError.Invalid, outcome.Error);
    }

    [Fact]
    public async Task Ask_ForeignDocument_ReturnsNotFound()
    {
        var doc = AddDocument(userId: OtherUserId);

        var outcome = await _sut.AskAsync(
            UserId, doc.Id, new AskRequest("Question?"), CancellationToken.None);

        Assert.Equal(ChatError.NotFound, outcome.Error);
    }

    [Fact]
    public async Task History_ReturnsSavedMessages()
    {
        var doc = AddDocument();
        await _sut.AskAsync(UserId, doc.Id, new AskRequest("Q1"), CancellationToken.None);

        var history = await _sut.HistoryAsync(UserId, doc.Id, CancellationToken.None);

        Assert.NotNull(history);
        Assert.Equal(2, history!.Count);
        Assert.Equal("user", history[0].Role);
        Assert.Equal("assistant", history[1].Role);
    }

    [Fact]
    public async Task History_ForeignDocument_ReturnsNull()
    {
        var doc = AddDocument(userId: OtherUserId);

        var history = await _sut.HistoryAsync(UserId, doc.Id, CancellationToken.None);

        Assert.Null(history);
    }
}
