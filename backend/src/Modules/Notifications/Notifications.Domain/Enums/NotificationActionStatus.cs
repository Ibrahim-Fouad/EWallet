using System.Text.Json.Serialization;

namespace EWallet.Modules.Notifications.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationActionStatus
{
    Pending,
    Approved,
    Rejected,
    Expired,
    Completed,
    Failed
}
