using PadiPay.Models;

namespace PadiPay.ViewModels;

public class UserProfileViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public List<Gig> ActiveGigs { get; set; } = new();
    public bool IsOwner { get; set; }

    public IEnumerable<string> ParsedSkills =>
        string.IsNullOrWhiteSpace(User?.Skills)
            ? Enumerable.Empty<string>()
            : User.Skills.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
