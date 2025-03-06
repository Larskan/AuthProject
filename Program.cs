using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AuthProject;
using AuthProject.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("AppDb");
    options.UseSqlite(connectionString);
});
builder.Services.AddScoped<DbSeeder>();
builder.Services.AddScoped<Token>();

builder.Services.AddControllers();

//JWT Auth Config
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; //true would require HTTPS, false for development
    options.SaveToken = true; //Save token in server
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, //Good practice, that its a trusted source, false is if you only need token signature
        ValidateAudience = true,//Good practice, ensures its intended for the API, false is for multiple clients or public use
        ValidateLifetime = true, //False means token never expires, bad security practice
        ValidateIssuerSigningKey = true, //Good practice, ensures token is signed with a key, false is no security
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
    };
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

//Adds a place to enter tokens in Right corner of UI
builder.Services.AddSwaggerGen(options =>
{
    //Instance of OpenApiSecurityScheme, defines authentication in Swagger
    var securityScheme = new OpenApiSecurityScheme 
    { 
        Name = "Authorization", //Header where token is expected
        Type = SecuritySchemeType.Http,//Specifies we are using HTTP Auth
        Scheme = "Bearer", //Type of Auth is Bearer Token, used in JWT Auth
        BearerFormat = "JWT", //Token is Json Web Token(JWT)
        In = ParameterLocation.Header,//Tells swagger that token must be sent in HTTP request header
        Description = "Enter 'Bearer {token}'" //Instruction to swagger UI on how to provide token
    };
    //Add field for entering token
    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        //Indicates that the Bearer token scheme is applicable to all endpoints
        {securityScheme, new string[] { }}
    });
});

builder.Services.AddAuthorization();

builder.Services.AddIdentityApiEndpoints<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

var app = builder.Build();

// Seed
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync().Wait();
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

//Auth middleware, auth requests using JWT..ALWAYS BEFORE AUTHORIZATION
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapIdentityApi<IdentityUser>();

app.Run();

