using System.Text.Json.Serialization;

namespace EWallet.Modules.Notifications.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationType
{
    TransferReceived,
    TransactionCompleted,
    TransactionFailed
}
