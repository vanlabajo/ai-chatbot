namespace Backend.Api
{
    public class WebSocketAuthenticationMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                // Check for token in the query string
                if (context.Request.Query.TryGetValue("access_token", out var token))
                {
                    context.Request.Headers["Authorization"] = $"Bearer {token}";
                }
                else
                {
                    // If no token is found, return a 401 Unauthorized response
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }
            }

            await _next(context);
        }
    }
}
