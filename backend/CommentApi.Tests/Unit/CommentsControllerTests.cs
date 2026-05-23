using CommentApi.Controllers;
using CommentApi.Data;

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
}
