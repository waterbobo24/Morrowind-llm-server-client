using System.Text.Json.Serialization;

namespace ZdoRpgAi.Protocol.Messages;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
// Server → Client
[JsonSerializable(typeof(NpcSpeaksMp3Payload))]
// Server → Mod
[JsonSerializable(typeof(SpeechRecognitionInProgressPayload))]
[JsonSerializable(typeof(SpeechRecognitionCompletePayload))]
[JsonSerializable(typeof(GetCharactersWhoHearRequestPayload))]
[JsonSerializable(typeof(GetPlayerInfoRequestPayload))]
[JsonSerializable(typeof(GetNpcInfoRequestPayload))]
[JsonSerializable(typeof(GetPlayerInfoResponsePayload))]
[JsonSerializable(typeof(GetNpcInfoResponsePayload))]
[JsonSerializable(typeof(SpawnOnGroundInFrontOfCharacterPayload))]
[JsonSerializable(typeof(PlaySound3dOnCharacterPayload))]
[JsonSerializable(typeof(NpcStartFollowCharacterPayload))]
[JsonSerializable(typeof(NpcStopFollowCharacterPayload))]
[JsonSerializable(typeof(NpcAttackPayload))]
[JsonSerializable(typeof(NpcStopAttackPayload))]
[JsonSerializable(typeof(ShowMessageBoxPayload))]
// Mod → Client
[JsonSerializable(typeof(StartSessionAckPayload))]
// Client → Mod
[JsonSerializable(typeof(SayMp3FilePayload))]
// Client → Both
[JsonSerializable(typeof(PlayerStartSpeakPayload))]
[JsonSerializable(typeof(PlayerStopSpeakPayload))]
// Client → Server
[JsonSerializable(typeof(PlayerSpeaksTextPayload))]
[JsonSerializable(typeof(PlayerSpeaksAudioPayload))]
// Mod → Server
[JsonSerializable(typeof(PlayerAddedPayload))]
[JsonSerializable(typeof(PlayerStateChangedPayload))]
[JsonSerializable(typeof(TargetChangedPayload))]
[JsonSerializable(typeof(CellChangePayload))]
[JsonSerializable(typeof(GameSaveLoadPayload))]
[JsonSerializable(typeof(NearbyCharacterInfo))]
[JsonSerializable(typeof(GetCharactersWhoHearResponsePayload))]
// Mod → Server (request text input)
[JsonSerializable(typeof(RequestTextInputPayload))]
[JsonSerializable(typeof(GameTimeUpdatePayload))]
public partial class PayloadJsonContext : JsonSerializerContext;
