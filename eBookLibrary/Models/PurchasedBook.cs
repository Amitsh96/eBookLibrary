using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eBookLibrary.Models
{
    public class PurchasedBook
    {
        [Key]
        public int Id { get; set; }  // Primary key

        [Required]
        public int UserId { get; set; } // User ID

        [Required]
        public int BookId { get; set; } // Book ID

        [ForeignKey("BookId")]
        public Book Book { get; set; } // Navigation property to the Book

        public string Title { get; set; }
        public string Author { get; set; }
        public DateTime PurchaseDate { get; set; } // Date of purchase
    }
}
