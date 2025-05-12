using Azure;
using Azure.AI.OpenAI;
using Backend.Api;
using Backend.Core;
using Backend.Infrastructure;
using Backend.Infrastructure.AzureOpenAI;
using Backend.Infrastructure.Tiktoken;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Tiktoken;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// This is required to support multiple audiences e.g. for Swagger UI and the API
builder.Services.Configure<JwtBearerOptions>(
      JwtBearerDefaults.AuthenticationScheme,
      options => options.TokenValidationParameters.ValidAudiences = builder.Configuration.GetSection("AzureAd:Audiences").Get<string[]>());

builder.Services
    .AddMemoryCache()
    .Configure<AzureOpenAIOptions>(builder.Configuration.GetSection(AzureOpenAIOptions.AzureOpenAI))
    .AddScoped<ICacheService, InMemoryCacheService>()
    .AddScoped<IOpenAIService, OpenAIService>()
    .AddScoped<ITokenizerService, TokenizerService>()
    .AddTransient<ExceptionHandlingMiddleware>()
    .AddSingleton(ModelToEncoder.For("gpt-4"))
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

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
    options.AddPolicy("AllowSpecificOrigins",
        builder =>
        {
            builder.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader();
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
                { "resource", builder.Configuration["SwaggerUI:ClientId"]!}
            });
    });
}

app.UseWebSockets();
app.UseMiddleware<WebSocketAuthenticationMiddleware>();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseCors("AllowSpecificOrigins");

app.MapControllers();

app.Run();

// Add this to make WebApplicationFactory work
public partial class Program { }
