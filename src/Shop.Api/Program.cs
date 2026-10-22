using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shop.Api.Mapping;
using Shop.Api.Middleware;
using Shop.BLL.IRepositories;
using Shop.BLL.Repositories;
using Shop.BLL.Security;
using Shop.DAL.Data;
using Shop.SL.TokenManager;

// Shop.Api composition root. Registers the DbContext, the repository/unit-of-work
// wrapper, the SL token service, the password hasher, AutoMapper, JWT auth, CORS,
// controllers, and Swagger; then orders the pipeline with the global exception
// middleware outermost. AddDbContext only configures a provider (no connection
// opens until a query runs) so the API builds and boots without a live database.
var builder = WebApplication.CreateBuilder(args);

IConfiguration config = builder.Configuration;

// CORS policy name for the Angular dev origin.
const string AngularCorsPolicy = "AngularDevPolicy";

// 1. EF Core DbContext over PostgreSQL (provider only; no connection yet).
builder.Services.AddDbContext<ShopContext>(o =>
    o.UseNpgsql(config.GetConnectionString("DefaultConnection")));

// 2-3a. Repository/unit-of-work + cross-cutting services.
builder.Services.AddScoped<IRepoWrapper, RepoWrapper>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();

// 4. AutoMapper — the Type overload comes from the 12.x DI extension package.
builder.Services.AddAutoMapper(typeof(MappingProfile));

// 5. JWT bearer authentication validated against the Jwt config section.
string secretKey = config["Jwt:SecretKey"]!;
string issuer = config["Jwt:Issuer"]!;
string audience = config["Jwt:Audience"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

// 6. Authorization.
builder.Services.AddAuthorization();

// 7. CORS for the Angular dev server.
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// 8. Controllers + Swagger with a JWT bearer security definition.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT bearer token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Pipeline (outermost first).
// 9. Global exception middleware wraps the entire pipeline so any unhandled
//    exception downstream (including inside auth) becomes the standard
//    ApiResponse 500 shape with no stack trace leaked.
app.UseMiddleware<ApiExceptionMiddleware>();

// 10. Swagger + Swagger UI in Development only.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 11-14. CORS, then authentication, authorization, and controller routing.
app.UseCors(AngularCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
