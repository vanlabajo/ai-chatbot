using Azure;
using Azure.AI.OpenAI;
using Backend.Api;
using Backend.Core;
using Backend.Infrastructure.AzureOpenAI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services
    .Configure<AzureOpenAIOptions>(builder.Configuration.GetSection(AzureOpenAIOptions.AzureOpenAI))
    .AddScoped<IOpenAIService, OpenAIService>()
    .AddTransient<ExceptionHandlingMiddleware>()
    .AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    })
    .AddSingleton(sp =>
    {
        var options = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
        var azureClient = new AzureOpenAIClient(new Uri(options.Endpoint), new AzureKeyCredential(options.ApiKey));
        return azureClient.GetChatClient(options.DeploymentName);
    });

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1.0", new OpenApiInfo { Title = "AI Chatbot", Version = "v1.0" });
    opt.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "OAuth2.0 Auth Code with PKCE",
        Name = "oauth2",
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{builder.Configuration["AzureAd:Instance"]!.TrimEnd('/')}/{builder.Configuration["AzureAd:TenantId"]}/oauth2/authorize"),
                Scopes = new Dictionary<string, string>
                {
                    { builder.Configuration["SwaggerUI:Scope"]!, "Access the API" }
                }
            }
        }
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            },
            new[] { builder.Configuration["SwaggerUI:Scope"] }
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsProduction())
{
    app.UseDeveloperExceptionPage();
    builder.Configuration.AddUserSecrets<Program>();
    app.UseSwagger();
    app.UseSwaggerUI(opt =>
    {
        opt.SwaggerEndpoint("/swagger/v1.0/swagger.json", "AI Chatbot v1.0");
        opt.OAuthClientId(builder.Configuration["AzureAd:ClientId"]);
        opt.OAuthScopeSeparator(" ");
        opt.OAuthUsePkce();
        opt.OAuthAdditionalQueryStringParams(new Dictionary<string, string>()
            {
                { "resource", builder.Configuration["AzureAd:ClientId"]!}
            });
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Add this to make WebApplicationFactory work
public partial class Program { }
