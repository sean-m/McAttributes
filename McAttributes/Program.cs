
using McAttributes;
using Microsoft.AspNetCore.OData;
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
using SMM.Helper;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.AspNetCore.HttpOverrides;

static IEdmModel GetEdmModel() {
    var edmBuilder = new ODataConventionModelBuilder();
    var users = edmBuilder.EntitySet<User>("User");
    users.EntityType.Ignore(u => u.Pronouns);

    edmBuilder.EntitySet<AlertLogEntry>("IssueLogEntry");

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
    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
          .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

    config.AddEnvironmentVariables();

    if (args != null) {
        config.AddCommandLine(args);
    }

    // NOTE: set the connection string value in an environment variable or appsettings json file with key: AppConfigConnectionString
    configString = builder.Configuration.GetValue<string>("AppConfigConnectionString");
    if (!String.IsNullOrEmpty(configString)) {
        config.AddAzureAppConfiguration(options => {
            options.Connect(configString)
                .Select("*","McAttributes");
        });
        didAzAppConfig = true;
    }
});
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
builder.Services.AddControllers()
    .AddNewtonsoftJson()
    .AddOData(options => {
        options.AddRouteComponents("odata", GetEdmModel())
            .EnableQueryFeatures(maxTopValue: 500)
            .Count();
        });


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    opt => opt.ResolveConflictingActions(a => a.First()));

// Logging
builder.Logging.AddConsole();


var connString = builder.Configuration.GetConnectionString("Identity") ??
    builder.Configuration.GetValue<string>("ConnectionStrings:Identity"); // For whatever reason the ConnectionStrings section of app config doesn't translate directly to Az App Configuraiton key:value use.
var configuredDbType = builder.Configuration.GetValue<String>("DbType", "sqlite");

if (String.IsNullOrEmpty(connString)) {
    throw new Exception($"You ain't getting there from here fam. No connection string, configuration isn't loaded.\n\t > configString: {configString}");
}



//if (configuredDbType.Like("npgsql")) {
// Fine, we'll just use Postgres, don't like sqlserver much anyhow.
var conn = new Npgsql.NpgsqlConnection(connString);
var sanitizedString = String.Join(';', conn.ConnectionString.Split(';').Select(x => {
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
//}
//else if (configuredDbType.Like("sqlserver")) {
//builder.Services.AddDbContext<IdDbContext>(
//    options => { options.UseSqlServer(connString); });
//}

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
        logger.LogInformation($"Database not automatically initialized. Environment: {app.Environment.IsDevelopment()}, InitializeDatabaseWhenMissing config: {shouldInitialize}");
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
app.UseHttpLogging();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseRouting();

app.UseAuthorization();


app.UseSwagger();
//app.UseSwaggerUI();

app.MapRazorPages();
app.MapControllers();

app.Run();
