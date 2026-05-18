using McAttributes;
using McAttributes.Data;
using McAttributes.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OData;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks; // Add this using directive at the top of the file
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using NuGet.Configuration;
using SMM.Helper;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static SMM.ConfigFormatter;

static IEdmModel GetEdmModel() {
    var edmBuilder = new ODataConventionModelBuilder();
    var users = edmBuilder.EntitySet<User>("User");
    users.EntityType.Ignore(u => u.Pronouns);
    users.EntityType.Ignore(u => u.SigninActivityJson);

    edmBuilder.EntitySet<AlertLogEntry>("AlertLogEntry");

    edmBuilder.EntitySet<AlertLogApproval>("AlertApproval");

    edmBuilder.EntitySet<Stargate>("Stargate");

    return edmBuilder.GetEdmModel();
}



var builder = WebApplication.CreateBuilder(args);

// Add and load configuration sources.
#pragma warning disable ASP0013 // Suggest switching from using Configure methods to WebApplicationBuilder.Configuration
bool didAzAppConfig = false;
string configString = String.Empty;
builder.Host.ConfigureAppConfiguration((hostingContext, config) => {
    config.Sources.Clear();

    var env = hostingContext.HostingEnvironment;
    // In production, we'll use environment variables and azure app config for settings
    if (env.IsDevelopment()) {
        config.AddUserSecrets<Program>();
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
    }

    config.AddEnvironmentVariables();

    if (args != null) {
        config.AddCommandLine(args);
    }

    // NOTE: set the connection string value in an environment variable or appsettings json file with key: AppConfigConnectionString
    configString = builder.Configuration.GetValue<string>("AppConfigConnectionString");
    Console.WriteLine($"Config string: {(String.IsNullOrEmpty(configString) ? "Not found" : "Found")}");
    var labelFilter = builder.Configuration.GetValue<string>("AppConfigLabelFilter", "\0");
    if (!String.IsNullOrEmpty(configString)) {
        config.AddAzureAppConfiguration(options => {
            options.Connect(configString)
                .Select("*", labelFilter);
        });
        didAzAppConfig = true;
    }
});
Console.WriteLine($"Did we load Azure App Configuration? {(didAzAppConfig ? "YES" : "NO")}");
{
    var configs = FlattenConfiguration(builder.Configuration);
    var table = SMM.ConsoleTable.FromItems<SMM.ConfigFormatter.Setting>(configs.Select(kv => kv.Value));
    var formatted = table.FormatTable(80, configs.Select(kv => kv.Value));
    Debug.WriteLine(formatted);
    Console.WriteLine(formatted);
}



#pragma warning restore ASP0013 // Suggest switching from using Configure methods to WebApplicationBuilder.Configuration



// Azure AD Auth OIDC
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd");

// Use forwarded headers for hosting behind a proxy
builder.Services.Configure<ForwardedHeadersOptions>(options => {
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });


builder.Services.AddRazorPages(options => {
    options.Conventions.AuthorizeFolder("/EmployeeIdRecords");
    options.Conventions.AuthorizeFolder("/Users");
    options.Conventions.AuthorizeFolder("/UserAlerts");
    })
    .AddMicrosoftIdentityUI(); ;

// Add services to the container.
int maxTop = builder.Configuration.GetValue<int?>("maxTopValue") ?? 1000;
builder.Services.AddControllers()
    .AddNewtonsoftJson()
    .AddOData(options => {
        options.AddRouteComponents("odata", GetEdmModel())
            .EnableQueryFeatures(maxTopValue: maxTop);
        });


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    opt => opt.ResolveConflictingActions(a => a.First()));

// Logging
builder.Logging.AddConsole();


var connString = builder.Configuration.GetConnectionString("Identity") ??
    builder.Configuration.GetValue<string>("ConnectionStrings:Identity") ?? "Data Source=./identity.db"; // For whatever reason the ConnectionStrings section of app config doesn't translate directly to Az App Configuraiton key:value use.
var configuredDbType = builder.Configuration.GetValue<String>("DbType", "sqlite");

if (String.IsNullOrEmpty(connString)) {
    throw new Exception($"You ain't getting there from here fam. No connection string, configuration isn't loaded.\n\t > configString: {configString}");
}

var sanitizedString = String.Join(';', connString.Split(';').Select(x => {
    if (x.TrimStart().StartsWith("Password=", StringComparison.CurrentCultureIgnoreCase)) {
        return "Password=*******";
    }
    return x;
}));

if (configuredDbType.Like("npgsql")) {
    // Fine, we'll just use Postgres, don't like sqlserver much anyhow.
    var conn = new Npgsql.NpgsqlConnection(connString);
    sanitizedString = String.Join(';', conn.ConnectionString.Split(';').Select(x => {
        if (x.TrimStart().StartsWith("Password=", StringComparison.CurrentCultureIgnoreCase)) {
            return "Password=*******";
        }
        return x;
    }));
    builder.Services.AddDbContextFactory<IdDbContext>(
    options => {
        options.UseNpgsql(conn, npgoptions => {
            npgoptions.EnableRetryOnFailure(4);
        });
    });
}
else if (configuredDbType.Like("sqlserver")) {
    builder.Services.AddDbContext<IdDbContext>(
        options => { options.UseSqlServer(connString); });
}
else if (configuredDbType.Like("sqlite")) {
    builder.Services.AddDbContext<IdDbContext>(
        options => { options.UseSqlite(connString); });
}
else {
    throw new Exception("This doesn't work without a database. You should really rethink that whole 'I can program thing'.");
}
builder.Services.AddHttpLogging(options => { });

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add health checks for Azure Container Apps
builder.Services.AddHealthChecks()
    .AddDbContextCheck<IdDbContext>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "ready" })
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

var app = builder.Build();


ILogger logger = app.Logger;
logger.LogInformation($"ConnectionString: {sanitizedString}");

// Updating an entity bombs without this. Postgresql requires UTC timestamps and for whatever
// reason, the default DateTime behavior is to just try shoving in a value with out a timezone.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

using (IServiceScope serviceScope = app.Services.GetService<IServiceScopeFactory>().CreateScope())
{
    var idDbContext = serviceScope.ServiceProvider.GetRequiredService<IdDbContext>();
    var shouldInitialize = builder.Configuration.GetValue<bool?>("InitializeDatabaseWhenMissing") ?? false;
    if (shouldInitialize) {
        if (idDbContext.Database.EnsureCreated()) {
            logger.LogDebug("Initialized database tables.");
            if (app.Environment.IsDevelopment()) {
                // Initialize the database with test data when running in
                // Development mode and having just created tables.
                logger.LogDebug("Loading test data from test_values.csv.");
                DebugInit.DbInit(idDbContext);
            }
        }
    }
    else {
        logger.LogInformation($"Database not automatically initialized. Environment is development: {app.Environment.IsDevelopment()}, InitializeDatabaseWhenMissing config: {shouldInitialize}");
    }
}


if (app.Configuration.GetValue<bool>("ForceHttpsScheme", false)) {
    logger.LogInformation("Enforcing https scheme.");
    app.Use((context, next) => {
        context.Request.Scheme = "https";
        return next(context);
    });
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.UseHttpLogging();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseRouting();

app.UseAuthorization();


app.UseSwagger();
//app.UseSwaggerUI();

// Health check endpoints for Azure Container Apps
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/startup", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapRazorPages();
app.MapControllers();

app.Run();


