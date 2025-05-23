using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Backend.Test.IntegrationTests.Api
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Clear();
                var newConfig = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddUserSecrets<CustomWebApplicationFactory>();

                config.AddConfiguration(newConfig.Build());
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestingAuthHandler.AuthenticationScheme;
                    options.DefaultScheme = TestingAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestingAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestingAuthHandler>(TestingAuthHandler.AuthenticationScheme, options => { });

                services.AddAuthorizationBuilder()
                    .SetDefaultPolicy(new AuthorizationPolicyBuilder(TestingAuthHandler.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build());
            });
        }
    }

    class TestingAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string AuthenticationScheme = "TestScheme";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "test-user") };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            var result = AuthenticateResult.Success(ticket);

            return Task.FromResult(result);
        }
    }

    public class AllowedOriginsFactory : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(static (context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AllowedOrigins:0"] = "http://localhost:3000"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestingAuthHandler.AuthenticationScheme;
                    options.DefaultScheme = TestingAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestingAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestingAuthHandler>(TestingAuthHandler.AuthenticationScheme, options => { });

                services.AddAuthorizationBuilder()
                    .SetDefaultPolicy(new AuthorizationPolicyBuilder(TestingAuthHandler.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build());
            });
        }
    }

    public class EmptyOriginsFactory : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(static (context, config) =>
            {
                config.Sources.Clear();
                config.AddInMemoryCollection([]);
            });

            builder.ConfigureTestServices(services =>
            {

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestingAuthHandler.AuthenticationScheme;
                    options.DefaultScheme = TestingAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestingAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestingAuthHandler>(TestingAuthHandler.AuthenticationScheme, options => { });

                services.AddAuthorizationBuilder()
                    .SetDefaultPolicy(new AuthorizationPolicyBuilder(TestingAuthHandler.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build());
            });
        }
    }

}
