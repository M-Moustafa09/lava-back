using System.Text.Json.Serialization;

namespace lavva_backend_clean.Application.Features.Notifications.Queries.GetMyNotifications;

public record GetMyNotificationsQuery(Guid UserId) : IRequest<Result<List<GetMyNotificationsResponse>>>;

public record GetMyNotificationsResponse(
    Guid Id,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] string Type,
    string Message,
    Guid? RelatedEntityId,
    bool IsRead,
    DateTime CreatedAt
);
