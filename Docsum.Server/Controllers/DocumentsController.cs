using System.Security.Claims;
using Docsum.Application.Chat;
using Docsum.Application.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Docsum.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public class DocumentsController(DocumentService documents, ChatService chat) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<DocumentListItemDto>>> List(CancellationToken ct) =>
        Ok(await documents.ListAsync(UserId, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var doc = await documents.GetAsync(UserId, id, ct);
        return doc is null ? NotFound() : Ok(doc);
    }

    [HttpPost]
    [RequestSizeLimit(DocumentService.MaxSizeBytes + 1024 * 1024)]
    public async Task<IActionResult> Upload(
        IFormFile file, [FromForm] int? folderId, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var result = await documents.UploadAsync(
            UserId, file.FileName, stream, file.Length, folderId, ct);

        return result.Success
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Move(
        int id, MoveDocumentRequest req, CancellationToken ct)
    {
        var result = await documents.MoveAsync(UserId, id, req.FolderId, ct);
        if (result.Success) return Ok(result.Value);

        return result.Error == "Document not found."
            ? NotFound()
            : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await documents.DeleteAsync(UserId, id, ct) ? NoContent() : NotFound();

    [HttpPost("{id:int}/summarize")]
    public async Task<IActionResult> Summarize(int id, CancellationToken ct)
    {
        var outcome = await documents.SummarizeAsync(UserId, id, ct);
        if (outcome.Success) return Ok(outcome.Value);

        return outcome.Error switch
        {
            SummarizeError.NotFound => NotFound(),
            SummarizeError.NotReady => BadRequest(new { error = outcome.Message }),
            SummarizeError.AiUnavailable => StatusCode(503, new { error = outcome.Message }),
            _ => StatusCode(502, new { error = outcome.Message })
        };
    }

    [HttpGet("{id:int}/chat")]
    public async Task<IActionResult> ChatHistory(int id, CancellationToken ct)
    {
        var history = await chat.HistoryAsync(UserId, id, ct);
        return history is null ? NotFound() : Ok(history);
    }

    [HttpPost("{id:int}/chat")]
    public async Task<IActionResult> Ask(int id, AskRequest req, CancellationToken ct)
    {
        var outcome = await chat.AskAsync(UserId, id, req, ct);
        if (outcome.Success) return Ok(outcome.Value);

        return outcome.Error switch
        {
            ChatError.NotFound => NotFound(),
            ChatError.Invalid or ChatError.NotReady => BadRequest(new { error = outcome.Message }),
            ChatError.AiUnavailable => StatusCode(503, new { error = outcome.Message }),
            _ => StatusCode(500, new { error = "Unexpected error." })
        };
    }
}
