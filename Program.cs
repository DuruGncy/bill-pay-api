using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MobileProviderBillPaymentSystem.Context;
using MobileProviderBillPaymentSystem.Gateway;
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
var jwtKey = builder.Configuration["JwtKey"];
var jwtIssuer = builder.Configuration["JwtIssuer"];
var jwtAudience = builder.Configuration["JwtAudience"];
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);



// --- API Versioning ---
builder.Services.AddApiVersioning(options => { 
    options.AssumeDefaultVersionWhenUnspecified = true; 
    options.DefaultApiVersion = new ApiVersion(1, 0); 
    options.ReportApiVersions = true; 
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; // e.g., v1
    options.SubstituteApiVersionInUrl = true;
});

// --- Swagger / OpenAPI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Mobile Provider API Gateway",
        Version = "v1"
    });

    // JWT Bearer support
    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'",
    };
    c.AddSecurityDefinition("Bearer", securityScheme);

    var securityReq = new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { securityScheme, new string[] { } }
    };
    c.AddSecurityRequirement(securityReq);

   

});


// JWT authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // During development you can set this false if you're testing without HTTPS
    options.RequireHttpsMetadata = false;
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

    // Add events to surface why authentication is failing
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            // Useful for debugging: ensure token is being read from Authorization header
            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
            logger.LogDebug("OnMessageReceived. Authorization header present: {hasAuth}",
                ctx.Request.Headers.ContainsKey("Authorization"));
            return Task.CompletedTask;
        },
        OnTokenValidated = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
            logger.LogInformation("Token validated for {name}", ctx.Principal?.Identity?.Name ?? "<no-name>");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
            logger.LogError(ctx.Exception, "Authentication failed");
            return Task.CompletedTask;
        },
        OnChallenge = ctx =>
        {
            // This runs when authentication fails and a 401 is about to be returned
            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
            logger.LogWarning("Authentication challenge. Error: {error}; Description: {desc}", ctx.Error, ctx.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// --- Swagger + API Versioning integration ---
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<ISubscriberService, SubscriberService>();


// Add controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
        options.JsonSerializerOptions.WriteIndented = true;
    });


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
app.UseMiddleware<GatewayMiddleware>();
app.MapControllers();

app.Run();

