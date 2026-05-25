using CommentApi.Models;

namespace CommentApi.Data;

public class CommentStore
{
    private readonly CommentDbContext _db;

    public CommentStore(CommentDbContext dbContext)
    {
        _db = dbContext;
    }

    public IReadOnlyList<Comment> GetAll() => _db.Comments.ToList();

    public Comment Add(Comment input)
    {
        var created = new Comment
        {
            AuthorId = input.AuthorId,
            AuthorName = input.AuthorName,
            Content = input.Content,
            CreatedAt = DateTimeOffset.UtcNow,
            ParentId = input.ParentId,
            UpdatedAt = null,
            IsDeleted = false,
        };
        _db.Comments.Add(created);
        _db.SaveChanges();
        return created;
    }

    // Persistence only. Authorization (owner + 5-minute window) lives in
    // CommentAuthorization.CheckCanModify and is enforced by the controller.
    public bool SoftDelete(int id)
    {
        var comment = _db.Comments.FirstOrDefault(c => c.Id == id);
        if (comment == null) return false;
        comment.IsDeleted = true;
        _db.SaveChanges();
        return true;
    }

    public Comment? EditComment(int id, string newContent)
    {
        var comment = _db.Comments.FirstOrDefault(c => c.Id == id);
        if (comment == null) return null;
        comment.Content = newContent;
        comment.UpdatedAt = DateTimeOffset.UtcNow;
        _db.SaveChanges();
        return comment;
    }
}
