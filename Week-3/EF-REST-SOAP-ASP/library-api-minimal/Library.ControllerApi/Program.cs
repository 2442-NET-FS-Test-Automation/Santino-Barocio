using Library.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var conn_string = "Server=localhost,1433;Database=LibraryMinimalDb;User Id=sa;Password=S4nt1n0L!;TrustServerCertificate=true";

builder.Services.AddDbContext<LibraryDbContext>(o => o.UseSqlServer(conn_string));

builder.Services.AddScoped<IInventoryRepository, IInventoryRepository>(); // Could Later swap for InventoryMongoRepo

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen();

// App area
var app = builder.Build();

//Swagger stuff added to app
app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
