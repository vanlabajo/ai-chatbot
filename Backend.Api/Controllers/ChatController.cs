using Backend.Core;
using Backend.Core.DTOs;
using Backend.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController(IOpenAIService openAiService) : ControllerBase
    {
        private readonly IOpenAIService _openAiService = openAiService;

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Message cannot be empty.");

            var response = await _openAiService.GetChatResponseAsync([
                new ChatMessage { Role = "user", Content = request.Message}
            ]);
            return Ok(new ChatResponse { Response = response.Content });
        }
    }
}
