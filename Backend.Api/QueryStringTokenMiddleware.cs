namespace Backend.Api
{
    public class QueryStringTokenMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Query.TryGetValue("access_token", out var token))
            {
                context.Request.Headers.Authorization = $"Bearer {token}";
            }
            await _next(context);
        }
    }
}
