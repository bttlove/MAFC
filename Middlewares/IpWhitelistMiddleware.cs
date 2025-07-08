// Middlewares/IpWhitelistMiddleware.cs
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace pviBase.Middlewares
{
    public class IpWhitelistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<IpWhitelistMiddleware> _logger;
        private readonly string[] _whitelistedIps;

        public IpWhitelistMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<IpWhitelistMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _whitelistedIps = configuration.GetSection("IpWhitelist:WhitelistedIps").Get<string[]>() ?? new string[0];
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (_whitelistedIps.Length == 0)
            {
                _logger.LogWarning("IP Whitelist is enabled but no IPs are configured.");
                await _next(context);
                return;
            }

            var remoteIp = context.Connection.RemoteIpAddress;
            _logger.LogInformation($"Request from IP: {remoteIp}");

            if (remoteIp != null && _whitelistedIps.Any(ip => IPAddress.Parse(ip).Equals(remoteIp)))
            {
                await _next(context);
            }
            else
            {
                _logger.LogWarning($"Forbidden: Request from untrusted IP address: {remoteIp}");
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync("Forbidden: Your IP address is not whitelisted.");
            }
        }
    }

    public static class IpWhitelistMiddlewareExtensions
    {
        public static IApplicationBuilder UseIpWhitelistMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<IpWhitelistMiddleware>();
        }
    }
}