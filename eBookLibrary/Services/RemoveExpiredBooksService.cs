using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace eBookLibrary.Services
{
    public class RemoveExpiredBooksService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public RemoveExpiredBooksService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<LibraryContext>();

                    var expiredBooks = context.BorrowedBooks
                        .Where(b => b.ReturnDate < DateTime.Now) // Ensure this references the correct column
                        .ToList();

                    foreach (var book in expiredBooks)
                    {
                        var originalBook = context.Books.FirstOrDefault(b => b.BookId == book.BookId);
                        if (originalBook != null)
                        {
                            originalBook.AvailableCopies += 1; // Restore the available copy
                        }

                        context.BorrowedBooks.Remove(book);
                    }

                    await context.SaveChangesAsync();
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Run daily
            }
        }
    }
}
