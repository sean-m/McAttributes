
using McAttributes;
using Microsoft.AspNetCore.OData;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using McAttributes.Data;
using McAttributes.Models;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NuGet.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureAppConfiguration((hostingContext, config) => {
    config.Sources.Clear();
    var env = hostingContext.HostingEnvironment;

    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
          .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

    config.AddEnvironmentVariables();

    if (args != null) {
        config.AddCommandLine(args);
    }
});



static IEdmModel GetEdmModel() {
    var edmBuilder = new ODataConventionModelBuilder();
    var users = edmBuilder.EntitySet<User>("User");
    users.EntityType.Ignore(u => u.Pronouns);
    edmBuilder.EntitySet<IssueLogEntry>("IssueLogEntry");
    return edmBuilder.GetEdmModel();
}

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson()
    .AddOData(
        options => options.AddRouteComponents("odata", GetEdmModel())
            .Select().Filter().OrderBy().Count().SkipToken().SetMaxTop(500));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    opt => opt.ResolveConflictingActions(a => a.First()));
builder.Logging.AddConsole();


if (builder.Environment.IsProduction()) {
    // User Postgrsql
    throw new NotImplementedException("Haven't done the Production db setup yet.");
} 
else {
    var connString = builder.Configuration.GetConnectionString("Identity");
    var conn = new Npgsql.NpgsqlConnection(connString);
    builder.Services.AddDbContext<IdDbContext>(
        options => { options.UseNpgsql(conn); });

    builder.Services.AddDbContext<IssueLogContext>(options =>
        options.UseNpgsql(conn)
    );
    /*
    var conn = new SqliteConnection("Data Source=:memory:");
    conn.Open();
    DebugInit.DbInit(conn);
    builder.Services.AddDbContext<IdDbContext>(
        options => { options.UseSqlite(conn); });

    builder.Services.AddDbContext<IssueLogContext>(options =>
        options.UseSqlite(conn)
    );
    */
}



var app = builder.Build();

using (IServiceScope serviceScope = app.Services.GetService<IServiceScopeFactory>().CreateScope())
{
    var context = serviceScope.ServiceProvider.GetRequiredService<IssueLogContext>();
    context.Database.EnsureCreated();
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

//app.MapControllers();

app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.UseSwagger();
app.UseSwaggerUI();

app.Run();
