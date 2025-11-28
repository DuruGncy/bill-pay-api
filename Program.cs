using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MobileProviderBillPaymentSystem.Context;
using System.Text;
using System.Threading.RateLimiting;



var builder = WebApplication.CreateBuilder(args);

// JWT settings from configuration
var jwtKey = builder.Configuration["Jwt_Key"] ?? "!";
var jwtIssuer = builder.Configuration["Jwt_Issuer"] ?? "";
var jwtAudience = builder.Configuration["Jwt_Audience"] ?? "";

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);


// Add services to the container
builder.Services.AddControllers();

// Add Swagger / OpenAPI with JWT bearer support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Mobile Provider API",
        Version = "v1"
    });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter 'Bearer {token}'",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, bearerScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { bearerScheme, Array.Empty<string>() }
    });
});

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

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.ReportApiVersions = true;

}).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV"; // e.g., v1
        options.SubstituteApiVersionInUrl = true;
    });



// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("QueryBillLimiter", httpContext =>
    {
        // Identify subscriber
        var subscriberNo = httpContext.Request.Query["subscriberNo"].ToString();
        if (string.IsNullOrEmpty(subscriberNo))
            subscriberNo = "anonymous";

        // Token Bucket: 3 requests per day
        return RateLimitPartition.GetTokenBucketLimiter(subscriberNo, key => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 3,
            TokensPerPeriod = 3,
            ReplenishmentPeriod = TimeSpan.FromDays(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded", cancellationToken: token);
    };
});




var app = builder.Build();

// Swagger middleware (enabled in Development)

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mobile Provider API V1");
    c.RoutePrefix = "swagger";
});


app.UseRateLimiter();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
