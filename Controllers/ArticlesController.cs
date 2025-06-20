using IncanaPortfolio.Data;
using IncanaPortfolio.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IncanaPortfolio.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArticlesController : ControllerBase
    {
        private readonly IncanaPortfolioDbContext _context;

        public ArticlesController(IncanaPortfolioDbContext context)
        {
            _context = context;
        }

        // GET: api/articles
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ArticleDto>>> GetArticles()
        {
            var articles = await _context.Articles
                .Select(a => new ArticleDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    PublishedDate = a.PublishedDate,
                    AuthorUsername = a.Author.UserName,
                    Content = a.Content
                })
                .OrderByDescending(a => a.PublishedDate)
                .ToListAsync();
            return Ok(articles);
        }

        // GET: api/articles/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ArticleDto>> GetArticle(int id)
        {
            var article = await _context.Articles
                .Include(a => a.Author)
                .Include(a => a.Comments)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
            {
                return NotFound();
            }

            var result = new ArticleDto
            {
                Id = article.Id,
                Title = article.Title,
                Content = article.Content,
                PublishedDate = article.PublishedDate,
                AuthorUsername = article.Author.UserName,
                Comments = article.Comments.Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    AuthorEmail = c.AuthorEmail,
                    PostedDate = c.PostedDate,
                    ParentCommentId = c.ParentCommentId
                }).ToList()
            };

            return Ok(result);
        }

        // POST: api/articles
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ArticleDto>> PostArticle(ArticleCreateModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("User ID could not be determined from token.");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return BadRequest("Author not found.");
            }

            var article = new Article
            {
                Title = model.Title,
                Content = model.Content,
                AuthorId = userId,
                Author = user,
                PublishedDate = System.DateTime.UtcNow
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            var result = new ArticleDto
            {
                Id = article.Id,
                Title = article.Title,
                Content = article.Content,
                PublishedDate = article.PublishedDate,
                AuthorUsername = article.Author.UserName,
                Comments = new List<CommentDto>()
            };

            return CreatedAtAction(nameof(GetArticle), new { id = article.Id }, result);
        }

        // PUT: api/articles/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutArticle(int id, ArticleUpdateModel model)
        {
            var article = await _context.Articles.FindAsync(id);

            if (article == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (article.AuthorId != userId)
            {
                return Forbid("You are not authorized to edit this article.");
            }

            article.Title = model.Title;
            article.Content = model.Content;

            _context.Entry(article).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Articles.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/articles/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteArticle(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (article.AuthorId != userId)
            {
                return Forbid("You are not authorized to delete this article.");
            }

            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }


    public class ArticleDto
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public System.DateTime PublishedDate { get; set; }
        public required string AuthorUsername { get; set; }
        public List<CommentDto> Comments { get; set; } = new List<CommentDto>();
    }

    public class CommentDto
    {
        public int Id { get; set; }
        public required string Content { get; set; }
        public required string AuthorEmail { get; set; }
        public System.DateTime PostedDate { get; set; }
        public int? ParentCommentId { get; set; }
    }


    public class ArticleCreateModel
    {
        public required string Title { get; set; }
        public required string Content { get; set; }
    }

    public class ArticleUpdateModel
    {
        public required string Title { get; set; }
        public required string Content { get; set; }
    }
}
