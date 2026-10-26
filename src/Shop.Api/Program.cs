using System.Text;
using Microsoft.AspNetCore.DataProtection;
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
using Shop.Api.Tenancy;
using System.Threading.RateLimiting;

// Shop.Api composition root. Registers the DbContext, the repository/unit-of-work
// wrapper, the SL token service, the password hasher, AutoMapper, JWT auth, CORS,
// controllers, and Swagger; then orders the pipeline with the global exception
// middleware outermost. AddDbContext only configures a provider (no connection
// opens until a query runs) so the API builds and boots without a live database.
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("email-secrets.json",optional:true,reloadOnChange:true).AddEnvironmentVariables();

IConfiguration config = builder.Configuration;
var deployment = new DeploymentProfile(config);
builder.Services.AddSingleton(deployment);
builder.Services.AddScoped<StoreCapabilities>();
builder.Services.AddHttpContextAccessor();

// CORS policy name for the Angular dev origin.
const string AngularCorsPolicy = "AngularDevPolicy";

// 1. EF Core DbContext. The active provider is chosen by the "Database:Provider"
//    config setting: "Postgres" (production target) or "Sqlite" (zero-install
//    local/demo run against a local file). Defaults to Sqlite so the demo runs
//    without a database server; set it to "Postgres" to use PostgreSQL.
builder.Services.AddDbContext<PlatformContext>(o => o.UseSqlite(config.GetConnectionString("PlatformConnection")
    ?? new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = Path.Combine(builder.Environment.ContentRootPath, "shop_platform.db") }.ToString()));
builder.Services.AddScoped<StoreScope>();
builder.Services.AddScoped<StorefrontContent>();
builder.Services.AddSingleton<Shop.Api.Operations.OperationJournal>();
builder.Services.AddSingleton<Shop.Api.Operations.StoreBackups>();
builder.Services.AddHostedService<Shop.Api.Operations.BackupScheduler>();
builder.Services.AddScoped<OrderTrackingAccess>();
builder.Services.AddScoped<Shop.Api.Security.CustomerEmailSecurity>();
builder.Services.AddScoped<Shop.Api.Security.OrderReturnsService>();
builder.Services.AddScoped<Shop.Api.Security.EmailOutbox>();
builder.Services.AddScoped<IEmailOutbox>(sp=>sp.GetRequiredService<Shop.Api.Security.EmailOutbox>());
builder.Services.AddHostedService<Shop.Api.Security.EmailOutboxWorker>();
builder.Services.AddDataProtection().SetApplicationName("ShopEmailOutbox").PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath,"store-data","email-keys")));
builder.Services.AddScoped<IStoreIdentity>(sp => sp.GetRequiredService<StoreScope>());
builder.Services.AddDbContext<ShopContext>((sp, o) => StoreConnections.Configure(o, config,
    sp.GetRequiredService<StoreScope>().Store?.ConnectionKey ?? (EF.IsDesignTime ? "DefaultStore" : throw new InvalidOperationException("Store resolution is required."))));

// 2-3a. Repository/unit-of-work + cross-cutting services.
builder.Services.AddScoped<IRepoWrapper, RepoWrapper>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();

// Email service. Options are bound from the "Email" config section and the
// host content-root path (used by the demo-mode disk sink) is supplied here.
builder.Services.AddScoped<EmailService>(sp =>
{
    var store = sp.GetRequiredService<StoreScope>();
    EmailOptions options = config.GetSection(store.Store?.ConnectionKey == "DefaultStore" ? "Email" : "StoreEmail:" + store.StoreId).Get<EmailOptions>() ?? new EmailOptions();
    options.AllowDevelopmentSink = builder.Environment.IsDevelopment();
    options.Language=sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request.Headers.AcceptLanguage.ToString().Split(',')[0].Split(';')[0].Split('-')[0].Trim().ToLowerInvariant()=="ar"?"ar":"en";
    options.Translations=sp.GetRequiredService<StorefrontContent>().ReadAsync().GetAwaiter().GetResult().Translations;
    StoreEmailPreferences.Read(sp.GetRequiredService<ShopContext>(),options);
    options.ContentRootPath = Path.Combine(builder.Environment.ContentRootPath, "store-data", store.StoreId);
    return new EmailService(options, sp.GetRequiredService<ILogger<EmailService>>(), sp.GetRequiredService<IEmailOutbox>());
});
builder.Services.AddScoped<IEmailService>(sp=>sp.GetRequiredService<EmailService>());

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
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var user = context.Principal!;
                var platform = context.HttpContext.RequestServices.GetRequiredService<PlatformContext>();
                if (context.Request.Path.StartsWithSegments("/api/platform"))
                {
                    if (!user.IsInRole("PlatformAdmin") || user.HasClaim(c => c.Type == "store_id") ||
                        !int.TryParse(user.FindFirst("actor_id")?.Value, out var id) ||
                        !await platform.Operators.AnyAsync(o => o.Id == id)) context.Fail("Invalid platform session");
                    return;
                }
                var scope = context.HttpContext.RequestServices.GetRequiredService<StoreScope>();
                if (scope.Store is null || user.FindFirst("store_id")?.Value != scope.StoreId || user.IsInRole("PlatformAdmin"))
                { context.Fail("Session belongs to another store"); return; }
                if((user.FindFirst("store_security_version")?.Value??"")!=scope.SecurityVersion)
                {context.Fail("Store session revoked after recovery");return;}
                if (user.IsInRole("Merchant"))
                {
                    var db = context.HttpContext.RequestServices.GetRequiredService<ShopContext>();
                    if (!int.TryParse(user.FindFirst("actor_id")?.Value, out var id) ||
                        !await db.AdminUsers.AnyAsync(a => a.Id == id && a.IsActive && (a.Role!.Name == "Merchant" || a.Role.Name == "Admin")))
                        context.Fail("Merchant access revoked");
                }
                else if(user.IsInRole("Customer"))
                {
                    var db=context.HttpContext.RequestServices.GetRequiredService<ShopContext>();
                    if(!int.TryParse(user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,out int customerId)) {context.Fail("Invalid customer");return;}
                    var customer=await db.Customers.AsNoTracking().FirstOrDefaultAsync(c=>c.Id==customerId);
                    string stamp=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(customer?.PasswordHash??"")));
                    if(customer is null || user.FindFirst("session_stamp")?.Value != stamp) context.Fail("Customer session revoked");
                }
            }
        };
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
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("Marketplace",context=>RateLimitPartition.GetFixedWindowLimiter("marketplace:"+context.Connection.RemoteIpAddress,
        _=>new FixedWindowRateLimiterOptions{PermitLimit=60,Window=TimeSpan.FromMinutes(1),QueueLimit=0,AutoReplenishment=true}));
    options.AddPolicy("MerchantRegistration",context=>RateLimitPartition.GetFixedWindowLimiter(
        "merchant-apply:"+context.Connection.RemoteIpAddress,
        _=>new FixedWindowRateLimiterOptions{PermitLimit=20,Window=TimeSpan.FromMinutes(10),QueueLimit=0,AutoReplenishment=true}));
    options.AddPolicy("MerchantApplicationStatus",context=>RateLimitPartition.GetFixedWindowLimiter(
        "merchant-status:"+context.Connection.RemoteIpAddress,
        _=>new FixedWindowRateLimiterOptions{PermitLimit=10,Window=TimeSpan.FromMinutes(10),QueueLimit=0,AutoReplenishment=true}));
    options.AddPolicy("Tracking", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Request.Host.Host + ":" + context.Connection.RemoteIpAddress,
        _ => new FixedWindowRateLimiterOptions { PermitLimit=30, Window=TimeSpan.FromMinutes(1), QueueLimit=0, AutoReplenishment=true }));
    options.AddPolicy("AccountRecovery", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Request.Host.Host+":"+context.Connection.RemoteIpAddress+":"+context.Request.Path,
        _=>new FixedWindowRateLimiterOptions {PermitLimit=10,Window=TimeSpan.FromMinutes(10),QueueLimit=0,AutoReplenishment=true}));
    options.AddPolicy("AccountRegistration",context=>RateLimitPartition.GetFixedWindowLimiter(context.Request.Host.Host+":"+context.Connection.RemoteIpAddress,
        _=>new FixedWindowRateLimiterOptions{PermitLimit=20,Window=TimeSpan.FromMinutes(10),QueueLimit=0,AutoReplenishment=true}));
    options.AddPolicy("ImageProcessing", context => RateLimitPartition.GetConcurrencyLimiter(
        context.Request.Host.Host, _ => new ConcurrencyLimiterOptions { PermitLimit=2, QueueLimit=20, QueueProcessingOrder=QueueProcessingOrder.OldestFirst }));
});

// 7. CORS for the Angular dev server.
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCorsPolicy, policy =>
        policy.SetIsOriginAllowed(origin =>
            (config.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>()).Contains(origin, StringComparer.OrdinalIgnoreCase) ||
            (builder.Environment.IsDevelopment() && Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Scheme == "http" && uri.Port == 4200 &&
                (uri.Host == "localhost" || uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))))
            .AllowAnyHeader()
            .WithExposedHeaders("X-Checkout-Outcome")
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
using var instanceLock=Shop.Api.Operations.OfflineRecovery.AcquireInstanceLock(builder.Environment.ContentRootPath);
if(args.Contains("--restore-backup"))
{
    try{await Shop.Api.Operations.OfflineRecovery.RestoreAsync(app.Services,config,builder.Environment,config["restore-backup"]??"",config["confirm-store"]??"");}
    catch(Exception ex){Console.Error.WriteLine(ex is InvalidOperationException?ex.Message:"Recovery stopped ("+ex.GetType().Name+"). Verify the backup, confirmed store ID, filesystem access and recovery procedure. No API was started.");Environment.ExitCode=1;}
    return;
}

using (var scope = app.Services.CreateScope())
{
    var platform = scope.ServiceProvider.GetRequiredService<PlatformContext>();
    platform.Database.EnsureCreated();
    SubscriptionSchema.Apply(platform, deployment.SaasEnabled);
    scope.ServiceProvider.GetRequiredService<Shop.Api.Operations.OperationJournal>().InitializeAsync().GetAwaiter().GetResult();
    // Additive registry upgrade for the first local registry version; never recreate it.
    platform.Database.OpenConnection();
    using (var column = platform.Database.GetDbConnection().CreateCommand())
    {
        column.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Stores') WHERE name='DatabaseIdentity'";
        if (Convert.ToInt64(column.ExecuteScalar()) == 0)
            platform.Database.ExecuteSqlRaw("ALTER TABLE Stores ADD COLUMN DatabaseIdentity TEXT NULL");
    }
    foreach (var registered in deployment.Stores(platform))
    {
        string identity = StoreConnections.DatabaseIdentity(config, registered.ConnectionKey);
        if (registered.DatabaseIdentity is not null && registered.DatabaseIdentity != identity)
            throw new InvalidOperationException("A registered store database reference was changed. Use an explicit migration procedure.");
        registered.DatabaseIdentity = identity;
    }
    platform.SaveChanges();
    platform.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Stores_DatabaseIdentity ON Stores(DatabaseIdentity)");
    platform.Database.CloseConnection();
    if ((deployment.SaasEnabled || deployment.StandaloneStoreId=="default") && !platform.Stores.Any(s => s.ConnectionKey == "DefaultStore"))
        platform.Stores.Add(new StoreRegistration { Id = "default", Name = "MakaniShop", Host = (config["Platform:DefaultStoreHost"] ?? "localhost").ToLowerInvariant(), ConnectionKey = "DefaultStore", DatabaseIdentity = StoreConnections.DatabaseIdentity(config, "DefaultStore"), State = "Active" });
    string? operatorName = config["Platform:BootstrapUsername"], operatorPassword = config["Platform:BootstrapPassword"];
    if (deployment.SaasEnabled && !platform.Operators.Any() && !string.IsNullOrWhiteSpace(operatorName) && operatorPassword?.Length >= 12)
        platform.Operators.Add(new PlatformOperator { Username = operatorName.Trim(), PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>().Hash(operatorPassword) });
    platform.SaveChanges();
    if(deployment.SaasEnabled && platform.Stores.Any(s=>s.Host==(config["Platform:PublicHost"]??"platform.localhost").ToLowerInvariant()))
        throw new InvalidOperationException("The platform hostname must be separate from every merchant hostname.");
    if (!deployment.SaasEnabled && !deployment.Stores(platform).Any())
        throw new InvalidOperationException("The configured standalone store does not exist. Provision it first and preserve its store ID, database and uploads.");
}

// On the SQLite demo path, create and seed the local database on startup if it
// doesn't exist yet. EnsureCreated() builds the full schema (including HasData
// seed rows) directly from the model, so no migrations/DB server are needed for
// the demo. The Postgres path uses real migrations (dotnet ef database update)
// and is left untouched here.
using (IServiceScope scope = app.Services.CreateScope())
{
    var platform = scope.ServiceProvider.GetRequiredService<PlatformContext>();
    var keys = deployment.Stores(platform).AsNoTracking().Where(s => s.State == "Active" || s.State == "Suspended")
        .Select(s => s.ConnectionKey).ToList();
    if(deployment.SaasEnabled)keys.Add("DefaultStore");
    foreach (string key in keys.Distinct())
    {
        var (provider, connection) = StoreConnections.Read(config, key);
        if (!provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase)) continue;
        if (key != "DefaultStore" && !File.Exists(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connection).DataSource))
            throw new InvalidOperationException("A registered store database is missing; restore it instead of recreating demo data.");
        var options = new DbContextOptionsBuilder<ShopContext>();
        StoreConnections.Configure(options, config, key);
        using var db = new ShopContext(options.Options);
        bool newlyCreated = db.Database.EnsureCreated();
        SqliteSchemaUpgrade.Apply(db, newlyCreated);
    }
}

// Pipeline (outermost first).
// 9. Global exception middleware wraps the entire pipeline so any unhandled
//    exception downstream (including inside auth) becomes the standard
//    ApiResponse 500 shape with no stack trace leaked.
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseRouting();
app.UseCors(AngularCorsPolicy);
app.UseMiddleware<StoreResolutionMiddleware>();

// 10. Swagger + Swagger UI in Development only.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serve static files from wwwroot (including runtime uploads under
// wwwroot/uploads) so admin-uploaded images resolve by their returned path.
app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = context => context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff" });

// 11-14. CORS, then authentication, authorization, and controller routing.
app.UseAuthentication();
app.UseMiddleware<Shop.Api.Operations.OperationJournalMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();
