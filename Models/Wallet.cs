namespace PadiPay.Models;

public class Wallet
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}


