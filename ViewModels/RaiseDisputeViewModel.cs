using System.ComponentModel.DataAnnotations;

namespace PadiPay.ViewModels;

public class RaiseDisputeViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal OrderAmount { get; set; }
    public string GigTitle { get; set; } = string.Empty;
    public string CounterpartyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a reason for raising this dispute.")]
    [StringLength(200)]
    public string Reason { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please describe the problem and your attempted resolution.")]
    [StringLength(3000, MinimumLength = 20, ErrorMessage = "Explanation must be between 20 and 3000 characters.")]
    public string Explanation { get; set; } = string.Empty;
}
