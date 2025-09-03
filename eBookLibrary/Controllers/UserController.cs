using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using eBookLibrary.Models;
using eBookLibrary.Services;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;


public class UserController : Controller
{
    // ------------------------------ Database context -------------------------------
    private readonly LibraryContext _context;
    private readonly ILogger<UserController> _logger;
    public UserController(LibraryContext context, ILogger<UserController> logger)
    {
        _context = context;
        _logger = logger;
    }
    private IActionResult EnsureUserAccess()
    {
        if (HttpContext.Session.GetString("Role") != "User")
        {
            return RedirectToAction("Login", "Account");
        }
        return null;
    }
    // ------------------------------ Add User to the Waiting list -------------------------------
    private async Task AddToWaitingList(int bookId, int userId)
    {
        // Check if the user is already in the waiting list
        if (_context.WaitingList.Any(w => w.BookId == bookId && w.UserId == userId))
        {
            TempData["Message"] = "You are already in the waiting list for this book.";
            return;
        }

        // Calculate the position and estimated days (waiting time)
        var position = _context.WaitingList.Count(w => w.BookId == bookId) + 1;
        var estimatedDays = position * 30;  // Assuming 30 days per user in the list

        // Add the user to the waiting list
        var waitingEntry = new WaitingList
        {
            BookId = bookId,
            UserId = userId,
            Position = position,
            IsNotified = false,
            EstimatedDays = estimatedDays // Set estimated waiting time
        };

        _context.WaitingList.Add(waitingEntry);
        await _context.SaveChangesAsync();
    }


    // ------------------------------ Notify next user that waiting for a book -------------------------------
    private async Task NotifyNextUserInWaitingList(int bookId)
    {
        var nextUserInLine = await _context.WaitingList
            .Where(w => w.BookId == bookId && !w.IsNotified)
            .OrderBy(w => w.Position)
            .FirstOrDefaultAsync();

        if (nextUserInLine != null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == nextUserInLine.UserId);
            if (user != null)
            {
                var emailService = HttpContext.RequestServices.GetService<EmailService>();
                await emailService.SendEmailAsync(
                    user.Email,
                    "Your Turn to Borrow the Book",
                    $"The book '{nextUserInLine.Book.Title}' is now available. Please borrow it soon!"
                );

                // Mark the user as notified
                nextUserInLine.IsNotified = true;
                await _context.SaveChangesAsync();
            }
        }
    }

    // ------------------------------ Add User to the waiting list -------------------------------


    [HttpGet]
    public IActionResult ConfirmJoinWaitingList()
    {
        var bookId = (int?)TempData["BookId"]; // Retrieve the bookId from TempData
        if (!bookId.HasValue)
        {
            TempData["ErrorMessage"] = "Book not found.";
            return RedirectToAction("ExploreBooks");
        }

        var book = _context.Books.FirstOrDefault(b => b.BookId == bookId.Value);
        if (book == null)
        {
            TempData["ErrorMessage"] = "Book not found.";
            return RedirectToAction("ExploreBooks");
        }

        var waitingListInfo = new WaitingListViewModel
        {
            BookId = book.BookId,
            UserEmail = HttpContext.Session.GetString("Username"),
            Position = _context.WaitingList.Count(w => w.BookId == bookId.Value) + 1,
            IsNotified = false
        };

        ViewBag.BookTitle = book.Title;
        return View("ConfirmJoinWaitingList", waitingListInfo);
    }



    [HttpPost]
    public async Task<IActionResult> AddToWaitingListConfirmed(int bookId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            TempData["ErrorMessage"] = "You must log in to join the waiting list.";
            return RedirectToAction("Login", "Account");
        }

        // Check if already in the waiting list
        if (_context.WaitingList.Any(w => w.BookId == bookId && w.UserId == userId.Value))
        {
            TempData["Message"] = "You are already in the waiting list for this book.";
            return RedirectToAction("ExploreBooks");
        }

        var book = await _context.Books.FirstOrDefaultAsync(b => b.BookId == bookId);
        if (book == null)
        {
            TempData["ErrorMessage"] = "Book not found.";
            return RedirectToAction("ExploreBooks");
        }

        // Calculate the position and estimated days (waiting time)
        var position = _context.WaitingList.Count(w => w.BookId == bookId) + 1;
        var estimatedDays = position * 30;  // Assuming 30 days per user in the list

        // Add the user to the waiting list
        var waitingEntry = new WaitingList
        {
            BookId = bookId,
            UserId = userId.Value,
            Position = position,
            IsNotified = false,
            EstimatedDays = estimatedDays // Set estimated waiting time
        };

        _context.WaitingList.Add(waitingEntry);
        await _context.SaveChangesAsync();

        TempData["Message"] = "You have been added to the waiting list.";
        return RedirectToAction("Library", "User");
    }

    // ------------------------------ User returning a book that he borrowed -------------------------------

    [HttpPost]
    public async Task<IActionResult> ReturnBook(int bookId)
    {
        _logger.LogInformation($"Attempting to return book with BookId: {bookId}");

        // Retrieve the user ID from the authentication claims
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Json(new { success = false, message = "User not authenticated" });
        }

        if (!int.TryParse(userIdString, out int userId))
        {
            return Json(new { success = false, message = "Invalid user ID" });
        }

        // Check if bookId is valid
        if (bookId == 0)
        {
            _logger.LogWarning($"Invalid BookId: {bookId} for UserId: {userId}");
            return Json(new { success = false, message = "Invalid book ID" });
        }

        // Find the borrowed book
        var borrowedBook = await _context.BorrowedBooks
            .FirstOrDefaultAsync(b => b.BookId == bookId && b.UserId == userId);

        if (borrowedBook == null)
        {
            _logger.LogWarning($"No borrowed book found for UserId: {userId} and BookId: {bookId}");
            return Json(new { success = false, message = "You have not borrowed this book." });
        }

        var book = await _context.Books.FirstOrDefaultAsync(b => b.BookId == bookId);
        if (book != null)
        {
            book.AvailableCopies += 1;
        }

        _context.BorrowedBooks.Remove(borrowedBook);
        await _context.SaveChangesAsync();

        // Redirect to BorrowedBooks page
        return RedirectToAction("BorrowedBooks");
    }













    // ------------------------------ User purchasing a book -------------------------------
    public IActionResult PurchaseBook(int bookId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var book = _context.Books.FirstOrDefault(b => b.BookId == bookId);
        if (book == null)
        {
            TempData["Message"] = "The book does not exist.";
            return RedirectToAction("ExploreBooks");
        }

        var purchasedBook = new PurchasedBook
        {
            UserId = userId.Value,
            BookId = bookId,
            PurchaseDate = DateTime.Now
        };

        _context.PurchasedBooks.Add(purchasedBook);
        _context.SaveChanges();

        TempData["Message"] = "Book purchased successfully!";
        return RedirectToAction("ExploreBooks");
    }

    // ------------------------------ Users explore books (The Library of eBook) -------------------------------

    public IActionResult ExploreBooks(string searchQuery, string genre, decimal? minPrice, decimal? maxPrice, int? year, bool onlyDiscounted = false, string sortOrder = null)
    {
        var books = _context.Books
            .Include(b => b.Ratings) // Include Ratings for each book
            .AsQueryable();

        // Filtering by search query
        if (!string.IsNullOrEmpty(searchQuery))
        {
            books = books.Where(b => EF.Functions.Like(b.Title, $"%{searchQuery}%") ||
                                     EF.Functions.Like(b.Author, $"%{searchQuery}%") ||
                                     EF.Functions.Like(b.Publisher, $"%{searchQuery}%"));
        }

        // Filter by genre if provided
        if (!string.IsNullOrEmpty(genre))
        {
            books = books.Where(b => b.Genre == genre);
        }

        // Filter by price range if provided
        if (minPrice.HasValue)
        {
            books = books.Where(b => b.PricePurchase >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            books = books.Where(b => b.PricePurchase <= maxPrice.Value);
        }

        // Filter by year if provided
        if (year.HasValue)
        {
            books = books.Where(b => b.Year == year.Value);
        }

        // Filter by discount if the 'onlyDiscounted' checkbox is checked
        if (onlyDiscounted)
        {
            books = books.Where(b => b.DiscountEndDate != null && b.DiscountEndDate > DateTime.Now);
        }

        // Sorting logic
        switch (sortOrder)
        {
            case "PriceAsc":
                books = books.OrderBy(b => b.PricePurchase); // Price increase
                break;
            case "PriceDesc":
                books = books.OrderByDescending(b => b.PricePurchase); // Price decrease
                break;
            case "MostPopular":
                books = books.OrderByDescending(b => b.Ratings.Count()); // Most popular (based on number of ratings)
                break;
            case "Genre":
                books = books.OrderBy(b => b.Genre); // Genre sorting
                break;
            case "Year":
                books = books.OrderBy(b => b.Year); // Year of publishing sorting
                break;
            default:
                books = books.OrderBy(b => b.Title); // Default sorting by title
                break;
        }

        // Project with AverageRating
        var bookList = books
            .Select(b => new Book
            {
                BookId = b.BookId,
                Title = b.Title,
                Author = b.Author,
                Publisher = b.Publisher,
                Format = b.Format,
                AvailableCopies = b.AvailableCopies,
                Genre = b.Genre,
                Year = b.Year,
                PricePurchase = b.PricePurchase,
                DiscountEndDate = b.DiscountEndDate,
                DiscountedPrice = b.DiscountedPrice,
                CoverImage = b.CoverImage,
                AverageRating = b.Ratings.Any() ? b.Ratings.Average(r => r.RatingValue) : 0
            })
            .ToList();

        return View(bookList);
    }







    // ------------------------------ Personal Library Books -------------------------------
    public IActionResult Library()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        // Fetch Borrowed Books
        var borrowedBooks = _context.BorrowedBooks
            .Where(b => b.UserId == userId.Value && b.ReturnDate > DateTime.Now) // Exclude expired books
            .Include(b => b.Book)
            .ToList();

        // Fetch Purchased Books
        var purchasedBooks = _context.PurchasedBooks
            .Where(p => p.UserId == userId.Value)
            .Include(p => p.Book)
            .ToList();

        // Fetch Waiting List Books and calculate the number of people waiting for each book
        var waitingList = _context.WaitingList
            .Where(w => w.UserId == userId.Value)
            .Include(w => w.Book)
            .ToList();

        // Get the count of people waiting for each book
        var waitingListGrouped = _context.WaitingList
            .GroupBy(w => w.BookId)
            .Select(g => new
            {
                BookId = g.Key,
                WaitingCount = g.Count()
            }).ToList();

        // Create view model
        var viewModel = new UserLibraryViewModel
        {
            BorrowedBooks = borrowedBooks.Select(b => new BorrowedBookViewModel
            {
                BookId = b.Book.BookId,
                Title = b.Book.Title,
                Author = b.Book.Author,
                DueDate = b.ReturnDate,
                CoverImage = b.Book.CoverImage,
                Reviews = b.Book.Reviews
            }).ToList(),

            PurchasedBooks = purchasedBooks.Select(p => new PurchasedBookViewModel
            {
                BookId = p.Book.BookId,
                Title = p.Book.Title,
                Author = p.Book.Author,
                PurchaseDate = p.PurchaseDate,
                CoverImage = p.Book.CoverImage,
                Reviews = p.Book.Reviews
            }).ToList(),

            // Map WaitingList to WaitingBookViewModel and add Position
            WaitingList = waitingList.Select(w => new WaitingBookViewModel
            {
                BookId = w.Book.BookId,
                Title = w.Book.Title,
                CoverImage = w.Book.CoverImage,
                EstimatedDays = w.EstimatedDays,
                Position = w.Position,
                WaitingCount = waitingListGrouped.FirstOrDefault(wg => wg.BookId == w.BookId)?.WaitingCount ?? 0 // Add the waiting count for each book
            }).ToList(),

            Notifications = new List<string>() // Add logic for notifications if needed
        };

        return View(viewModel);
    }



    public IActionResult Explore()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Set a flag to indicate explore mode
        ViewBag.ExploreMode = true;
        return View("Library"); // Assuming "Library" is the view you want to display for Explore
    }

    [HttpGet]
    [Route("User/Checkout")]
    public IActionResult Checkout(int bookId, string actionType)
    {
        var book = _context.Books.FirstOrDefault(b => b.BookId == bookId);
        if (book == null)
        {
            TempData["Message"] = "This book does not exist.";
            return RedirectToAction("ExploreBooks");
        }

        HttpContext.Session.Set("SelectedBook", new ShoppingCart
        {
            BookId = book.BookId,
            Title = book.Title,
            Price = actionType == "borrow" ? 0 : book.PricePurchase // Borrowing is free
        });
        HttpContext.Session.SetString("ActionType", actionType);

        return View("Checkout", book);
    }

    [HttpPost]
    [Route("User/ProcessPayment/SingleBook")]
    public IActionResult ProcessPaymentSingleBook(string cardNumber, string expiry, string cvv, int bookId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            TempData["ErrorMessage"] = "You need to log in to purchase a book.";
            return RedirectToAction("Login", "Account");
        }

        var book = _context.Books.FirstOrDefault(b => b.BookId == bookId);
        if (book == null)
        {
            TempData["ErrorMessage"] = "The book does not exist.";
            return RedirectToAction("ExploreBooks");
        }

        // Simulate payment processing
        var paymentResult = FakePaymentGateway.ProcessPayment(cardNumber, expiry, cvv, book.PricePurchase);

        if (!paymentResult.IsSuccess)
        {
            TempData["ErrorMessage"] = "Payment failed. Please try again.";
            return RedirectToAction("Checkout", new { bookId, actionType = "purchase" });
        }

        // Add the purchased book to the database
        var purchasedBook = new PurchasedBook
        {
            UserId = userId.Value,
            BookId = book.BookId,
            Title = book.Title,
            Author = book.Author,
            PurchaseDate = DateTime.Now
        };

        _context.PurchasedBooks.Add(purchasedBook);
        _context.SaveChanges();

        TempData["SuccessMessage"] = $"Payment successful! '{book.Title}' has been added to your library.";

        // Redirect to PurchasedBooks
        return RedirectToAction("PurchasedBooks", "User");
    }


    // ------------------------------ Payment -------------------------------

    [HttpPost]
    public IActionResult ProcessPayment(string cardNumber, string expiry, string cvv, int bookId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            TempData["ErrorMessage"] = "You need to log in to purchase a book.";
            return RedirectToAction("Login", "Account");
        }

        var book = _context.Books.FirstOrDefault(b => b.BookId == bookId);
        if (book == null)
        {
            TempData["ErrorMessage"] = "Book not found.";
            return RedirectToAction("ExploreBooks");
        }
        var paymentResult = FakePaymentGateway.ProcessPayment(cardNumber, expiry, cvv, book.PricePurchase);

        if (!paymentResult.IsSuccess)
        {
            TempData["ErrorMessage"] = "Payment failed. Please try again.";
            return RedirectToAction("Checkout", new { bookId, actionType = "purchase" });
        }

        // Update AvailableCopies (only if not unlimited copies)
        if (book.AvailableCopies > 0)
        {
            book.AvailableCopies -= 1; // Decrement available copies
        }

        // Save the purchased book record
        var purchasedBook = new PurchasedBook
        {
            UserId = userId.Value,
            BookId = book.BookId,
            Title = book.Title,
            Author = book.Author, // Ensure Author is populated
            PurchaseDate = DateTime.Now
        };

        _context.PurchasedBooks.Add(purchasedBook);
        _context.SaveChanges();

        TempData["SuccessMessage"] = $"Payment successful! '{book.Title}' has been added to your library.";
        return RedirectToAction("PurchasedBooks");
    }

    // ------------------------------ Purchased Books -------------------------------
    public IActionResult PurchasedBooks()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var purchasedBooks = _context.PurchasedBooks
            .Where(p => p.UserId == userId.Value)
            .Select(p => new PurchasedBookViewModel
            {
                BookId = p.BookId,
                Title = p.Book.Title,
                Author = p.Book.Author,
                PurchaseDate = p.PurchaseDate,
                Reviews = p.Book.Reviews 
            })
            .ToList();

        return View(purchasedBooks);
    }

    // ------------------------------ Borrowed Books -------------------------------
    [HttpGet]
    public IActionResult BorrowedBooks()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account");

        var borrowedBooks = _context.BorrowedBooks
            .Where(b => b.UserId == userId.Value && b.ReturnDate > DateTime.Now)
            .Include(b => b.Book)  // Ensure Book is included
            .ToList();

        var borrowedBookViewModels = borrowedBooks.Select(b => new BorrowedBookViewModel
        {
            BookId = b.Book.BookId,
            Title = b.Book.Title,
            Author = b.Book.Author,
            DueDate = b.ReturnDate,
            CoverImage = b.Book.CoverImage,
            Reviews = b.Book.Reviews  // Fetch reviews for the book
        }).ToList();

        return View(borrowedBookViewModels);
    }

    // ------------------------------ Get Borrowed Books -------------------------------
    [HttpGet]
    public IActionResult GetBorrowedBooks()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return Json(new { success = false, message = "User not found." });
        }

        var borrowedBooks = _context.BorrowedBooks
            .Include(b => b.Book)
            .Where(b => b.UserId == userId.Value)
            .ToList();

        return PartialView("BorrowedBooksPartial", borrowedBooks);
    }


    [HttpGet]
    public IActionResult GetPurchasedBooks()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return Json(new { success = false, message = "User not found." });
        }

        var purchasedBooks = _context.PurchasedBooks
            .Include(p => p.Book)
            .Where(p => p.UserId == userId.Value)
            .ToList();

        return PartialView("_PurchasedBooksPartial", purchasedBooks);
    }

    // ------------------------------ User borrowing a book -------------------------------
    [HttpPost]
    public async Task<IActionResult> BorrowBook(int bookId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get the user ID from Claims
        if (string.IsNullOrEmpty(userIdString))
        {
            return RedirectToAction("Login", "Account");
        }

        if (int.TryParse(userIdString, out int userId))
        {
            try
            {
                _logger.LogInformation($"Attempting to borrow book with BookId: {bookId} for UserId: {userId}");

                var book = await _context.Books.FirstOrDefaultAsync(b => b.BookId == bookId);
                if (book == null)
                {
                    _logger.LogWarning($"Book with BookId: {bookId} does not exist.");
                    TempData["ErrorMessage"] = "The book does not exist.";
                    return RedirectToAction("ExploreBooks");
                }

                // Check if the book is available
                if (book.AvailableCopies > 0)
                {
                    // If available copies > 0, allow borrowing
                    var borrowedBook = new BorrowedBook
                    {
                        UserId = userId,
                        BookId = bookId,
                        BorrowDate = DateTime.Now,
                        ReturnDate = DateTime.Now.AddDays(30) // Set the return date for 30 days
                    };

                    _context.BorrowedBooks.Add(borrowedBook);
                    book.AvailableCopies -= 1;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Book borrowed successfully!";
                }
                else
                { 
                    TempData["BookId"] = bookId; // Store bookId to use when joining the waiting list
                    return RedirectToAction("ConfirmJoinWaitingList");
                }

                return RedirectToAction("BorrowedBooks"); // Redirect to borrowed books view
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while borrowing the book.");
                TempData["ErrorMessage"] = "An error occurred while borrowing the book. Please try again later.";
            }
        }
        else
        {
            _logger.LogWarning("Invalid user ID.");
            TempData["ErrorMessage"] = "Invalid user ID.";
        }

        return RedirectToAction("ExploreBooks");
    }



    // GET: Redirect to the review page
    [HttpGet]
    public IActionResult RateBook(int bookId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Fetch the book by its ID
        var book = _context.Books.FirstOrDefault(b => b.BookId == bookId);
        if (book == null)
        {
            TempData["ErrorMessage"] = "Book not found.";
            return RedirectToAction("Library");
        }

        return View(book); // Pass the book to the view for the review form
    }
    // ------------------------------------------------ REVIEWS -------------------------------------------------------
    public IActionResult Reviews(int bookId)
    {
        try
        {
            var book = _context.Books
                .FirstOrDefault(b => b.BookId == bookId);

            if (book == null)
            {
                return NotFound();
            }

            var reviews = _context.Reviews
                .Include(r => r.User)
                .Where(r => r.BookId == bookId)
                .Select(r => new Review
                {
                    ReviewId = r.ReviewId,
                    BookId = r.BookId,
                    UserId = r.UserId,
                    Content = r.Content ?? "", // Handle null content
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt,
                    User = r.User, // Include user information
                    Book = r.Book  // Include book information if needed
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            var viewModel = new BookReviewsViewModel
            {
                Book = book,
                Reviews = reviews
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine($"Error in Reviews action: {ex.Message}");
            return View("Error");
        }
    }


    [HttpPost]
    public async Task<IActionResult> SubmitReview(int bookId, string feedback, int ratingValue)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Check if the user has borrowed or purchased the book
        var hasAccess = _context.BorrowedBooks.Any(b => b.BookId == bookId && b.UserId == userId.Value) ||
                        _context.PurchasedBooks.Any(p => p.BookId == bookId && p.UserId == userId.Value);

        if (!hasAccess)
        {
            TempData["ErrorMessage"] = "You can only review books you have borrowed or purchased.";
            return RedirectToAction("Library");
        }

        // Create the review object
        var review = new Review
        {
            BookId = bookId,
            UserId = userId.Value,
            Content = feedback,
            Rating = ratingValue, // Save rating
            CreatedAt = DateTime.Now
        };

        // Add to the reviews
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thank you for your review!";
        return RedirectToAction("Library");
    }



    [HttpGet]
    public IActionResult Feedback()
    {
        return View(); // This assumes Feedback.cshtml is in Views/User/
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitFeedback([Range(1, 5)] int ratingValue, [StringLength(1000)] string feedback)
    {
        try
        {
            // Log the start of the method
            _logger.LogInformation("SubmitFeedback called with Rating: {RatingValue}, Feedback: {Feedback}", ratingValue, feedback);

            // Validate ModelState
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid. RatingValue: {RatingValue}, Feedback: {Feedback}", ratingValue, feedback);
                TempData["ErrorMessage"] = "Invalid input. Rating must be between 1-5 and feedback cannot exceed 1000 characters.";
                return RedirectToAction("Feedback", "User");
            }

            // Get the logged-in user's ID
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                _logger.LogWarning("User is not logged in.");
                TempData["ErrorMessage"] = "You need to log in to give feedback.";
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation("User ID retrieved: {UserId}", userId.Value);

            // Check if the user has submitted feedback recently
            var hasRecentFeedback = await _context.ServiceFeedbacks
                .AnyAsync(f => f.UserId == userId.Value && f.CreatedAt >= DateTime.UtcNow.AddHours(-24));

            if (hasRecentFeedback)
            {
                _logger.LogWarning("User {UserId} has already submitted feedback within the last 24 hours.", userId.Value);
                TempData["ErrorMessage"] = "You can only submit feedback once per day.";
                return RedirectToAction("Feedback", "User");
            }

            // Create a new feedback entity
            var serviceFeedback = new ServiceFeedback
            {
                UserId = userId.Value,
                RatingValue = ratingValue,
                Feedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Feedback object created: {Feedback}", serviceFeedback);

            // Add the feedback to the database and save changes
            await _context.ServiceFeedbacks.AddAsync(serviceFeedback);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                _logger.LogInformation("Feedback saved successfully for UserId: {UserId}", userId.Value);
                TempData["SuccessMessage"] = "Thank you for your feedback!";
            }
            else
            {
                _logger.LogWarning("Feedback was not saved to the database.");
                TempData["ErrorMessage"] = "Something went wrong. Please try again later.";
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update exception occurred while saving feedback.");
            TempData["ErrorMessage"] = "A database error occurred while submitting your feedback. Please try again later.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while saving feedback.");
            TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
        }

        return RedirectToAction("Index", "Home");
    }




    // ------------------------------ Deleting a purchased book -------------------------------

    [HttpPost]
    public IActionResult DeletePurchasedBook(int bookId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            TempData["ErrorMessage"] = "You need to log in to delete a book.";
            return RedirectToAction("Login", "Account");
        }

        var purchasedBook = _context.PurchasedBooks
            .FirstOrDefault(p => p.BookId == bookId && p.UserId == userId.Value);

        if (purchasedBook != null)
        {
            // Remove the book from the user's purchased books
            _context.PurchasedBooks.Remove(purchasedBook);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "The book has been successfully deleted from your library.";
        }
        else
        {
            TempData["ErrorMessage"] = "Book not found in your library.";
        }

        return RedirectToAction("PurchasedBooks", "User");
    }



}
