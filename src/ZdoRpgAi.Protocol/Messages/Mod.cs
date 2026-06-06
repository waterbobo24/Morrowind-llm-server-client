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
}

public record PlayerAddedPayload(
    string PlayerId,
    string Name = "",
    string Race = "",
    string Sex = "",
    string ClassName = "",
    int Level = 0,
    int HealthCurrent = 0,
    int HealthMax = 0,
    int MagickaCurrent = 0,
    int MagickaMax = 0,
    int FatigueCurrent = 0,
    int FatigueMax = 0,
    string? CellName = null);

public record PlayerStateChangedPayload(
    string PlayerId,
    int HealthCurrent,
    int HealthMax,
    int MagickaCurrent,
    int MagickaMax,
    int FatigueCurrent,
    int FatigueMax,
    string? CellName,
    bool IsDead);

public record TargetChangedPayload(string PlayerId, string? NpcId);
public record CellChangePayload(string PlayerId, string CellName);
public record GameSaveLoadPayload();
public record NearbyCharacterInfo(string CharacterId, float DistanceMeters);
public record GetCharactersWhoHearResponsePayload(NearbyCharacterInfo[] Characters);
public record RequestTextInputPayload(string PlayerId, string? NpcId);
