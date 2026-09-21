using PadiPay.Models;

namespace PadiPay.ViewModels;

public class UserProfileViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public List<Gig> ActiveGigs { get; set; } = new();
    public List<Review> ReviewsReceived { get; set; } = new();
    public bool IsOwner { get; set; }

    public double AverageRating =>
        ReviewsReceived.Any() ? Math.Round(ReviewsReceived.Average(r => r.Rating), 1) : 5.0;

    public int TotalReviewsCount => ReviewsReceived.Count;

    public IEnumerable<string> ParsedSkills =>
        string.IsNullOrWhiteSpace(User?.Skills)
            ? Enumerable.Empty<string>()
            : User.Skills.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
