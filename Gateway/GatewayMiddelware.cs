namespace MobileProviderBillPaymentSystem.Gateway;

using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class GatewayMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _gatewaySecret;

    public GatewayMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        // Load the secret from configuration
        _gatewaySecret = config["Gateway_Secret"];

        if (string.IsNullOrWhiteSpace(_gatewaySecret))
        {
            throw new InvalidOperationException($"gateway secret is not set.");
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check for the custom header
        if (context.Request.Path.StartsWithSegments("/api") &&
     (!context.Request.Headers.TryGetValue("X-Gateway-Secret", out var secret)
      || secret != _gatewaySecret))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Access denied. Requests must come from the API gateway.");
            return;
        }


        await _next(context);
    }
}
