using System.ComponentModel.DataAnnotations;
using PadiPay.Models;

namespace PadiPay.ViewModels;

public class AdminUserItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
    public int AccessFailedCount { get; set; }
    public string? SecurityStamp { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsSeller { get; set; }
    public decimal WalletBalance { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public bool IsAdmin => Roles.Contains("Admin");
    public string? AdminNotes { get; set; }
    public int GigCount { get; set; }
    public int OrdersAsBuyerCount { get; set; }
    public int OrdersAsSellerCount { get; set; }
    public int DisputesInvolvedCount { get; set; }
}

public class WalletAdjustmentRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string AdjustmentType { get; set; } = "Credit"; // "Credit" or "Debit"

    [Required]
    [Range(1, 10000000, ErrorMessage = "Amount must be between ₦1 and ₦10,000,000")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(250, ErrorMessage = "Reason cannot exceed 250 characters")]
    public string Reason { get; set; } = string.Empty;
}

public class SaveAdminNotesRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public string? AdminNotes { get; set; }
}

public class ResetUserPasswordRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, ErrorMessage = "The password must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

