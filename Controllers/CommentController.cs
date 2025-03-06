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
    #region GET
    [HttpGet]
    public async Task<IEnumerable<CommentDto>> Get([FromQuery] int? articleId)
    {
        var query = db.Comments.Include(x => x.Author).AsQueryable();

        if (articleId.HasValue)
            query = query.Where(c => c.ArticleId == articleId);

        var comments = await query.ToListAsync();
        return comments.Select(CommentDto.FromEntity);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<CommentDto>> GetById(int id)
    {
        var comment = await db.Comments.Include(x => x.Author).SingleOrDefaultAsync(x => x.Id == id);
        if (comment == null) return NotFound();
        return CommentDto.FromEntity(comment);
    }
    #endregion GET

    #region POST
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
    #endregion POST

    #region PUT
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
    #endregion PUT

    #region DELETE
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Editor)] // Only Editors can delete comments
    public async Task<ActionResult> Delete(int id)
    {
        var comment = await db.Comments.FirstOrDefaultAsync(x => x.Id == id);
        if (comment == null) return NotFound();

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
        return NoContent();
    }
    #endregion DELETE

    #region Ramus Code
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

    //[HttpGet(":id")]
    //public CommentDto? GetById(int id)
    //{
    //    return db
    //        .Comments.Include(x => x.Author)
    //        .Select(CommentDto.FromEntity)
    //        .SingleOrDefault(x => x.Id == id);
    //}
    #endregion Ramus Code

}
