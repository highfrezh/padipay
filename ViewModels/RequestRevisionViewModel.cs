using System.ComponentModel.DataAnnotations;

namespace PadiPay.ViewModels;

public class RequestRevisionViewModel
{
    [Required]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Please detail the specific changes or fixes you need.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Revision feedback must be between 10 and 2000 characters.")]
    [Display(Name = "Revision Feedback & Requested Changes")]
    public string RevisionNotes { get; set; } = string.Empty;
}
