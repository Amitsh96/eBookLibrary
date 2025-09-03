using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace eBookLibrary.Models
{
    public class Book
    {
        public int BookId { get; set; } // Primary key
        public string Title { get; set; }
        public string Author { get; set; }
        public string Publisher { get; set; }
        public decimal PricePurchase { get; set; }
        public string Format { get; set; }
        public int AvailableCopies { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public DateTime? DiscountEndDate { get; set; }
        public string Genre { get; set; }
        public int Year { get; set; }
        public bool IsBorrowable { get; set; }
        public bool IsBuyOnly { get; set; }
        public int CopiesBorrowed { get; set; }

        [NotMapped]
        public double AverageRating { get; set; }  // Calculated average rating, not stored in DB

        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();  // Ratings (Reviews)
        public ICollection<Review> Reviews { get; set; } = new List<Review>();  // Reviews from Users

        public string CoverImage { get; set; }
        public int AgeLimit { get; set; }
    }
}
