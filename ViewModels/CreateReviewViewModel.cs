using System.ComponentModel.DataAnnotations;

namespace PadiPay.ViewModels;

public class CreateReviewViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int GigId { get; set; }
    public string GigTitle { get; set; } = string.Empty;
    public string? GigCoverImageUrl { get; set; }

    public string SellerId { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string? SellerHeadline { get; set; }
    public string? SellerPictureUrl { get; set; }

    [Required(ErrorMessage = "Please select a star rating.")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "Please share details of your experience with this freelancer.")]
    [StringLength(1500, MinimumLength = 5, ErrorMessage = "Review must be between 5 and 1500 characters.")]
    public string Comment { get; set; } = string.Empty;
}
