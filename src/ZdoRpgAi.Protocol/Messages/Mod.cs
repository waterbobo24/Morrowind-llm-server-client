using System.Text.Json.Serialization;
namespace ZdoRpgAi.Protocol.Messages;

// Mod → Server

public enum ModToServerMessageType {
    PlayerAdded,
    PlayerStateChanged,
    TargetChanged,
    CellChange,
    GameSaveLoad,
    GetCharactersWhoHearResponse,
    RequestTextInput,
    GameTimeUpdate,
}

public record PlayerAddedPayload(
    [property: JsonPropertyName("playerId")] string PlayerId,
    [property: JsonPropertyName("name")] string Name = "",
    [property: JsonPropertyName("race")] string Race = "",
    [property: JsonPropertyName("sex")] string Sex = "",
    [property: JsonPropertyName("className")] string ClassName = "",
    [property: JsonPropertyName("level")] int Level = 0,
    [property: JsonPropertyName("healthCurrent")] float HealthCurrent = 0,
    [property: JsonPropertyName("healthMax")] float HealthMax = 0,
    [property: JsonPropertyName("magickaCurrent")] float MagickaCurrent = 0,
    [property: JsonPropertyName("magickaMax")] float MagickaMax = 0,
    [property: JsonPropertyName("fatigueCurrent")] float FatigueCurrent = 0,
    [property: JsonPropertyName("fatigueMax")] float FatigueMax = 0,
    [property: JsonPropertyName("cellName")] string? CellName = null);

public record PlayerStateChangedPayload(
    [property: JsonPropertyName("playerId")] string PlayerId,
    [property: JsonPropertyName("healthCurrent")] float HealthCurrent,
    [property: JsonPropertyName("healthMax")] float HealthMax,
    [property: JsonPropertyName("magickaCurrent")] float MagickaCurrent,
    [property: JsonPropertyName("magickaMax")] float MagickaMax,
    [property: JsonPropertyName("fatigueCurrent")] float FatigueCurrent,
    [property: JsonPropertyName("fatigueMax")] float FatigueMax,
    [property: JsonPropertyName("cellName")] string? CellName,
    [property: JsonPropertyName("isDead")] bool IsDead);

public record TargetChangedPayload(string PlayerId, string? NpcId);
public record CellChangePayload(string PlayerId, string CellName);
public record GameSaveLoadPayload(
    [property: JsonPropertyName("saveId")] string? SaveId = null);
public record NearbyCharacterInfo(string CharacterId, float DistanceMeters);
public record GameTimeUpdatePayload(string GameTime);

public record GetCharactersWhoHearResponsePayload(NearbyCharacterInfo[] Characters);
public record RequestTextInputPayload(string PlayerId, string? NpcId);
