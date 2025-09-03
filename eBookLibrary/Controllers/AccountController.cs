using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using eBookLibrary.Models;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

public class AccountController : Controller
{
    private readonly LibraryContext _context;

    public AccountController(LibraryContext context)
    {
        _context = context;
    }

    // Register Page (GET request)
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // Handle Register Request (POST request)
    [HttpPost]
    public async Task<IActionResult> Register(string email, string password, string role)
    {
        // Check if email or password is empty
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ViewBag.ErrorMessage = "Email or Password cannot be empty.";
            return View();
        }

        // Check if user already exists in the database
        var existingUser = await _context.Users
                                         .FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
        {
            ViewBag.ErrorMessage = "Email is already registered.";
            return View();
        }

        // Hash the password using PasswordHasher
        var passwordHasher = new PasswordHasher<User>();
        var hashedPassword = passwordHasher.HashPassword(null, password);

        // Create new user
        var newUser = new User
        {
            Email = email,
            Password = hashedPassword,
            Role = role // Set role (Admin/User)
        };

        // Add user to the database
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // Redirect to Login page after successful registration
        return RedirectToAction("Login");
    }

    // Login Page (GET request)
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // Handle Login Request (POST request)
    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        // Check if email or password is empty
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ViewBag.ErrorMessage = "Email or Password cannot be empty.";
            return View();
        }

        // Check if user exists in the database asynchronously
        var user = await _context.Users
                                 .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            ViewBag.ErrorMessage = "Invalid email or password.";
            return View();
        }

        // Use PasswordHasher to verify the password
        var passwordHasher = new PasswordHasher<User>();
        var result = passwordHasher.VerifyHashedPassword(user, user.Password, password);

        if (result == PasswordVerificationResult.Failed)
        {
            ViewBag.ErrorMessage = "Invalid email or password.";
            return View();
        }

        // Create claims for the logged-in user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // Store userId as string in Claims
            new Claim(ClaimTypes.Name, user.Email), // Store email in Claims
            new Claim(ClaimTypes.Role, user.Role) // Store role (Admin/User)
        };

        // Create ClaimsIdentity
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // Create a ClaimsPrincipal
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Sign-in the user with the ClaimsPrincipal
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

        // Store session data
        HttpContext.Session.SetString("Username", user.Email);
        HttpContext.Session.SetString("Role", user.Role);
        HttpContext.Session.SetInt32("UserId", user.UserId);

        // Redirect based on role
        if (user.Role == "Admin")
        {
            return RedirectToAction("Dashboard", "Admin");
        }
        else if (user.Role == "User")
        {
            return RedirectToAction("ExploreBooks", "User"); // Redirect to Explore after login
        }

        // Default redirect in case of unexpected roles
        return RedirectToAction("Index", "Home");
    }

    // Logout
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Sign out the user
        HttpContext.Session.Clear(); // Clear session
        return RedirectToAction("Index", "Home");
    }
}
