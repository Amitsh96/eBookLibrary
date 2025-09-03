using Microsoft.EntityFrameworkCore;
using eBookLibrary.Models;

public class LibraryContext : DbContext
{
    public LibraryContext(DbContextOptions<LibraryContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BorrowedBook>()
            .HasOne(b => b.Book)
            .WithMany()
            .HasForeignKey(b => b.BookId);

        modelBuilder.Entity<WaitingList>()
            .HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId);

        modelBuilder.Entity<PurchasedBook>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<ServiceFeedback>()
        .ToTable("ServiceFeedback");
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<BorrowedBook> BorrowedBooks { get; set; }
    public DbSet<PurchasedBook> PurchasedBooks { get; set; }
    public DbSet<WaitingList> WaitingList { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    public DbSet<Rating> Ratings { get; set; }
    public DbSet<ServiceFeedback> ServiceFeedbacks { get; set; }
    public DbSet<Review> Reviews { get; set; }
}
