using System.Collections.Concurrent;
using ZdoRpgAi.Core;
using ZdoRpgAi.Protocol.Channel;
using ZdoRpgAi.Protocol.Messages;
using ZdoRpgAi.Protocol.Rpc;

namespace ZdoRpgAi.Server.Game;

public class PlayerStateTracker {
    private static readonly ILog Log = Logger.Get<PlayerStateTracker>();
    private readonly IRpcChannel _rpc;
    private readonly ConcurrentDictionary<string, PlayerState> _players = new();

    public PlayerStateTracker(IRpcChannel rpc) {
        _rpc = rpc;
        rpc.MessageReceived += OnMessageReceived;
    }

    private void OnMessageReceived(Message msg) {
        switch (msg.Type) {
            case nameof(ModToServerMessageType.PlayerAdded):
                HandlePlayerAdded(msg);
                break;
            case nameof(ModToServerMessageType.PlayerStateChanged):
                HandlePlayerStateChanged(msg);
                break;
            case nameof(ModToServerMessageType.CellChange):
                HandleCellChange(msg);
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
    int HealthCurrent,
    int HealthMax,
    int MagickaCurrent,
    int MagickaMax,
    int FatigueCurrent,
    int FatigueMax,
    string? CellName,
    bool IsDead);
