using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace eBookLibrary.Services
{
    public class UpdateWaitingListService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public UpdateWaitingListService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() => UpdateWaitingListAsync(), cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task UpdateWaitingListAsync()
        {
            while (true)
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<LibraryContext>();

                var waitingLists = context.WaitingList.ToList();
                foreach (var entry in waitingLists)
                {
                    var borrowedBooks = context.BorrowedBooks
                        .Where(b => b.BookId == entry.BookId)
                        .OrderBy(b => b.ReturnDate)
                        .ToList();

                    // Update the EstimatedDays based on the queue length
                    entry.EstimatedDays = entry.Position * 30;
                }

                context.SaveChanges(); // Save all updates

                // Wait for the next daily update
                await Task.Delay(TimeSpan.FromHours(24));
            }
        }
    }
}