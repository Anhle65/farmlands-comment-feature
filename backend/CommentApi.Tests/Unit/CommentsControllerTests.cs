using CommentApi.Controllers;
using CommentApi.Data;
using CommentApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommentApi.Tests.Unit;

public class CommentsControllerTests
{
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

        const string authorId = "55555555-5555-5555-5555-555555555555";
        const string authorName = "Anh";
        const string content = "This is a new comment.";

        // Act
        var action = controller.PostComment(new Comment
        {
            AuthorId = authorId,
            AuthorName = authorName,
            Content = content,
        });

        // Assert — unwrap the 201 Created result
        var result = AssertCreated(action);

        // Assert — side effect on the store
        Assert.Equal(before + 1, store.GetAll().Count);

        // Assert — returned comment carries input fields
        Assert.Equal(authorId, result.AuthorId);
        Assert.Equal(authorName, result.AuthorName);
        Assert.Equal(content, result.Content);

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
            AuthorId = "55555555-5555-5555-5555-555555555555",
            AuthorName = "Anh",
            Content = "Parent comment.",
        }));

        var before = store.GetAll().Count;

        const string authorId = "55555555-5555-5555-5555-555555555555";
        const string authorName = "Anh";
        const string content = "This is a reply.";

        // Act
        var action = controller.PostComment(new Comment
        {
            AuthorId = authorId,
            AuthorName = authorName,
            Content = content,
            ParentId = parent.Id,
        });
        var result = AssertCreated(action);

        // Assert — side effect on the store
        Assert.Equal(before + 1, store.GetAll().Count);

        // Assert — returned comment carries input fields
        Assert.Equal(authorId, result.AuthorId);
        Assert.Equal(authorName, result.AuthorName);
        Assert.Equal(content, result.Content);

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
            AuthorId = "55555555-5555-5555-5555-555555555555",
            AuthorName = "Anh",
            Content = "Reply to a parent that doesn't exist.",
            ParentId = -1, // intentionally not in the store
        });

        var bad = Assert.IsType<BadRequestObjectResult>(action.Result);
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
}
