using System.Collections.Concurrent;
using ZdoRpgAi.Core;
using ZdoRpgAi.Repository;
using ZdoRpgAi.Protocol.Channel;
using ZdoRpgAi.Protocol.Messages;
using ZdoRpgAi.Protocol.Rpc;

namespace ZdoRpgAi.Server.Game;

public class PlayerStateTracker {
    private static readonly ILog Log = Logger.Get<PlayerStateTracker>();
    private readonly IRpcChannel _rpc;
    private readonly ISaveGameRepository _saveGameRepo;
    private readonly ConcurrentDictionary<string, PlayerState> _players = new();
    private readonly ConcurrentDictionary<string, List<EquippedItem>> _npcEquipment = new();

    public PlayerStateTracker(IRpcChannel rpc, ISaveGameRepository saveGameRepo) {
        _rpc = rpc;
        _saveGameRepo = saveGameRepo;
        rpc.MessageReceived += OnMessageReceived;
    }

    private void OnMessageReceived(Message msg) {
        switch (msg.Type) {
            case nameof(ModToServerMessageType.GameSaveLoad):
                HandleGameSaveLoad(msg);
                break;
            case nameof(ModToServerMessageType.PlayerAdded):
                HandlePlayerAdded(msg);
                break;
            case nameof(ModToServerMessageType.PlayerStateChanged):
                HandlePlayerStateChanged(msg);
                break;
            case nameof(ModToServerMessageType.CellChange):
                HandleCellChange(msg);
                break;
            case "NpcEquipment":
                HandleNpcEquipment(msg);
                break;
            case "PlayerEquipment":
                HandlePlayerEquipment(msg);
                break;
        }
    }

    private void HandlePlayerAdded(Message msg) {
        var payload = msg.Json?.DeserializeSafe(PayloadJsonContext.Default.PlayerAddedPayload);
        if (payload == null) return;
        Log.Info("Player added: {Name} ({Race} {Sex}, level {Level})", payload.Name, payload.Race, payload.Sex, payload.Level);
        _players[payload.PlayerId] = new PlayerState(
            payload.PlayerId, payload.Name, payload.Race, payload.Sex,
            payload.ClassName, payload.Level,
            payload.HealthCurrent, payload.HealthMax,
            payload.MagickaCurrent, payload.MagickaMax,
            payload.FatigueCurrent, payload.FatigueMax,
            payload.CellName, false);
    }

    private void HandlePlayerStateChanged(Message msg) {
        var payload = msg.Json?.DeserializeSafe(PayloadJsonContext.Default.PlayerStateChangedPayload);
        if (payload == null) return;
        if (_players.TryGetValue(payload.PlayerId, out var existing)) {
            _players[payload.PlayerId] = existing with {
                HealthCurrent = payload.HealthCurrent,
                HealthMax = payload.HealthMax,
                MagickaCurrent = payload.MagickaCurrent,
                MagickaMax = payload.MagickaMax,
                FatigueCurrent = payload.FatigueCurrent,
                FatigueMax = payload.FatigueMax,
                CellName = payload.CellName,
                IsDead = payload.IsDead,
            };
            Log.Trace("Player {PlayerId} state updated: HP {HP}/{HPMax}, Mag {Mag}/{MagMax}, Fat {Fat}/{FatMax}, Dead={Dead}",
                payload.PlayerId, payload.HealthCurrent, payload.HealthMax,
                payload.MagickaCurrent, payload.MagickaMax,
                payload.FatigueCurrent, payload.FatigueMax,
                payload.IsDead);
        } else {
            _players[payload.PlayerId] = new PlayerState(
                payload.PlayerId, "", "", "", "", 0,
                payload.HealthCurrent, payload.HealthMax,
                payload.MagickaCurrent, payload.MagickaMax,
                payload.FatigueCurrent, payload.FatigueMax,
                payload.CellName, payload.IsDead);
        }
    }

    private void HandleCellChange(Message msg) {
        var payload = msg.Json?.DeserializeSafe(PayloadJsonContext.Default.CellChangePayload);
        if (payload == null) return;
        if (_players.TryGetValue(payload.PlayerId, out var existing)) {
            _players[payload.PlayerId] = existing with { CellName = payload.CellName };
            Log.Info("Player {PlayerId} moved to {CellName}", payload.PlayerId, payload.CellName);
        }
    }

    private void HandleGameSaveLoad(Message msg) {
        var payload = msg.Json?.DeserializeSafe(PayloadJsonContext.Default.GameSaveLoadPayload);
        if (payload?.SaveId is not null) {
            _saveGameRepo.SetSaveContext(payload.SaveId);
            _players.Clear();
            Log.Info("Switched to save context: {SaveId}, cleared {Count} cached player(s)", payload.SaveId, _players.Count);
        }
    }

    private void HandleNpcEquipment(Message msg) {
        var payload = msg.Json?.DeserializeSafe(PayloadJsonContext.Default.EquipmentMessage);
        if (payload?.Items != null) {
            _npcEquipment[payload.Id] = payload.Items;
            Log.Info("NPC {NpcId} equipment updated: {Count} items", payload.Id, payload.Items.Count);
        }
    }

    private void HandlePlayerEquipment(Message msg) {
        var payload = msg.Json?.DeserializeSafe(PayloadJsonContext.Default.EquipmentMessage);
        if (payload?.Items != null && _players.TryGetValue(payload.Id, out var existing)) {
            _players[payload.Id] = existing with { Equipment = payload.Items };
            Log.Info("Player {PlayerId} equipment updated: {Count} items", payload.Id, payload.Items.Count);
        }
    }

    public List<EquippedItem> GetNpcEquipment(string npcId) =>
        _npcEquipment.GetValueOrDefault(npcId) ?? new List<EquippedItem>();

    public PlayerState? GetPlayerState(string playerId) => _players.GetValueOrDefault(playerId);

    public List<string> ListPlayerIds() => _players.Keys.ToList();
}

public record PlayerState(
    string PlayerId,
    string Name,
    string Race,
    string Sex,
    string ClassName,
    int Level,
    float HealthCurrent,
    float HealthMax,
    float MagickaCurrent,
    float MagickaMax,
    float FatigueCurrent,
    float FatigueMax,
    string? CellName,
    bool IsDead,
    List<EquippedItem>? Equipment = null);
