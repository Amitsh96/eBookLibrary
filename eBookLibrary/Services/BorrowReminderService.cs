using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using eBookLibrary.Models;

namespace eBookLibrary.Services
{
    public class BorrowReminderService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public BorrowReminderService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() => CheckBorrowedBooksAsync(), cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task CheckBorrowedBooksAsync()
        {
            while (true)
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<LibraryContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                var booksToNotify = context.BorrowedBooks
                    .Where(b => b.ReturnDate <= DateTime.Now.AddDays(5) && !b.IsReminderSent)
                    .ToList();

                foreach (var borrowedBook in booksToNotify)
                {
                    var user = context.Users.FirstOrDefault(u => u.UserId == borrowedBook.UserId);
                    if (user != null)
                    {
                        // Send email notification
                        await emailService.SendEmailAsync(
                            user.Email,
                            "Borrowing Reminder",
                            $"Reminder: The book '{borrowedBook.Book.Title}' is due on {borrowedBook.ReturnDate:yyyy-MM-dd}."
                        );

                        // Update reminder status
                        borrowedBook.IsReminderSent = true;
                        context.SaveChanges();
                    }
                }

                // Sleep for 24 hours
                await Task.Delay(TimeSpan.FromHours(24));
            }
        }
    }
}
