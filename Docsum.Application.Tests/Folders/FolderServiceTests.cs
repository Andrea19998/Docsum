using Docsum.Application.Folders;
using Docsum.Application.Tests.Fakes;

namespace Docsum.Application.Tests.Folders;

public class FolderServiceTests
{
    private readonly FakeFolderRepository _folders = new();
    private readonly FolderService _sut;

    private const int UserId = 1;
    private const int OtherUserId = 2;

    public FolderServiceTests()
    {
        _sut = new FolderService(_folders);
    }

    private async Task<int> Create(string name, int? parentId = null, int userId = UserId)
    {
        var result = await _sut.CreateAsync(
            userId, new CreateFolderRequest(name, parentId), CancellationToken.None);
        Assert.True(result.Success);
        return result.Value!.Id;
    }

    [Fact]
    public async Task Create_AtRoot_Succeeds()
    {
        var id = await Create("Finanzamt");

        var folder = Assert.Single(_folders.Items);
        Assert.Equal("Finanzamt", folder.Name);
        Assert.Null(folder.ParentFolderId);
        Assert.Equal(id, folder.Id);
    }

    [Fact]
    public async Task Create_DuplicateSiblingName_IsRejectedCaseInsensitively()
    {
        await Create("Finanzamt");

        var result = await _sut.CreateAsync(
            UserId, new CreateFolderRequest("finanzamt", null), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Single(_folders.Items);
    }

    [Fact]
    public async Task Create_SameNameInDifferentParents_IsAllowed()
    {
        var a = await Create("A");
        var b = await Create("B");

        await Create("Letters", a);
        var result = await _sut.CreateAsync(
            UserId, new CreateFolderRequest("Letters", b), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(4, _folders.Items.Count);
    }

    [Fact]
    public async Task Create_UnderForeignParent_IsRejected()
    {
        var foreign = await Create("Foreign", userId: OtherUserId);

        var result = await _sut.CreateAsync(
            UserId, new CreateFolderRequest("Letters", foreign), CancellationToken.None);

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_EmptyName_IsRejected(string name)
    {
        var result = await _sut.CreateAsync(
            UserId, new CreateFolderRequest(name, null), CancellationToken.None);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Update_Rename_Succeeds()
    {
        var id = await Create("Old");

        var result = await _sut.UpdateAsync(
            UserId, id, new UpdateFolderRequest("New", null), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("New", _folders.Items.Single().Name);
    }

    [Fact]
    public async Task Update_MoveIntoOwnDescendant_IsRejected()
    {
        var a = await Create("A");
        var b = await Create("B", a);
        var c = await Create("C", b);

        var result = await _sut.UpdateAsync(
            UserId, a, new UpdateFolderRequest("A", c), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("itself", result.Error);
    }

    [Fact]
    public async Task Update_MoveIntoItself_IsRejected()
    {
        var id = await Create("A");

        var result = await _sut.UpdateAsync(
            UserId, id, new UpdateFolderRequest("A", id), CancellationToken.None);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Delete_MovesChildrenToParent()
    {
        var parent = await Create("Parent");
        var child = await Create("Child", parent);
        var grandchild = await Create("Grandchild", child);

        var deleted = await _sut.DeleteAsync(UserId, child, CancellationToken.None);

        Assert.True(deleted);
        Assert.DoesNotContain(_folders.Items, f => f.Id == child);
        Assert.Equal(parent, _folders.Items.Single(f => f.Id == grandchild).ParentFolderId);
    }

    [Fact]
    public async Task Delete_ForeignFolder_ReturnsFalse()
    {
        var id = await Create("Mine");

        var deleted = await _sut.DeleteAsync(OtherUserId, id, CancellationToken.None);

        Assert.False(deleted);
        Assert.Single(_folders.Items);
    }
}
