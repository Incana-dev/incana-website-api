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
        public async Task<ActionResult<IEnumerable<Article>>> GetArticles()
        {

            var articles = await _context.Articles
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.PublishedDate,
                    AuthorUsername = a.Author.UserName,
                    a.Content 
                })
                .OrderByDescending(a => a.PublishedDate)
                .ToListAsync();

            return Ok(articles);
        }

        // GET: api/articles/{id}
        // Comments functionality isn't implemented yet, but eh. I'll work on it later :V
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Article>> GetArticle(int id)
        {
            var article = await _context.Articles
                .Include(a => a.Author)
                .Include(a => a.Comments)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
            {
                return NotFound();
            }

            var result = new
            {
                article.Id,
                article.Title,
                article.Content,
                article.PublishedDate,
                Author = new { article.Author.UserName },
                Comments = article.Comments.Select(c => new
                {
                    c.Id,
                    c.Content,
                    c.AuthorEmail,
                    c.PostedDate,
                    c.ParentCommentId
                })
            };

            return Ok(result);
        }

        // POST: api/articles
        [HttpPost]
        [Authorize] // Only authenticated users can create articles
        public async Task<ActionResult<Article>> PostArticle(ArticleCreateModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("User ID could not be determined from token.");
            }

            var article = new Article
            {
                Title = model.Title,
                Content = model.Content,
                AuthorId = userId,
                PublishedDate = DateTime.UtcNow
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetArticle), new { id = article.Id }, article);
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
