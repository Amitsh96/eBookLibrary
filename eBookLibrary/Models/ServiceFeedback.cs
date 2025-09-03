using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eBookLibrary.Models
{
    public class ServiceFeedback
    {
        [Key]
        public int Id { get; set; } // Primary Key

        [Required]
        public int UserId { get; set; } // Foreign Key to User

        [Required]
        [Range(1, 5)]
        public int RatingValue { get; set; } // Rating (1 to 5)

        public string Feedback { get; set; } // Optional feedback

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now; // Timestamp for feedback

        // Navigation Properties
        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
