using Microsoft.AspNetCore.Identity;

namespace PadiPay.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Headline { get; set; } = "Tech Freelancer";
    public string Bio { get; set; } = string.Empty;
    public string Skills { get; set; } = string.Empty;
    public string Location { get; set; } = "Lagos, Nigeria";
    public string? ProfilePictureUrl { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsSeller { get; set; } = true;

    // Navigation properties
    public virtual Wallet? Wallet { get; set; }
    public virtual ICollection<Gig> Gigs { get; set; } = new List<Gig>();
    public virtual ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();
    public virtual ICollection<Review> ReviewsGiven { get; set; } = new List<Review>();
}
