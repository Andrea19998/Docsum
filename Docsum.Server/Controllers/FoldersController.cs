using System.Security.Claims;
using Docsum.Application.Folders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Docsum.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/folders")]
public class FoldersController(FolderService folders) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<FolderDto>>> List(CancellationToken ct) =>
        Ok(await folders.ListAsync(UserId, ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateFolderRequest req, CancellationToken ct)
    {
        var result = await folders.CreateAsync(UserId, req, ct);
        if (!result.Success) return BadRequest(new { error = result.Error });

        var folder = result.Value!;
        return Created($"/api/folders/{folder.Id}", folder);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateFolderRequest req, CancellationToken ct)
    {
        var result = await folders.UpdateAsync(UserId, id, req, ct);
        if (result.Success) return Ok(result.Value);

        return result.Error == "Folder not found."
            ? NotFound()
            : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await folders.DeleteAsync(UserId, id, ct) ? NoContent() : NotFound();
}
