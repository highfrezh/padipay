using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PadiPay.ViewModels;

public class EditProfileViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Professional headline is required.")]
    [StringLength(120, ErrorMessage = "Headline cannot exceed 120 characters.")]
    [Display(Name = "Professional Headline")]
    public string Headline { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters.")]
    [Display(Name = "About / Bio")]
    public string Bio { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "Skills list cannot exceed 300 characters.")]
    [Display(Name = "Skills & Tech Stack")]
    public string Skills { get; set; } = string.Empty;

    [Required(ErrorMessage = "Location is required.")]
    [StringLength(100, ErrorMessage = "Location cannot exceed 100 characters.")]
    [Display(Name = "Location in Nigeria")]
    public string Location { get; set; } = string.Empty;

    [Display(Name = "Profile Avatar Photo")]
    public IFormFile? ProfilePicture { get; set; }

    public string? CurrentProfilePictureUrl { get; set; }
}
