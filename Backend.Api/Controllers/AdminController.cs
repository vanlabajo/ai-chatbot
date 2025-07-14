using Backend.Api.Hubs;
using Backend.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController(IChatSessionService chatSessionService, ICacheService cacheService, IHubContext<ChatHub> hubContext) : ControllerBase
    {
        private readonly IChatSessionService _chatSessionService = chatSessionService;
        private readonly ICacheService _cacheService = cacheService;
        private readonly IHubContext<ChatHub> _hubContext = hubContext;

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
        {
            var sessions = await _chatSessionService.GetAllSessionsAsync(cancellationToken);
            return Ok(sessions);
        }

        [HttpDelete("sessions/{userId}/{sessionId}")]
        public async Task<IActionResult> DeleteSession(string userId, string sessionId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest("User ID and Session ID cannot be empty.");
            }
            try
            {
                await _chatSessionService.DeleteSessionAsync(userId, sessionId, cancellationToken);
                await _cacheService.RemoveAsync($"session-{userId}", cancellationToken: cancellationToken);
                await _hubContext.Clients.All.SendAsync(HubEventNames.SessionDelete, sessionId, cancellationToken);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
