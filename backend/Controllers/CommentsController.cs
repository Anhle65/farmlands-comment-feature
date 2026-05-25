using CommentApi.Authorization;
using CommentApi.Data;
using CommentApi.Models;
using CommentApi.Validation;
using Microsoft.AspNetCore.Mvc;

namespace CommentApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly CommentStore _store;

    public CommentsController(CommentStore store) => _store = store;

    /// <summary>Returns all comments, newest first.</summary>
    [HttpGet]
    public IEnumerable<Comment> GetComments() =>
        _store.GetAll().OrderByDescending(c => c.CreatedAt);

    /// <summary>
    /// Creates a new comment. Returns 201 Created with the new comment on success;
    /// 400 Bad Request with an error message when validation fails or the supplied
    /// <c>ParentId</c> does not point to an existing comment.
    /// </summary>
    [HttpPost]
    public ActionResult<Comment> PostComment(Comment input)
    {
        // 1. Structural validation (delegated)
        var error = CommentValidator.Validate(input);
        if (error is not null) return BadRequest(error);

        // 2. Referential check (store concern — knows what comments exist)
        if (input.ParentId.HasValue &&
            !_store.GetAll().Any(c => c.Id == input.ParentId.Value))
        {
            return BadRequest($"Parent comment with id {input.ParentId.Value} does not exist");
        }

        // 3. Persist — store stamps Id, CreatedAt, defaults
        var created = _store.Add(input);
        return CreatedAtAction(nameof(GetComments), value: created);
    }

    [HttpDelete("{id}")]
    public ActionResult DeleteComment(
        int id,
        [FromHeader(Name = "X-AuthorId")] string authorId,
        [FromHeader(Name = "X-AuthorName")] string authorName)
    {
        var comment = _store.GetAll().FirstOrDefault(c => c.Id == id);
        if (comment is null)
            return NotFound($"Comment with id {id} does not exist");

        if (!CommentAuthorization.CheckCanModify(comment, authorId, authorName, DateTimeOffset.UtcNow))
            return StatusCode(StatusCodes.Status403Forbidden);

        _store.SoftDelete(id);
        return NoContent();
    }

    [HttpPatch("{id}")]
    public ActionResult PatchComment(
        int id,
        [FromBody] Microsoft.AspNetCore.JsonPatch.SystemTextJson.JsonPatchDocument<Comment> patchDoc,
        [FromHeader(Name = "X-AuthorId")] string authorId,
        [FromHeader(Name = "X-AuthorName")] string authorName)
    {
        var comment = _store.GetAll().FirstOrDefault(c => c.Id == id);
        if (comment is null)
            return NotFound($"Comment with id {id} does not exist");

        if (!CommentAuthorization.CheckCanModify(comment, authorId, authorName, DateTimeOffset.UtcNow))
            return StatusCode(StatusCodes.Status403Forbidden);

        // Apply the patch to a copy of the comment to validate before mutating store data
        var commentCopy = new Comment
        {
            Id = comment.Id,
            AuthorId = comment.AuthorId,
            AuthorName = comment.AuthorName,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            ParentId = comment.ParentId,
            UpdatedAt = comment.UpdatedAt,
            IsDeleted = comment.IsDeleted,
        };
        patchDoc.ApplyTo(commentCopy);
        var error = CommentValidator.Validate(commentCopy);
        if (error is not null) return BadRequest(error);

        var updated = _store.EditComment(id, commentCopy.Content);
        return Ok(updated); 
    }
}