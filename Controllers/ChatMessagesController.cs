using IncanaPortfolio.Data;
using IncanaPortfolio.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace IncanaPortfolio.Api.Controllers
{

    [ApiController]
    [Route("api/chat")]
    public class ChatMessagesController : ControllerBase
    {
        private readonly IncanaPortfolioDbContext _context;

        public ChatMessagesController(IncanaPortfolioDbContext context)
        {
            _context = context;
        }

        // GET api/chat
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMessages()
        {
            var messages = await _context.ChatMessages
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
            return Ok(messages);
        }


        // POST api/chat
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> PostMessage(ChatMessage model)
        {
            var message = new ChatMessage
            {
                SenderName = model.SenderName,
                MessageContent = model.MessageContent,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new { status = "message sent :)" });
        }

        // PUT: api/chat/{id}/read
        [HttpPut("{id}/read")]
        [Authorize]
        public async Task<IActionResult> MarkMessageAsRead(int id)
        {
            var message = await _context.ChatMessages.FindAsync(id);

            if (message == null)
            {
                return NotFound();
            }

            message.IsRead = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }


    }

    public class ChatMessageCreateModel
    {
        [Required]
        [MaxLength(100)]
        public required string SenderName { get; set; }

        [Required]
        [MaxLength(2000)]
        public required string MessageContent { get; set; }
    }
}
