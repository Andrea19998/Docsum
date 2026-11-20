using Docsum.Application.Tests.Fakes;
using Docsum.Application.Documents;
using Docsum.Domain;

namespace Docsum.Application.Tests.Documents;

public class DocumentServiceTests
{
    private readonly FakeDocumentRepository _documents = new();
    private readonly FakeFileStorage _storage = new();
    private readonly FakeFolderRepository _folders = new();
    private readonly DocumentService _sut;

    public DocumentServiceTests()
    {
        _sut = new DocumentService(_documents, _storage, _folders);
    }

    private static MemoryStream Pdf() => new("%PDF-1.7 fake content"u8.ToArray());
    private static int _sampleUserId = 1;
    private const int _otherUserId = 2;

    [Fact]
    public async Task Upload_ValidPdf_SavesFileAndDocument()
    {
        Stream samplePdf = Pdf();
        var result = await _sut.UploadAsync(
            _sampleUserId, "samplefile.pdf", samplePdf, samplePdf.Length, null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(_sampleUserId, _documents.Items[0].UserId);
        Assert.Single(_documents.Items);
        Assert.Single(_storage.Files);
    }

    [Fact]
    public async Task Upload_NotAPdf_Fails_AndSavesNothing()
    {
        Stream sampleTxt = new MemoryStream("Fake content"u8.ToArray());
        var result = await _sut.UploadAsync(
            _sampleUserId, "samplefile.pdf", sampleTxt, sampleTxt.Length, null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Empty(_documents.Items);
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task Upload_PathTraversalInFileName_IsStripped()
    {
        Stream sampleExploit = Pdf();
        var result = await _sut.UploadAsync(
            _sampleUserId, "../../sampleexploit.pdf", sampleExploit, sampleExploit.Length, null,
            CancellationToken.None);

        Assert.True(result.Success);
        var doc = Assert.Single(_documents.Items);
        Assert.Equal("sampleexploit.pdf", doc.FileName);
    }

    [Fact]
    public async Task Upload_IntoFolder_AssignsFolder()
    {
        var folder = new Folder { Id = 7, UserId = _sampleUserId, Name = "Finanzamt" };
        _folders.Items.Add(folder);
        Stream samplePdf = Pdf();

        var result = await _sut.UploadAsync(
            _sampleUserId, "letter.pdf", samplePdf, samplePdf.Length, folder.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(folder.Id, Assert.Single(_documents.Items).FolderId);
    }

    [Fact]
    public async Task Upload_IntoUnknownFolder_Fails()
    {
        Stream samplePdf = Pdf();
        var result = await _sut.UploadAsync(
            _sampleUserId, "letter.pdf", samplePdf, samplePdf.Length, 999, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Empty(_documents.Items);
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task Get_DocumentOfAnotherUser_ReturnsNull()
    {
        Stream sampleData = Pdf();
        var upload = await _sut.UploadAsync(
            _sampleUserId, "samplefile.pdf", sampleData, sampleData.Length, null, CancellationToken.None);
        var docId = upload.Value!.Id;

        var asOtherUser = await _sut.GetAsync(_otherUserId, docId, CancellationToken.None);
        Assert.Null(asOtherUser);

        var asOwner = await _sut.GetAsync(_sampleUserId, docId, CancellationToken.None);
        Assert.NotNull(asOwner);
    }

    [Fact]
    public async Task List_ReturnsOnlyOwnDocuments()
    {
        _documents.Items.Add(new Document { Id = 1, UserId = _sampleUserId, FileName = "a.pdf" });
        _documents.Items.Add(new Document { Id = 2, UserId = _otherUserId, FileName = "b.pdf" });
        _documents.Items.Add(new Document { Id = 3, UserId = _otherUserId, FileName = "c.pdf" });

        var mine = await _sut.ListAsync(_sampleUserId, CancellationToken.None);
        var their = await _sut.ListAsync(_otherUserId, CancellationToken.None);

        Assert.Single(mine);
        Assert.Equal(2, their.Count);
    }

    [Fact]
    public async Task Move_IntoFolder_UpdatesFolderId()
    {
        var folder = new Folder { Id = 4, UserId = _sampleUserId, Name = "Finanzamt" };
        _folders.Items.Add(folder);
        Stream data = Pdf();
        var upload = await _sut.UploadAsync(
            _sampleUserId, "letter.pdf", data, data.Length, null, CancellationToken.None);

        var result = await _sut.MoveAsync(
            _sampleUserId, upload.Value!.Id, folder.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(folder.Id, _documents.Items[0].FolderId);
    }

    [Fact]
    public async Task Move_ToForeignFolder_Fails()
    {
        var foreignFolder = new Folder { Id = 5, UserId = _otherUserId, Name = "Private" };
        _folders.Items.Add(foreignFolder);
        Stream data = Pdf();
        var upload = await _sut.UploadAsync(
            _sampleUserId, "letter.pdf", data, data.Length, null, CancellationToken.None);

        var result = await _sut.MoveAsync(
            _sampleUserId, upload.Value!.Id, foreignFolder.Id, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Null(_documents.Items[0].FolderId);
    }

    [Fact]
    public async Task Delete_RemovesDocumentAndStoredFile()
    {
        Stream data = Pdf();
        var upload = await _sut.UploadAsync(
            _sampleUserId, "letter.pdf", data, data.Length, null, CancellationToken.None);
        var storedName = _documents.Items[0].StoredFileName;

        var deleted = await _sut.DeleteAsync(
            _sampleUserId, upload.Value!.Id, CancellationToken.None);

        Assert.True(deleted);
        Assert.Empty(_documents.Items);
        Assert.False(_storage.Files.ContainsKey(storedName));
    }
}
