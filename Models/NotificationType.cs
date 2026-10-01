namespace PadiPay.Models;

public enum NotificationType
{
    System = 0,
    OrderPlaced = 1,
    WorkDelivered = 2,
    RevisionRequested = 3,
    OrderCompleted = 4,
    DisputeRaised = 5,
    DisputeResolved = 6,
    MessageReceived = 7,
    WalletFunded = 8
}
