namespace ZdoRpgAi.Protocol.Messages;

// Server → Mod

public enum ServerToModMessageType {
    Say,
    SayMp3File,
    SpawnOnGroundInFrontOfCharacter,
    PlaySound3dOnCharacter,
    NpcStartFollowCharacter,
    NpcStopFollowCharacter,
    NpcAttack,
    NpcStopAttack,
    ShowMessageBox,
    GetCharactersWhoHear,
    SpeechRecognitionInProgress,
    SpeechRecognitionComplete,
    GetPlayerInfo,
    GetNpcInfo,
    RequestTextInput,
    TransferItem,
}

public enum ServerToClientMessageType {
    ServerSpeak,
    NpcSpeaksMp3,
}

// Server → Client
public record NpcSpeaksMp3Payload(string NpcId, string Text, double DurationSec);

// Server → Mod
public record SayPayload(string NpcId, string Text, string Language);
public record SpawnOnGroundInFrontOfCharacterPayload(string NpcId, string ItemId, int Count = 1);
public record PlaySound3dOnCharacterPayload(string NpcId, string Sound);
public record NpcStartFollowCharacterPayload(string NpcId, string TargetCharacterId);
public record NpcStopFollowCharacterPayload(string NpcId);
public record NpcAttackPayload(string NpcId, string TargetCharacterId);
public record NpcStopAttackPayload(string NpcId);
public record ShowMessageBoxPayload(string Message);
public record GetCharactersWhoHearRequestPayload(string CharacterId, float MaxDistanceMeters = 100f);
public record SpeechRecognitionInProgressPayload(string PlayerId, string Text);
public record SpeechRecognitionCompletePayload(string PlayerId, string Text);
public record TransferItemPayload(string FromCharacterId, string ToCharacterId, string ItemId, int Count = 1, bool IsServicePayment = false);

public record GetPlayerInfoRequestPayload(string PlayerId);
public record GetPlayerInfoResponsePayload(
    string ObjectId,
    string Name,
    string Race,
    string Sex,
    string ClassName = "",
    int Level = 0,
    int HealthCurrent = 0,
    int HealthMax = 0,
    int MagickaCurrent = 0,
    int MagickaMax = 0,
    int FatigueCurrent = 0,
    int FatigueMax = 0,
    bool IsDead = false);
public record GetNpcInfoRequestPayload(string NpcId);
public record GetNpcInfoResponsePayload(string NpcId, string Name, string Race, string Sex, string? Class = null, string? ClassName = null, string? Faction = null, string? FactionRank = null, int? Level = null);
