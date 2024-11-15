using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using CareerAI.Services;
using CareerAI.Models;
using CareerAI.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json; // Add this using directive
using Newtonsoft.Json.Linq; // Add this using directive

namespace CareerAI.Controllers
{
    public class ChatController : Controller
    {
        private readonly GoogleApiService _googleApiService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(GoogleApiService googleApiService, ILogger<ChatController> logger)
        {
            _googleApiService = googleApiService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var chatHistory = HttpContext.Session.GetObjectFromJson<List<ChatMessage>>("ChatHistory") ?? new List<ChatMessage>();
            return View(chatHistory);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return BadRequest("Message cannot be empty.");
            }

            try
            {
                var chatHistory = HttpContext.Session.GetObjectFromJson<List<ChatMessage>>("ChatHistory") ?? new List<ChatMessage>();
                chatHistory.Add(new ChatMessage { Role = "user", Parts = new List<ChatPart> { new ChatPart { Text = message } } });

                // Add your system instruction here
                var systemInstruction = new ChatMessage
                {
                    Role = "user",
                    Parts = new List<ChatPart>
                    {
                        new ChatPart
                        {
                            Text = "You are a career assistant or guide that helps students assess their skills and their capabilities to assist them in what career they might want to pursue. Remove the formatting of the message respond as if you are replying with just one paragraph. Please provide concise and accurate responses."
                        }
                    }
                };

                var responseText = await _googleApiService.GenerateContentAsync(chatHistory, systemInstruction);

                chatHistory.Add(new ChatMessage { Role = "model", Parts = new List<ChatPart> { new ChatPart { Text = responseText } } });

                HttpContext.Session.SetObjectAsJson("ChatHistory", chatHistory);

                return Json(new { response = responseText });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request error occurred while sending message.");
                return StatusCode(500, "HTTP request error");
            }
            catch (JsonReaderException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON parsing error occurred while sending message.");
                return StatusCode(500, "JSON parsing error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while sending message.");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}