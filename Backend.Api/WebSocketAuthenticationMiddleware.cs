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
                    context.Request.Headers.Authorization = $"Bearer {token}";
                }
            }

            await _next(context);
        }
    }
}
