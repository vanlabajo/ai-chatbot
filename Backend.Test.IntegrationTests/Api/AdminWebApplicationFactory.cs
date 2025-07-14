using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Backend.Test.IntegrationTests.Api
{
    public class AdminWebApplicationFactory : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureTestServices(services =>
            {
                // Remove the previous handler if present
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = AdminTestingAuthHandler.AuthenticationScheme;
                    options.DefaultScheme = AdminTestingAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = AdminTestingAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, AdminTestingAuthHandler>(AdminTestingAuthHandler.AuthenticationScheme, options => { });
            });
        }
    }

    class AdminTestingAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string AuthenticationScheme = "AdminTestScheme";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim(ClaimConstants.ObjectId, "abec87ca-d675-4072-bbe4-381717cc86a6") };
            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
