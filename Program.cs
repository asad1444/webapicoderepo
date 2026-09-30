using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Helpers;
using SmartProManWebAPI.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── Controllers ────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ─── Swagger with JWT support ───────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName!.Replace("+", "."));

    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartProMan API", Version = "v1" });

    // Allow JWT Bearer token in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: Bearer {your_token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // Resolve action conflicts (multiple actions with same route)
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    
    // Ignore obsolete errors
    c.IgnoreObsoleteActions();
    c.IgnoreObsoleteProperties();
});

// ─── Database ────────────────────────────────────────────────────────────────
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnection))
{
    defaultConnection = "Data Source=smartproman.db";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(defaultConnection));

// ─── JWT Authentication ──────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero  // No tolerance for expired tokens
        };
    });

builder.Services.AddAuthorization();

// ─── Register JwtHelper ──────────────────────────────────────────────────────
builder.Services.AddSingleton<JwtHelper>();

// ─── Register SMS Service ─────────────────────────────────────────────────────
// Development: logs OTP to console (no real SMS sent)
// Production:  swap ConsoleSmsService → TwilioSmsService
builder.Services.AddScoped<SmartProManWebAPI.Services.ISmsService,
                           SmartProManWebAPI.Services.ConsoleSmsService>();

// ─── Register Email Service ───────────────────────────────────────────────────
// Uses Gmail SMTP — configure Email:FromEmail and Email:Password in appsettings.json
builder.Services.AddScoped<SmartProManWebAPI.Services.IEmailService,
                           SmartProManWebAPI.Services.GmailEmailService>();

// ─── CORS ────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    // Development: allow any origin (React dev server)
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // Production: restrict to known origins
    options.AddPolicy("AllowProductionOrigins", policy =>
    {
        policy.WithOrigins(
                "https://smartproman.com",
                "https://app.smartproman.com"
              )
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ─── Build ───────────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── Auto-migrate database on startup ────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        app.Logger.LogInformation("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "SQL Server is not available at startup. The API will continue without auto-migration. Start SQL Server or update the connection string.");
    }
}

// ─── Middleware pipeline ──────────────────────────────────────────────────────

// Global exception handler (must be first)
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseDeveloperExceptionPage();

    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartProMan API v1");
        c.DisplayRequestDuration();
    });
}
app.UseCors("AllowReactApp");

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
