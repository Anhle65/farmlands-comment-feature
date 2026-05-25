using CommentApi.Controllers;
using CommentApi.Data;
using CommentApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
namespace CommentApi.Tests.Unit;

public class CommentsControllerTests
{
    private string _authorId = "55555555-5555-5555-5555-555555555555";
    private string _authorName = "Anh";
    private string _notAuthorId = "00000000-0000-0000-0000-000000000000";
    private string _notAuthorName = "Someone";
    [Fact]
    public void GetComments_ReturnsAllStoreComments()
    {
        var store = new CommentStore();
        var controller = new CommentsController(store);

        var result = controller.GetComments().ToList();

        Assert.Equal(store.GetAll().Count, result.Count);
    }

    [Fact]
    public void GetComments_OrdersByCreatedAtDescending()
    {
        var controller = new CommentsController(new CommentStore());

        var result = controller.GetComments().ToList();

        for (var i = 0; i < result.Count - 1; i++)
        {
            Assert.True(
                result[i].CreatedAt >= result[i + 1].CreatedAt,
                $"Position {i} (CreatedAt={result[i].CreatedAt}) is not newer than position {i + 1} (CreatedAt={result[i + 1].CreatedAt})");
        }
    }

    [Fact]
    public void PostComment_ValidInput_AddsToStoreAndReturns201Created()
    {
        // Arrange
        var store = new CommentStore();
        var controller = new CommentsController(store);
        var before = store.GetAll().Count;

        // Act
        var action = controller.PostComment(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "This is a new comment.",
        });

        // Assert — unwrap the 201 Created result
        var result = AssertCreated(action);

        // Assert — side effect on the store
        Assert.Equal(before + 1, store.GetAll().Count);

        // Assert — returned comment carries input fields
        Assert.Equal(_authorId, result.AuthorId);
        Assert.Equal(_authorName, result.AuthorName);
        Assert.Equal("This is a new comment.", result.Content);

        // Assert — server-controlled fields are populated by the controller, not the client
        Assert.True(result.Id > 0, "Id should be assigned by the server");
        Assert.NotEqual(default(DateTimeOffset), result.CreatedAt);
        Assert.False(result.IsDeleted, "new comment should not be soft-deleted");
        Assert.Null(result.UpdatedAt);
        Assert.Null(result.ParentId);
    }

    [Fact]
    public void PostComment_ValidReply_AddsToStoreAndReturns201Created()
    {
        // Arrange
        var store = new CommentStore();
        var controller = new CommentsController(store);

        // Create the parent in this test so the assertion doesn't depend on the seed
        var parent = AssertCreated(controller.PostComment(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "Parent comment.",
        }));

        var before = store.GetAll().Count;

        // Act
        var action = controller.PostComment(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "This is a reply.",
            ParentId = parent.Id,
        });
        var result = AssertCreated(action);

        // Assert — side effect on the store
        Assert.Equal(before + 1, store.GetAll().Count);

        // Assert — returned comment carries input fields
        Assert.Equal(_authorId, result.AuthorId);
        Assert.Equal(_authorName, result.AuthorName);
        Assert.Equal("This is a reply.", result.Content);

        // Assert — server-controlled fields are populated by the controller, not the client
        Assert.True(result.Id > 0, "Id should be assigned by the server");
        Assert.NotEqual(default(DateTimeOffset), result.CreatedAt);
        Assert.False(result.IsDeleted, "new comment should not be soft-deleted");
        Assert.Null(result.UpdatedAt);
        Assert.Equal(parent.Id, result.ParentId);
    }

    [Fact]
    public void PostComment_ReplyToNonExistentParent_Returns400AndDoesNotAdd()
    {
        var store = new CommentStore();
        var controller = new CommentsController(store);
        var before = store.GetAll().Count;

        var action = controller.PostComment(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "Reply to a parent that doesn't exist.",
            ParentId = -1, // intentionally not in the store
        });

        var bad = Assert.IsType<BadRequestObjectResult>(action.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, bad.StatusCode);
        Assert.Contains("Parent", bad.Value?.ToString() ?? "");
        Assert.Equal(before, store.GetAll().Count);
    }

    // ── helper ─────────────────────────────────────────────────────────
    // Unwraps an ActionResult<Comment> that should be a 201 Created and returns
    // the underlying Comment. Fails the test with a clear message if not.
    private static Comment AssertCreated(ActionResult<Comment> action)
    {
        var created = Assert.IsType<CreatedAtActionResult>(action.Result);
        return Assert.IsType<Comment>(created.Value);
    }

    [Fact]
    public void DeleteComment_OwnerWithinWindow_Returns204AndSoftDeletes()
    {
        var store = new CommentStore();
        var controller = new CommentsController(store);

        // Create a comment via the real flow — fresh CreatedAt is automatically inside the window
        var added = AssertCreated(controller.PostComment(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "Will be deleted.",
        }));

        var result = controller.DeleteComment(added.Id, _authorId, _authorName);
        var status = Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, status.StatusCode);
        Assert.True(store.GetAll().First(c => c.Id == added.Id).IsDeleted);
    }

    [Fact]
    public void DeleteComment_NotOwner_Returns403AndDoesNotDelete()
    {
        var store = new CommentStore();
        var controller = new CommentsController(store);

        // Create a comment via the real flow — fresh CreatedAt is automatically inside the window
        var added = AssertCreated(controller.PostComment(new Comment
        {
            AuthorId = _authorId,
            AuthorName = _authorName,
            Content = "Will not be deleted.",
        }));

        var result = controller.DeleteComment(added.Id, _notAuthorId, _notAuthorName);
        var status = Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, status.StatusCode);
        Assert.False(store.GetAll().First(c => c.Id == added.Id).IsDeleted);
    }

    [Fact]
    public void DeleteComment_NonExistentId_Returns404AndChangesNothing()
    {
        // returning 204 for a non-existent
        // id would silently pass (flipping IsDeleted on nothing is a no-op).
        var store = new CommentStore();
        var controller = new CommentsController(store);
        var beforeDeleted = store.GetAll().Count(c => c.IsDeleted);

        var result = controller.DeleteComment(int.MaxValue, _authorId, _authorName);
        var status = Assert.IsAssignableFrom<IStatusCodeActionResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, status.StatusCode);
        Assert.Equal(beforeDeleted, store.GetAll().Count(c => c.IsDeleted));
    }
}

