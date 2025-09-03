using eBookLibrary.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace eBookLibrary.Models
{

    public class BorrowedBook
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }  // Represents the user who borrowed the book

        [Required]
        public int BookId { get; set; }

        [ForeignKey("BookId")]
        public virtual Book Book { get; set; }

        [Required]
        public DateTime ReturnDate { get; set; }

        [Required]
        public DateTime BorrowDate { get; set; }

        public bool IsReminderSent { get; set; }

    }
}
