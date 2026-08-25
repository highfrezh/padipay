using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PadiPay.ViewModels;

public class EditGigViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please give your gig a catchy title.")]
    [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.", MinimumLength = 10)]
    [Display(Name = "Gig Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Please describe what you will deliver in detail.")]
    [MinLength(50, ErrorMessage = "Description must be at least 50 characters to give buyers good context.")]
    [Display(Name = "Detailed Service Description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(1000, 10000000, ErrorMessage = "Price must be between ₦1,000 and ₦10,000,000.")]
    [Display(Name = "Price (₦)")]
    public decimal Price { get; set; }

    [Required]
    [Range(1, 90, ErrorMessage = "Turnaround time must be between 1 and 90 days.")]
    [Display(Name = "Turnaround Time (Days)")]
    public int DeliveryDays { get; set; }

    [Display(Name = "Revisions Included")]
    [Range(0, 10, ErrorMessage = "Revisions must be between 0 and 10.")]
    public int RevisionCount { get; set; }

    [Required(ErrorMessage = "Tell buyers what information, repos, or designs you need to start.")]
    [Display(Name = "Buyer Requirements")]
    public string Requirements { get; set; } = string.Empty;

    [Display(Name = "Tags (comma-separated)")]
    public string Tags { get; set; } = string.Empty;

    public string? ExistingCoverImageUrl { get; set; }

    [Display(Name = "Change Cover Image (optional)")]
    public IFormFile? CoverImage { get; set; }

    public bool IsActive { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
}
