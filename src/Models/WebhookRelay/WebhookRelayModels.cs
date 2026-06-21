using System.Text.Json;

namespace Game_Daily_Routine_Launcher;

public sealed record WebhookRelayRequest(string Url, JsonElement Body);

public sealed record WebhookRelayResult(bool Success, string Message, string? ForwardedBody = null);
