using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using eBookLibrary.Models;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

public class AdminController : Controller
{
    private readonly LibraryContext _context;
    private readonly ILogger<AdminController> _logger;

    // Inject LibraryContext via DI
    public AdminController(LibraryContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Middleware: Check Admin permissions
    private IActionResult EnsureAdminAccess()
    {
        if (HttpContext.Session.GetString("Role") != "Admin")
        {
            return RedirectToAction("Login", "Account");
        }
        return null;
    }

    public IActionResult Dashboard()
    {
        var accessCheck = EnsureAdminAccess();
        if (accessCheck != null) return accessCheck;

        // Pass statistics to ViewBag
        ViewBag.TotalUsers = _context.Users.Count();
        ViewBag.TotalBooks = _context.Books.Count();
        ViewBag.BorrowedBooks = _context.BorrowedBooks.Count();
        ViewBag.PendingNotifications = _context.WaitingList.Count(w => !w.IsNotified);

        return View();
    }

    public IActionResult ManageBooks(string searchQuery)
    {
        var accessCheck = EnsureAdminAccess();
        if (accessCheck != null) return accessCheck;

        var books = string.IsNullOrEmpty(searchQuery)
            ? _context.Books.ToList()
            : _context.Books.Where(b => b.Title.Contains(searchQuery) || b.Author.Contains(searchQuery)).ToList();

        return View(books);
    }

    public IActionResult AddBook()
    {
        var accessCheck = EnsureAdminAccess();
        if (accessCheck != null) return accessCheck;

        var book = new Book(); // Initialize a new book
        return View(book);
    }

    [HttpPost]
    public async Task<IActionResult> AddBook(Book book)
    {
        var accessCheck = EnsureAdminAccess();
        if (accessCheck != null) return accessCheck;

        // Handle CoverImage being optional
        if (string.IsNullOrEmpty(book.CoverImage))
        {
            book.CoverImage = "default_image_url";  // Use a default image URL or leave it empty.
        }

        // Ignore ratings field (optional)
        if (book.Ratings == null)
        {
            book.Ratings = new List<Rating>();  // Empty collection if no ratings are provided
        }

        if (ModelState.IsValid)
        {
            _context.Books.Add(book); // Add the book to the database
            await _context.SaveChangesAsync(); // Save changes asynchronously
            TempData["SuccessMessage"] = "Book added successfully!"; // Success feedback
            return RedirectToAction("ManageBooks"); // Redirect to ManageBooks
        }

        return View(book); // Return the form with validation errors if not valid
    }

    public IActionResult EditBook(int id)
    {
        var accessCheck = EnsureAdminAccess();
        if (accessCheck != null) return accessCheck;

        var book = _context.Books.FirstOrDefault(b => b.BookId == id);
        if (book == null)
        {
            return NotFound();
        }

        return View(book);
    }


    [HttpPost]
    [Route("Admin/EditBook")]
    public IActionResult EditBook(Book model)
    {
        // Ensure user has the required permissions (optional)
        var accessCheck = EnsureAdminAccess();
        if (accessCheck != null) return accessCheck;

        if (ModelState.IsValid)
        {
            // Retrieve the book from the database
            var book = _context.Books.FirstOrDefault(b => b.BookId == model.BookId);
            if (book != null)
            {
                // Validate the discounted price
                if (model.DiscountedPrice.HasValue && model.DiscountedPrice.Value >= model.PricePurchase)
                {
                    TempData["DiscountError"] = "The discounted price must be lower than the original price.";
                    return RedirectToAction("EditBook", new { id = model.BookId });
                }

                // Validate the discount end date
                if (model.DiscountEndDate.HasValue && model.DiscountEndDate.Value > DateTime.Now.AddDays(7))
                {
                    TempData["DateError"] = "The sale cannot be longer than 7 days.";
                    return RedirectToAction("EditBook", new { id = model.BookId });
                }

                // Update book properties
                book.Title = model.Title;
                book.Author = model.Author;
                book.Publisher = model.Publisher;
                book.PricePurchase = model.PricePurchase;
                book.DiscountedPrice = model.DiscountedPrice;
                book.DiscountEndDate = model.DiscountEndDate;
                book.Format = model.Format;
                book.AvailableCopies = model.AvailableCopies;
                book.Genre = model.Genre;
                book.Year = model.Year;
                book.IsBorrowable = model.IsBorrowable;
                book.IsBuyOnly = model.IsBuyOnly;
                book.CoverImage = string.IsNullOrEmpty(model.CoverImage) ? "default_image_url" : model.CoverImage;

                // Save changes to the database
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Book updated successfully!";
                return RedirectToAction("ManageBooks");
            }

            ModelState.AddModelError("", "Book not found.");
        }

        return View(model); // Return the form with validation errors
    }













    public IActionResult DeleteBook(int id)
    {
        var accessCheck = EnsureAdminAccess();
        if (accessCheck != null) return accessCheck;

        try
        {
            // Fetch the book from the database
            var book = _context.Books.FirstOrDefault(b => b.BookId == id);
            if (book == null)
            {
                TempData["ErrorMessage"] = "The book you are trying to delete does not exist.";
                return RedirectToAction("ManageBooks");
            }

            // Check for related records (Reviews)
            var relatedReviews = _context.Reviews.Where(r => r.BookId == id).ToList();
            if (relatedReviews.Any())
            {
                _context.Reviews.RemoveRange(relatedReviews); // Remove related reviews
            }

            // Check for related borrow history
            var relatedBorrows = _context.BorrowedBooks.Where(b => b.BookId == id).ToList();
            if (relatedBorrows.Any())
            {
                _context.BorrowedBooks.RemoveRange(relatedBorrows); // Remove related borrow records
            }

            // Check for waiting list entries
            var relatedWaitingList = _context.WaitingList.Where(w => w.BookId == id).ToList();
            if (relatedWaitingList.Any())
            {
                _context.WaitingList.RemoveRange(relatedWaitingList); // Remove related waiting list entries
            }

            // Remove the book
            _context.Books.Remove(book);
            _context.SaveChanges();

            // Set success message
            TempData["SuccessMessage"] = $"The book '{book.Title}' was successfully deleted.";
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger.LogError(ex, "An error occurred while deleting the book with ID {BookId}", id);

            // Set error message
            TempData["ErrorMessage"] = "An error occurred while deleting the book. Please try again.";
        }

        return RedirectToAction("ManageBooks");
    }





    // --------------------------- Finish -------------------------------



    public IActionResult ManageUsers()
    {
        var accessCheck = EnsureAdminAccess();
        if (accessCheck != null) return accessCheck;

        var users = _context.Users.ToList();
        return View(users);
    }

    // GET: Load the Edit User form
    [HttpGet]
    public IActionResult EditUser(int id)
    {
        var accessCheck = EnsureAdminAccess();
        if (accessCheck != null) return accessCheck;

        var user = _context.Users.FirstOrDefault(u => u.UserId == id);
        if (user == null)
        {
            return NotFound();
        }

        return View(user); // Pass the user model to the view
    }

    // POST: Save the edited user details
    [HttpPost]
    public IActionResult EditUser(User model)
    {
        var accessCheck = EnsureAdminAccess();
        if (accessCheck != null) return accessCheck;

        // Fetch the user from the database
        var user = _context.Users.FirstOrDefault(u => u.UserId == model.UserId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("ManageUsers");
        }

        // Update user properties
        user.Email = model.Email;
        user.Role = model.Role;

        // Save changes to the database
        _context.SaveChanges();

        TempData["SuccessMessage"] = "User details updated successfully.";
        return RedirectToAction("ManageUsers");
    }

    public IActionResult DeleteUser(int id)
    {
        var accessCheck = EnsureAdminAccess();
        if (accessCheck != null) return accessCheck;

        var user = _context.Users.FirstOrDefault(u => u.UserId == id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("ManageUsers");
        }

        // Delete related records in WaitingList
        var waitingListEntries = _context.WaitingList.Where(w => w.UserId == id).ToList();
        if (waitingListEntries.Any())
        {
            _context.WaitingList.RemoveRange(waitingListEntries);
        }

        // Delete the user
        _context.Users.Remove(user);
        _context.SaveChanges();

        TempData["SuccessMessage"] = "User deleted successfully.";
        return RedirectToAction("ManageUsers");
    }





}
