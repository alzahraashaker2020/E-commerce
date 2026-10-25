using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shop.Api.Mapping;
using Shop.Api.Middleware;
using Shop.BLL.Email;
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

// 1. EF Core DbContext. The active provider is chosen by the "Database:Provider"
//    config setting: "Postgres" (production target) or "Sqlite" (zero-install
//    local/demo run against a local file). Defaults to Sqlite so the demo runs
//    without a database server; set it to "Postgres" to use PostgreSQL.
string dbProvider = config["Database:Provider"] ?? "Sqlite";
builder.Services.AddDbContext<ShopContext>(o =>
{
    if (string.Equals(dbProvider, "Postgres", StringComparison.OrdinalIgnoreCase))
    {
        o.UseNpgsql(config.GetConnectionString("DefaultConnection"));
    }
    else
    {
        // SQLite: a local file alongside the API; no server to install or start.
        o.UseSqlite(config.GetConnectionString("SqliteConnection")
            ?? "Data Source=shop_demo.db");
    }
});

// 2-3a. Repository/unit-of-work + cross-cutting services.
builder.Services.AddScoped<IRepoWrapper, RepoWrapper>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();

// Email service. Options are bound from the "Email" config section and the
// host content-root path (used by the demo-mode disk sink) is supplied here.
builder.Services.AddScoped<IEmailService>(sp =>
{
    EmailOptions options = config.GetSection("Email").Get<EmailOptions>() ?? new EmailOptions();
    options.ContentRootPath = builder.Environment.ContentRootPath;
    return new EmailService(options, sp.GetRequiredService<ILogger<EmailService>>());
});

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
// AddJsonOptions registers a JsonStringEnumConverter so System.Text.Json binds
// enum request fields (e.g. PaymentMethod) from their string names ("Cash"/"Card")
// — matching what the Angular client sends — and serializes enums back as strings.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// [ApiController] model-state validation failures default to a ProblemDetails body
// (no "message" field), which leaves the Angular error path (err.error.message)
// empty. Override the factory so validation 400s return the standard ApiResponse
// envelope with a readable, aggregated message instead.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(opt =>
    opt.InvalidModelStateResponseFactory = ctx =>
    {
        var msg = string.Join("; ", ctx.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid request" : e.ErrorMessage));
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
            Shop.BLL.Response_VM.ApiResponse<object>.Fail(string.IsNullOrWhiteSpace(msg) ? "Invalid request" : msg, 400));
    });

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

// Initialize the static-file provider even on a fresh install with no uploads yet.
// Otherwise a missing wwwroot at startup leaves a null provider after the first upload.
string webRoot = builder.Environment.WebRootPath
    ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRoot);
builder.Environment.WebRootPath = webRoot;
builder.Environment.WebRootFileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot);

var app = builder.Build();

// On the SQLite demo path, create and seed the local database on startup if it
// doesn't exist yet. EnsureCreated() builds the full schema (including HasData
// seed rows) directly from the model, so no migrations/DB server are needed for
// the demo. The Postgres path uses real migrations (dotnet ef database update)
// and is left untouched here.
if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    using IServiceScope scope = app.Services.CreateScope();
    ShopContext db = scope.ServiceProvider.GetRequiredService<ShopContext>();
    db.Database.EnsureCreated();
    SqliteSchemaUpgrade.Apply(db);
}

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

// Serve static files from wwwroot (including runtime uploads under
// wwwroot/uploads) so admin-uploaded images resolve by their returned path.
app.UseStaticFiles();

// 11-14. CORS, then authentication, authorization, and controller routing.
app.UseCors(AngularCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
