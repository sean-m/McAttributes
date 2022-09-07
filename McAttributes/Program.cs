
using McAttributes;
using Microsoft.AspNetCore.OData;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddNewtonsoftJson()
    .AddOData(
        options => options.Select().Filter().OrderBy().Count().SkipToken().SetMaxTop(500));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Logging.AddConsole();


if (builder.Environment.IsProduction()) {
    // User Postgrsql
    throw new NotImplementedException("Haven't done the Production db setup yet.");
} 
else {
    var conn = new SqliteConnection("Filename=mcattributes.db");
    conn.Open();
    DebugInit.DbInit(conn);
    builder.Services.AddDbContext<IdDbContext>(
        options => { options.UseSqlite(conn); });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
