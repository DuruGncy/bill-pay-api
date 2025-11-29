using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MobileProviderBillPaymentSystem.Context;
using MobileProviderBillPaymentSystem.Services;
using MobileProviderBillPaymentSystem.Services.Interfaces;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
// Determine environment
var isRender = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER_INTERNAL_HOSTNAME"));

// Pick correct connection string
var connectionString = isRender
    ? builder.Configuration["ConnectionStringsBillingDbInternal"]
    : builder.Configuration["ConnectionStringsBillingDb"];

builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null)));




// JWT settings from configuration
var jwtKey = builder.Configuration["Jwt_Key"] ?? "!";
var jwtIssuer = builder.Configuration["Jwt_Issuer"] ?? "";
var jwtAudience = builder.Configuration["Jwt_Audience"] ?? "";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);



// --- API Versioning ---
builder.Services.AddApiVersioning(options => { 
    options.AssumeDefaultVersionWhenUnspecified = true; 
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0); 
    options.ReportApiVersions = true; 
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; // e.g., v1
    options.SubstituteApiVersionInUrl = true;
});

// --- Swagger / OpenAPI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
    };
});

builder.Services.AddAuthorization();

// --- Swagger + API Versioning integration ---
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<ISubscriberService, SubscriberService>();


// Add services
builder.Services.AddControllers();

var app = builder.Build();

// Enable Swagger middleware
var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    foreach (var description in provider.ApiVersionDescriptions)
    {
        c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                          $"Mobile Provider API {description.GroupName}");
    }
    c.RoutePrefix = "swagger"; // swagger UI at /swagger
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// --- Helper class to generate Swagger docs per API version ---
public class ConfigureSwaggerOptions : Microsoft.Extensions.Options.IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;
    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = $"Mobile Provider API {description.ApiVersion}",
                Version = description.GroupName
            });

            // Optional: JWT Bearer auth
            var bearerScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT",
                Description = "Enter 'Bearer {token}'",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            };

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, bearerScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { bearerScheme, Array.Empty<string>() }
            });
        }
    }
}
