
using McAttributes;
using Microsoft.AspNetCore.OData;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using McAttributes.Data;

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

    builder.Services.AddDbContext<IssueLogContext>(options =>
        options.UseSqlite(conn)
    );
}



var app = builder.Build();

using (IServiceScope serviceScope = app.Services.GetService<IServiceScopeFactory>().CreateScope())
{
    var context = serviceScope.ServiceProvider.GetRequiredService<IssueLogContext>();
    context.Database.EnsureCreated();
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
