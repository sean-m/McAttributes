
using McAttributes;
using System;
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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web.UI;

static IEdmModel GetEdmModel() {
    var edmBuilder = new ODataConventionModelBuilder();
    var users = edmBuilder.EntitySet<User>("User");
    users.EntityType.Ignore(u => u.Pronouns);

    edmBuilder.EntitySet<IssueLogEntry>("IssueLogEntry");
    
    edmBuilder.EntitySet<Stargate>("Stargate");
    
    return edmBuilder.GetEdmModel();
}


var builder = WebApplication.CreateBuilder(args);

// Add and load configuration sources.
builder.Host.ConfigureAppConfiguration((hostingContext, config) => {
    config.Sources.Clear();

    var env = hostingContext.HostingEnvironment;
    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
          .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

    config.AddEnvironmentVariables();

    // NOTE: set the connection string value in an environment variable or appsettings json file with key: AppConfigConnectionString
    var configString = builder.Configuration.GetValue<string>("AppConfigConnectionString");
    if (!String.IsNullOrEmpty(configString)) { config.AddAzureAppConfiguration(configString); }
    else { Console.WriteLine("Azure app config script not provided."); }

    // Add command line args last so they can override anything else.
    if (args != null) {
        config.AddCommandLine(args);
    }
});


builder.Services.AddAuthentication(options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddOpenIdConnect(options => {
    builder.Configuration.Bind("AzureAD", options);
})
.AddJwtBearer(options => {
    builder.Configuration.Bind("AzureAD", options);
})
.AddCookie();



builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI(); ;


// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson()
    .AddOData(
        options => options.AddRouteComponents("odata", GetEdmModel())
            .EnableQueryFeatures(maxTopValue: 500));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    opt => opt.ResolveConflictingActions(a => a.First()));
builder.Logging.AddConsole();



var connString = builder.Configuration.GetConnectionString("Identity");
// var conn = new Npgsql.NpgsqlConnection(connString);
// builder.Services.AddDbContext<IdDbContext>(
//     options => { options.UseNpgsql(conn); });
if (!String.IsNullOrEmpty(connString)) {
builder.Services.AddDbContext<IdDbContext>(
    options => { options.UseSqlServer(connString); });
} else {
    throw new System.Exception("Connection string 'Identity' not defined in application config.");
}

var app = builder.Build();

// Updating an entity bombs without this. Postgresql requires UTC timestamps and for whatever
// reason, the default DateTime behavior is to just try shoving in a value with out a timezone.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

using (IServiceScope serviceScope = app.Services.GetService<IServiceScopeFactory>().CreateScope())
{
    try {
        var idDbContext = serviceScope.ServiceProvider.GetRequiredService<IdDbContext>();
        if (idDbContext.Database.EnsureCreated()) {
            if (!builder.Environment.IsProduction()) {
                // Initialize the database with test data when running in 
                // debug mode and having just created tables.
                System.Diagnostics.Trace.WriteLine("Initialized database tables. Loading table data from test_values.csv.");
                DebugInit.DbInit(idDbContext);
            }
        }
    } catch (Exception e) {
        Console.Error.WriteLine("Failed while trying to ensure database creation!.\n{0}", e.Message);
        var innerException = e.InnerException;
        int count = 0;
        while (innerException != null) {
            count++;
            Console.Error.WriteLine("Inner ex {0}: {1}", count, innerException?.Message);
            Console.Error.WriteLine("Inner ex {0}: {1}", count, innerException?.StackTrace);
            innerException = innerException.InnerException;
        }
        throw new Exception("Database init failed, dying...");
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
}

// Shouldn't crash if Azure App Config connection string isn't configured.
if (!String.IsNullOrEmpty(builder.Configuration.GetValue<string>("AppConfigConnectionString"))) {
    app.UseAzureAppConfiguration();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseRouting();

app.UseAuthorization();


app.MapRazorPages();
app.MapControllers();

app.UseSwagger();
//app.UseSwaggerUI();


app.Run();
