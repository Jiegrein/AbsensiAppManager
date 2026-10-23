using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AbsensiAppWebApi.Infrastructure;

/// <summary>
/// Requires the <c>X-Admin-Key</c> header to match the <c>Admin:Key</c> setting. Fails closed:
/// with no key configured every request is refused, so a misconfigured deployment cannot
/// expose the payroll export by accident.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AdminKeyAttribute : Attribute, IAsyncAuthorizationFilter
{
    public const string HeaderName = "X-Admin-Key";
    public const string ConfigurationKey = "Admin:Key";

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var configured = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>()[ConfigurationKey];
        if (string.IsNullOrWhiteSpace(configured))
        {
            context.HttpContext.RequestServices.GetRequiredService<ILogger<AdminKeyAttribute>>()
                .LogWarning("Admin request refused: '{Setting}' is not configured.", ConfigurationKey);
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }

        var presented = context.HttpContext.Request.Headers[HeaderName].ToString();
        if (!FixedTimeEquals(presented, configured))
        {
            context.Result = new UnauthorizedResult();
        }
        return Task.CompletedTask;
    }

    private static bool FixedTimeEquals(string presented, string expected) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presented), Encoding.UTF8.GetBytes(expected));
}
