using CommentApi.Data;
using CommentApi.Models;
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
}
