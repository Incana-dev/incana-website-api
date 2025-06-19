using IncanaPortfolio.Data.Models;
using IncanaPortfolio.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;



namespace IncanaPortfolio.Api.Controllers
{
    public class CommentsController : ControllerBase
    {

        private readonly IncanaPortfolioDbContext _context;

        public CommentsController(IncanaPortfolioDbContext context)
        {
            _context = context;
        }


        // GET api/articles/{articleID}/comments
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetComments(int articleId)
        {
            if (!await _context.Articles.AnyAsync(a => a.Id == articleId))
            {
                return NotFound("Article not found.");
            }

            var comments = await _context.Comments
                .Where(c => c.ArticleId == articleId)
                .OrderBy(c => c.PostedDate)
                .Select(c => new
                {
                    c.Id,
                    c.Content,
                    c.AuthorEmail,
                    c.PostedDate,
                    c.ParentCommentId

                })
                .ToListAsync();

            return Ok(comments);
        }



        // POST api/articles/{articleID}/comments
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Postcomment(int articleId, CommentCreateModel model)
        {
            if (!await _context.Articles.AnyAsync(a => a.Id == articleId))
            {
                return NotFound("Article not found.");
            }

            if (model.ParentCommentId.HasValue)
            { 
                var parent = await _context.Comments
                    .AnyAsync(c => c.Id == model.ParentCommentId.Value && c.ArticleId == articleId);

                if (!parent)
                {
                    return BadRequest("Parent Comment not found!");
                }

            }

            var comment = new Comment
            {
                Content = model.Content,
                AuthorEmail = model.AuthorEmail,
                ArticleId = articleId,
                ParentCommentId = model.ParentCommentId,
                PostedDate = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var result = new {
                comment.Id,
                comment.Content,
                comment.AuthorEmail,
                comment.PostedDate,
                comment.ParentCommentId
            };
            return CreatedAtAction(nameof(GetComments), new { articleId = articleId }, result);
        }



        public class CommentCreateModel
        {
            [Required]
            [MaxLength(2000)]
            public required string Content { get; set; }

            [Required]
            [EmailAddress]
            [MaxLength(256)]
            public required string AuthorEmail { get; set; }

            public int? ParentCommentId { get; set; } // Optional: for replies
        }
    }
}
