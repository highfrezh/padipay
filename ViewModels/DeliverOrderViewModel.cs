using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PadiPay.ViewModels;

public class DeliverOrderViewModel
{
    [Required]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Please describe the work you are delivering.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Delivery message must be between 10 and 2000 characters.")]
    [Display(Name = "Delivery Message & Instructions")]
    public string Message { get; set; } = string.Empty;

    [Url(ErrorMessage = "Please enter a valid URL (e.g. https://my-app.azurewebsites.net)")]
    [StringLength(300)]
    [Display(Name = "Live Preview URL (Optional)")]
    public string? LivePreviewUrl { get; set; }

    [Url(ErrorMessage = "Please enter a valid URL (e.g. https://github.com/username/project)")]
    [StringLength(300)]
    [Display(Name = "GitHub / Repository URL (Optional)")]
    public string? GithubRepoUrl { get; set; }

    [Display(Name = "Attachment / Deliverable File (.zip, .pdf, .png, .jpg - max 20MB)")]
    public IFormFile? DeliverableFile { get; set; }
}
