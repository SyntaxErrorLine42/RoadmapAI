using System.Text;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RoadmapAI.Api.Data;
using RoadmapAI.Api.Models.Configuration;
using RoadmapAI.Api.Models;
using RoadmapAI.Api.Services.AiService;
using RoadmapAI.Api.Services.CourseService;
using RoadmapAI.Api.Services.RoadmapService;

var builder = WebApplication.CreateBuilder(args);

// Allow large file uploads (200MB)
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 200_000_000);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register AutoMapper
builder.Services.AddAutoMapper(config => config.AddMaps(typeof(Program).Assembly));

// Register Services
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IRoadmapService, RoadmapService>();
var blobConnectionString = Environment.GetEnvironmentVariable("BLOB_CONNECTION_STRING")
    ?? builder.Configuration["BlobStorage:ConnectionString"];

var postgresConnectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrWhiteSpace(blobConnectionString))
{
    throw new InvalidOperationException("BLOB_CONNECTION_STRING is not configured.");
}

if (string.IsNullOrWhiteSpace(postgresConnectionString))
{
    throw new InvalidOperationException("POSTGRES_CONNECTION_STRING is not configured.");
}

builder.Services.Configure<BlobStorageOptions>(options =>
{
    options.ConnectionString = blobConnectionString;
    options.ContainerName = "course-materials";
});
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BlobStorageOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.ConnectionString))
    {
        throw new InvalidOperationException("BlobStorage:ConnectionString is not configured.");
    }

    return new BlobServiceClient(options.ConnectionString);
});
builder.Services.AddHttpClient<IAiService, AiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = Environment.GetEnvironmentVariable("AI_SERVICE_BASE_URL")
        ?? configuration["AiService:BaseUrl"]
        ?? "http://localhost:8001";
    client.BaseAddress = new Uri(baseUrl);
});

// Register EF Core with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnectionString)
);

// AddIdentityCore doesn't override auth schemes (AddIdentity does, breaking JWT)
builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>() // Makes sure the identity DI interfaces can access the database without using the database DI directly
.AddDefaultTokenProviders();

// Handles JWT token validation on incoming requests
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "a-very-long-and-secure-default-secret-key-32-chars-long";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // This is literally string "Bearer"
})
.AddJwtBearer(options => // This part defines the "Bearer". “For the scheme named 'Bearer', use this handler and these options to authenticate requests.”
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "RoadmapAI",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "RoadmapAI-Client",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Prefer Authorization header; fallback to HttpOnly auth cookie
            if (string.IsNullOrEmpty(context.Token) &&
                context.Request.Cookies.TryGetValue("roadmapai_auth", out var cookieToken))
            {
                context.Token = cookieToken;
            }

            return Task.CompletedTask;
        }
    };
});

// Allow Angular dev server / production site to call the API
var allowedOriginsString = builder.Configuration["AllowedOrigins"] 
    ?? "http://localhost:4200,http://127.0.0.1:4200,http://localhost:4201,http://127.0.0.1:4201,http://localhost:5104,http://127.0.0.1:5104";

var allowedOrigins = allowedOriginsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(o => o.Trim())
                                         .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // for JWT
    });
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapFallbackToFile("index.html");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// UseAuthentication() → checks the incoming request for a token (header or cookie) and validates it using your TokenValidationParameters
// UseAuthorization() → checks [Authorize] attributes and policies which checks the DefaultAuthenticateScheme by default, which we have configured in the JWT configuration

app.Run();
