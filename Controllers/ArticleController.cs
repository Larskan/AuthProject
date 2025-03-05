using AuthProject.DTOs;
using AuthProject.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace AuthProject.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ArticleController : ControllerBase
{
    private readonly AppDbContext db;
    private readonly UserManager<IdentityUser> userManager;

    public ArticleController(AppDbContext ctx, UserManager<IdentityUser> userManager)
    {
        db = ctx;
        this.userManager = userManager;
    }

    [HttpGet]
    public IEnumerable<ArticleDto> Get()
    {
        return db.Articles.Include(x => x.Author).Select(ArticleDto.FromEntity);
    }

    [HttpGet("{id}")]
    public ActionResult<ArticleDto> GetById(int id)
    {
        var article = db.Articles.Include(x => x.Author).FirstOrDefault(x => x.Id == id);
        if (article == null) return NotFound();
        return ArticleDto.FromEntity(article);
    }

    //[HttpGet(":id")]
    //public ArticleDto? GetById(int id)
    //{
    //    return db
    //        .Articles.Include(x => x.Author)
    //        .Where(x => x.Id == id)
    //        .Select(ArticleDto.FromEntity)
    //        .SingleOrDefault();
    //}

    [HttpPost]
    [Authorize(Roles = Roles.Writer)]
    public async Task<ActionResult<ArticleDto>> Post([FromBody] ArticleFormDto dto)
    {
        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Unauthorized();

        var entity = new Article
        {
            Title = dto.Title,
            Content = dto.Content,
            Author = user,
            CreatedAt = DateTime.Now
        };
        db.Articles.Add(entity);
        db.SaveChanges();
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ArticleDto.FromEntity(entity));
    }

    //[HttpPost]
    //public ArticleDto Post([FromBody] ArticleFormDto dto)
    //{
    //    var userName = HttpContext.User.Identity?.Name;
    //    var author = db.Users.Single(x => x.UserName == userName);
    //    var entity = new Article
    //    {
    //        Title = dto.Title,
    //        Content = dto.Content,
    //        Author = author,
    //        CreatedAt = DateTime.Now
    //    };
    //    var created = db.Articles.Add(entity).Entity;
    //    db.SaveChanges();
    //    return ArticleDto.FromEntity(created);
    //}

    [HttpPut(":id")]
    [Authorize]
    public async Task<ActionResult<ArticleDto>> Put(int id, [FromBody] ArticleFormDto dto)
    {

        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Unauthorized();

        var article = db.Articles.Include(x => x.Author).FirstOrDefault(x => x.Id == id);
        if (article == null) return NotFound();

        if(User.IsInRole(Roles.Editor) || article.Author.Id == user.Id)
        {
            article.Title = dto.Title;
            article.Content = dto.Content;
            db.SaveChanges();
            return ArticleDto.FromEntity(article);
        }
        return Forbid();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Editor)] // Only Editors can delete articles
    public ActionResult Delete(int id)
    {
        var article = db.Articles.FirstOrDefault(x => x.Id == id);
        if (article == null) return NotFound();

        db.Articles.Remove(article);
        db.SaveChanges();
        return NoContent();
    }
}
