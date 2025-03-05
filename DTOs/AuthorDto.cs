using Microsoft.AspNetCore.Identity;

namespace AuthProject.DTOs;

public class AuthorDto
{
    public string Id { get; set; }
    public string Name { get; set; }

    public static AuthorDto FromEntity(IdentityUser entity)
    {
        return new AuthorDto { Id = entity.Id, Name = entity.UserName };
    }
}