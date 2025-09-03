using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eBookLibrary.Models;

public class WaitingList
{
    [Key] // הגדרת מפתח ראשי
    public int WaitId { get; set; }

    [ForeignKey("User")]
    public int UserId { get; set; }

    [ForeignKey("Book")]
    public int BookId { get; set; }

    public int Position { get; set; }

    public bool IsNotified { get; set; }

    public int EstimatedDays { get; set; }

    // ניווט לטבלאות User ו-Book
    public User User { get; set; }
    public Book Book { get; set; }
}
