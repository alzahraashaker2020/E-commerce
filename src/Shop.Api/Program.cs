// Shop.Api entry point. This is the minimal, compiling host for FEAT-001:
// controllers + Swagger only. DI for repositories, services, AutoMapper, JWT,
// CORS, and the ShopContext is wired up later (FEAT-003); it is intentionally
// absent here so the solution builds and the API boots without a live database.
var builder = WebApplication.CreateBuilder(args);

// Register MVC controllers and the Swagger/OpenAPI explorer.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Expose Swagger UI only in the Development environment.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
