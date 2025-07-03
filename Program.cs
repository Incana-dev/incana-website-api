using IncanaPortfolio.Api.Services;
using IncanaPortfolio.Data;
using IncanaPortfolio.Data.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


var secretManager = new GoogleSecretManagerService();
string dbConnectionString;

if (builder.Environment.IsDevelopment())
{
    // This connection string points to the 'proxy-db' container.
    // You must get the user/password from your secret manager.
    var dbUser = secretManager.GetSecret("IncanaDbAdminName");
    var dbPass = secretManager.GetSecret("IncanaDbAdminPw");
    dbConnectionString = $"Host=proxy-db;Port=5432;Database=incana_portfolio_db;Username={dbUser};Password={dbPass}";
}
else
{
    // This is your existing production connection string
    dbConnectionString = secretManager.GetSecret("PostgreSqlConnection");
}
var jwtSecret = secretManager.GetSecret("JWTSecret");
var jwtIssuer = secretManager.GetSecret("ValidIssuer");
var jwtAudience = secretManager.GetSecret("ValidAudience");



var CorsPolicy = "_AllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: CorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:8081", "https://www.incana.studio", "https://incana.studio")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<IStorageService, GoogleCloudStorageService>();
builder.Services.AddSingleton<ISecretManagerService>(secretManager); 

builder.Services.AddDbContext<IncanaPortfolioDbContext>(options =>
{
    options.UseNpgsql(dbConnectionString);
    if (builder.Environment.IsDevelopment())
    {
        options.LogTo(Console.WriteLine, LogLevel.Information);
        options.EnableSensitiveDataLogging();
    }
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<IncanaPortfolioDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidIssuer = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

app.Run($"http://*:{port}");
