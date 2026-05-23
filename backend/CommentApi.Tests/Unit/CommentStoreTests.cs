using CommentApi.Data;
using CommentApi.Models;

namespace CommentApi.Tests.Unit;

public class CommentStoreTests
{
    [Fact]
    public void GetAll_ReturnsSeededComments()
    {
        var store = new CommentStore();

        Assert.NotEmpty(store.GetAll());
    }

    [Fact]
    public void GetAll_AllCommentsHaveRequiredFields()
    {
        var store = new CommentStore();

        foreach (var c in store.GetAll())
        {
            Assert.NotEqual(0, c.Id);
            Assert.False(string.IsNullOrWhiteSpace(c.AuthorId), $"Comment {c.Id} has empty AuthorId");
            Assert.False(string.IsNullOrWhiteSpace(c.AuthorName), $"Comment {c.Id} has empty AuthorName");
            Assert.False(string.IsNullOrWhiteSpace(c.Content), $"Comment {c.Id} has empty Content");
            Assert.NotEqual(default, c.CreatedAt);
        }
    }

    [Fact]
    public void GetAll_IncludesSoftDeletedComment()
    {
        // The seed should exercise the IsDeleted case so downstream behaviour (e.g. "[deleted]"
        // rendering, replies-survive-parent-delete) has data to act on.
        var store = new CommentStore();

        Assert.Contains(store.GetAll(), c => c.IsDeleted);
    }

    [Fact]
    public void GetAll_IncludesReply()
    {
        // The seed should exercise the reply case (ParentId != null) for the same reason.
        var store = new CommentStore();

        Assert.Contains(store.GetAll(), c => c.ParentId != null);
    }

    [Fact]
    public void GetAll_EachAuthorIdMapsToExactlyOneName()
    {
        // Integrity invariant: one identity (AuthorId) must always carry the same display name.
        // Catches mock-data drift where the same AuthorId ends up paired with two different
        // AuthorName values — which would be contradictory data.
        var store = new CommentStore();

        var conflicts = store.GetAll()
            .GroupBy(c => c.AuthorId)
            .Where(g => g.Select(c => c.AuthorName).Distinct().Count() > 1)
            .Select(g => $"{g.Key} -> [{string.Join(", ", g.Select(c => c.AuthorName).Distinct())}]")
            .ToList();

        Assert.True(conflicts.Count == 0,
            $"AuthorId(s) map to multiple names: {string.Join("; ", conflicts)}");
    }

    [Fact]
    public void Add_NewComment_AppendsToStore()
    {
        var store = new CommentStore();
        var before = store.GetAll().Count;
        store.Add(new Comment
        {
            AuthorId = "55555555-5555-5555-5555-555555555555",
            AuthorName = "Anh",
            Content = "This is a new comment.",
        });
        Assert.Equal(before + 1, store.GetAll().Count);
    }

    [Fact]
    public void Add_ReplyWithParentId_PreservesParentId()
    {
        var store = new CommentStore();
        var parentComment = store.Add(new Comment
        {
            AuthorId = "55555555-5555-5555-5555-555555555555",
            AuthorName = "Anh",
            Content = "This is top-level comment.",
        });
        Assert.NotNull(parentComment);

        var replyComment = store.Add(new Comment
        {
            AuthorId = "55555555-5555-5555-5555-555555555555",
            AuthorName = "Anh",
            Content = "This is a reply.",
            ParentId = parentComment.Id,
        });
        Assert.NotNull(replyComment);
        Assert.Equal(parentComment.Id, replyComment.ParentId);
    }
}
