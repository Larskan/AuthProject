using AuthProject.DTOs;
using AuthProject.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthProject.Controllers;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public class CommentController : ControllerBase
{
    private readonly AppDbContext db;
    private readonly UserManager<IdentityUser> userManager;

    public CommentController(AppDbContext ctx, UserManager<IdentityUser> userManager)
    {
        this.db = ctx;
        this.userManager = userManager;
    }

    [HttpGet]
    public IEnumerable<CommentDto> Get([FromQuery] int? articleId)
    {
        var query = db.Comments.Include(x => x.Author).AsQueryable();
        if (articleId.HasValue)
            query = query.Where(c => c.ArticleId == articleId);
        return query.Select(CommentDto.FromEntity);
    }

    [HttpGet(":id")]
    public CommentDto? GetById(int id)
    {
        return db
            .Comments.Include(x => x.Author)
            .Select(CommentDto.FromEntity)
            .SingleOrDefault(x => x.Id == id);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Subscriber)]
    public async Task<ActionResult<CommentDto>> Post([FromBody] CommentFormDto dto)
    {
        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Unauthorized();

        var article = db.Articles.FirstOrDefault(x => x.Id == dto.ArticleId);
        if (article == null) return NotFound();

        var entity = new Comment
        {
            Content = dto.Content,
            Author = user,
            Article = article
        };

        db.Comments.Add(entity);
        db.SaveChanges();
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, CommentDto.FromEntity(entity));
    }

    //[HttpPost]
    //public CommentDto Post([FromBody] CommentFormDto dto)
    //{
    //    var userName = HttpContext.User.Identity?.Name;
    //    var author = db.Users.Single(x => x.UserName == userName);
    //    var article = db.Articles.Single(x => x.Id == dto.ArticleId);
    //    var entity = new Comment
    //    {
    //        Content = dto.Content,
    //        Article = article,
    //        Author = author,
    //    };
    //    var created = db.Comments.Add(entity).Entity;
    //    db.SaveChanges();
    //    return CommentDto.FromEntity(created);
    //}

    //[HttpPut(":id")]
    //public CommentDto Put(int id, [FromBody] CommentFormDto dto)
    //{
    //    var userName = HttpContext.User.Identity?.Name;
    //    var entity = db
    //        .Comments.Include(x => x.Author)
    //        .Where(x => x.Author.UserName == userName)
    //        .Single(x => x.Id == id);
    //    entity.Content = dto.Content;
    //    var updated = db.Comments.Update(entity).Entity;
    //    db.SaveChanges();
    //    return CommentDto.FromEntity(updated);
    //}
    [HttpPut(":id")]
    [Authorize]
    public async Task<ActionResult<CommentDto>> Put(int id, [FromBody] CommentFormDto dto)
    {
        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Unauthorized();

        var comment = db.Comments.Include(x => x.Author).FirstOrDefault(x => x.Id == id);
        if (comment == null) return NotFound();

        if (User.IsInRole(Roles.Editor) || comment.Author.Id == user.Id) // Editors OR Comment Owners can edit
        {
            comment.Content = dto.Content;
            db.SaveChanges();
            return CommentDto.FromEntity(comment);
        }

        return Forbid();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Editor)] // Only Editors can delete comments
    public ActionResult Delete(int id)
    {
        var comment = db.Comments.FirstOrDefault(x => x.Id == id);
        if (comment == null) return NotFound();

        db.Comments.Remove(comment);
        db.SaveChanges();
        return NoContent();
    }

}
