using CommentApi.Data;
using CommentApi.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CommentApi.Tests.Unit;

public class CommentStoreTests: IDisposable
{
    private const string _authorId = "55555555-5555-5555-5555-555555555555";
    private const string _authorName = "Anh";
    private readonly CommentStore _store;
    private readonly CommentDbContext _db;
    private readonly SqliteConnection _connection;

    public CommentStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<CommentDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new CommentDbContext(options);
        _db.Database.EnsureCreated();
        _store = new CommentStore(_db);
    }

    [Fact]
    public void GetAll_AfterSoftDelete_StillReturnsTheRow()
    {
        // Soft delete is a flag flip, not a row removal — the row must remain visible
        // in GetAll so replies under a deleted parent still render.
        var added = _store.Add(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "Soft delete should not remove from GetAll",
        });
        _store.SoftDelete(added.Id);

        Assert.Contains(_store.GetAll(), c => c.Id == added.Id && c.IsDeleted);
    }

    [Fact]
    public void Add_NewComment_AppendsToStore()
    {
        var before = _store.GetAll().Count;
        _store.Add(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "This is a new comment.",
        });
        Assert.Equal(before + 1, _store.GetAll().Count);
    }

    [Fact]
    public void Add_ReplyWithParentId_PreservesParentId()
    {
        var parentComment = _store.Add(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "This is top-level comment.",
        });
        Assert.NotNull(parentComment);

        var replyComment = _store.Add(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "This is a reply.",
            ParentId = parentComment.Id,
        });
        Assert.NotNull(replyComment);
        Assert.Equal(parentComment.Id, replyComment.ParentId);
    }

    [Fact]
    public void SoftDelete_ExistingComment_SetsIsDeletedTrue()
    {
        var added = _store.Add(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "Will be deleted.",
        });
        var deleted = _store.SoftDelete(added.Id);
        Assert.True(deleted);
        Assert.True(_store.GetAll().First(c => c.Id == added.Id).IsDeleted);
    }

    [Fact]
    public void SoftDelete_NonExistentId_ReturnsFalseAndChangesNothing()
    {
        var before = _store.GetAll().Count(c => c.IsDeleted);
        var result = _store.SoftDelete(int.MaxValue);   // id doesn't exist
        Assert.False(result);
        Assert.Equal(before, _store.GetAll().Count(c => c.IsDeleted));
    }

    [Fact]
    public void SoftDelete_AlreadyDeletedComment_RemainsDeleted()
    {
        var added = _store.Add(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "Will be deleted twice.",
        });
        var firstDelete = _store.SoftDelete(added.Id);
        Assert.True(firstDelete);
        Assert.True(_store.GetAll().First(c => c.Id == added.Id).IsDeleted);
        var secondDelete = _store.SoftDelete(added.Id);
        Assert.True(secondDelete);
        Assert.True(_store.GetAll().First(c => c.Id == added.Id).IsDeleted);
    }

    [Fact]
    public void SoftDelete_AuthorHasMultipleComments_OnlyTargetedCommentIsDeleted()
    {
        // Authorization is tested separately in CommentAuthorizationTests; 
        // this test only proves the store mutates the row identified by id, not some other row owned by the same author.
        var keep = _store.Add(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "keep me",
        });
        var drop = _store.Add(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "delete me",
        });

        var ok = _store.SoftDelete(drop.Id);

        Assert.True(ok);
        Assert.True(_store.GetAll().Single(c => c.Id == drop.Id).IsDeleted);
        Assert.False(_store.GetAll().Single(c => c.Id == keep.Id).IsDeleted);
    }

    [Fact]
    public void Edit_ExistingComment_UpdatesContent()
    {
        var added = _store.Add(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "Original content.",
        });
        var newContent = "Updated content.";
        var updated = _store.EditComment(added.Id, newContent);
        Assert.NotNull(updated);
        Assert.Equal(newContent, updated.Content);
        Assert.Equal(added.Id, updated.Id);
    }

    [Fact]
    public void Edit_NonExistentId_ReturnsNullAndChangesNothing()
    {
        var before = _store.GetAll().Select(c => c.Content).ToList();
        var result = _store.EditComment(int.MaxValue, "New content");   // id doesn't exist
        Assert.Null(result);
        Assert.Equal(before, _store.GetAll().Select(c => c.Content).ToList());
    }

    [Fact]
    public void Edit_AuthorHasMultipleComments_OnlyTargetedCommentIsEdited()
    {
        // Similar to SoftDelete_AuthorHasMultipleComments_OnlyTargetedCommentIsDeleted, this test proves EditComment mutates the row identified by id, not some other row owned by the same author.
        var keepComment = _store.Add(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "keep comment",
        });
        var editComment = _store.Add(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "edit comment",
        });
        var newContent = "edited content";

        var updated = _store.EditComment(editComment.Id, newContent);

        Assert.NotNull(updated);
        Assert.Equal(newContent, updated.Content);
        Assert.Equal("keep comment", _store.GetAll().Single(c => c.Id == keepComment.Id).Content);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
