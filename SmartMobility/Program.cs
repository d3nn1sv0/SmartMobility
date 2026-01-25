using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartMobility.Configuration;
using SmartMobility.Data;
using SmartMobility.Hubs;
using SmartMobility.Repositories;
using SmartMobility.Repositories.Interfaces;
using SmartMobility.Services;
using SmartMobility.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings not configured");

var envSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
if (!string.IsNullOrEmpty(envSecret))
{
    jwtSettings.Secret = envSecret;
}

if (string.IsNullOrEmpty(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
{
    throw new InvalidOperationException(
        "JWT Secret must be at least 32 characters. " +
        "Set it with 'dotnet user-secrets set \"JwtSettings:Secret\" \"your-secret-key\"' (development) " +
        "or as environment variable JWT_SECRET (production).");
}

builder.Services.AddSingleton(jwtSettings);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Smart Mobility API",
        Version = "v1",
        Description = "Realtids informationssystem til offentlig transport"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Indtast JWT token fra login"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<SmartMobilityDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

builder.Services.AddScoped<IEtaService, EtaService>();
builder.Services.AddScoped<IGpsTrackingService, GpsTrackingService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SmartMobilityDbContext>();
    context.Database.EnsureCreated();

    if (!context.Users.Any(u => u.Role == SmartMobility.Models.Enums.UserRole.Admin))
    {
        var admin = new SmartMobility.Models.Entities.User
        {
            Email = "admin@smartmobility.dk",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Name = "Administrator",
            Role = SmartMobility.Models.Enums.UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(admin);
        context.SaveChanges();

        Console.WriteLine("===========================================");
        Console.WriteLine("Admin account created:");
        Console.WriteLine("Email: admin@smartmobility.dk");
        Console.WriteLine("Password: Admin123!");
        Console.WriteLine("===========================================");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Mobility API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("SignalRPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<GpsTrackingHub>("/hubs/gpstracking");

app.Run();