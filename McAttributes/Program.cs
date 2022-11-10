
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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

static IEdmModel GetEdmModel() {
    var edmBuilder = new ODataConventionModelBuilder();
    var users = edmBuilder.EntitySet<User>("User");
    users.EntityType.Ignore(u => u.Pronouns);
    edmBuilder.EntitySet<IssueLogEntry>("IssueLogEntry");
    return edmBuilder.GetEdmModel();
}


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


builder.Services.AddAuthentication(options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddOpenIdConnect(options => {
    builder.Configuration.Bind("AzureAD", options);
})
.AddCookie();


//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddMicrosoftIdentityWebApi(builder.Configuration, "AzureAD");


builder.Services.AddRazorPages();


// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson()
    .AddOData(
        options => options.AddRouteComponents("odata", GetEdmModel())
            .EnableQueryFeatures(maxTopValue: 250));

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

    // Uncomment to load in test values from CSV file
    //conn.Open();
    //DebugInit.DbInit(conn);
}



var app = builder.Build();

// Updating an entity bombs without this. Postgresql requires UTC timestamps and for whatever
// reason, the default DateTime behavior is to just try shoving in a value with out a timezone.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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
