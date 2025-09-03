using Microsoft.AspNetCore.Mvc;
using eBookLibrary.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;

public class HomeController : Controller
{
    private readonly LibraryContext _context;

    // Inject LibraryContext via Dependency Injection
    public HomeController(LibraryContext context)
    {
        _context = context;
    }

    
    
     public IActionResult About()
     {
        return View();
     }
    
    // Default Index Action for displaying feedback
    [HttpGet]
    [Route("")]
    [Route("Home/Index")]
    public IActionResult Index()
    {
        // Fetch the latest 10 feedback entries
        var feedbacks = _context.ServiceFeedbacks
            .FromSqlRaw("SELECT TOP 10 sf.*, u.Email FROM ServiceFeedback sf JOIN Users u ON sf.UserId = u.UserId ORDER BY sf.CreatedAt DESC")
            .ToList();

        // Ensure feedbacks is initialized
        ViewBag.Feedbacks = feedbacks ?? new List<ServiceFeedback>();

        return View("Index"); // Explicitly specify the view name
    }

    // Search Books Action
    [HttpGet]
    [Route("Home/SearchBooks")]
    public IActionResult SearchBooks(string searchQuery)
    {
        // Fetch books from the database
        var books = string.IsNullOrEmpty(searchQuery)
            ? _context.Books.ToList() // If no search query, get all books
            : _context.Books
                .Where(b => b.Title.Contains(searchQuery) || b.Author.Contains(searchQuery))
                .ToList(); // Filter books by title or author

        return View("SearchBooks", books); // Specify a unique view for searching books
    }

    // Test Database Connection (Optional debugging endpoint)
    [HttpGet]
    [Route("Home/TestConnection")]
    public IActionResult TestConnection()
    {
        try
        {
            _context.Database.OpenConnection(); // Try to open a database connection
            _context.Database.CloseConnection(); // Close the connection
            return Content("Database connection successful!");
        }
        catch (Exception ex)
        {
            return Content($"Database connection failed: {ex.Message}");
        }
    }
}
