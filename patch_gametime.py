#!/usr/bin/env python3
from pathlib import Path

BASE = Path("/home/bazzitejimjam")
MOD_LUA = BASE / ".config/AmethystModManager/Profiles/Morrowind (OpenMW)/mods/zdo-rpg-ai-openmw-mod-main/scripts/zdorpgai/global.lua"
PROTO_MOD = BASE / "zdo-rpg-ai/src/ZdoRpgAi.Protocol/Messages/Mod.cs"
PROTO_CTX = BASE / "zdo-rpg-ai/src/ZdoRpgAi.Protocol/Messages/PayloadJsonContext.cs"
CLIENT_APP = BASE / "zdo-rpg-ai/src/ZdoRpgAi.Client/App/ClientApplication.cs"

# ─── 1. Lua ───────────────────────────────────────────────────────────────────
lua = MOD_LUA.read_text()

if "openmw_aux.calendar" not in lua:
    lua = lua.replace(
        "local storage = require('openmw.storage')",
        "local storage = require('openmw.storage')\nlocal calendar = require('openmw_aux.calendar')"
    )

game_time_lua = '''-------------------------------------------------------------------------------
-- Game time tracking
-------------------------------------------------------------------------------

local lastGameTime = nil

local function checkGameTime()
    if not playerObject then return end
    local ok, gt = pcall(calendar.formatGameTime, "%H:%M, %d %B 3E %Y")
    if not ok then return end
    if gt ~= lastGameTime then
        lastGameTime = gt
        publish('GameTimeUpdate', { gameTime = gt })
    end
end
'''
if "checkGameTime" not in lua:
    lua = lua.replace(
        "-------------------------------------------------------------------------------\n-- Cell change detection",
        game_time_lua + "-------------------------------------------------------------------------------\n-- Cell change detection"
    )

lua = lua.replace(
    "    checkCellChange()\nend\n\nlocal function onPlayerAdded",
    "    checkCellChange()\n    checkGameTime()\nend\n\nlocal function onPlayerAdded"
)

MOD_LUA.write_text(lua)
print("[OK] Lua global.lua patched")

# ─── 2. Protocol Mod.cs ───────────────────────────────────────────────────────
mod_cs = PROTO_MOD.read_text()

if "GameTimeUpdate" not in mod_cs:
    mod_cs = mod_cs.replace(
        "    RequestTextInput,\n}",
        "    RequestTextInput,\n    GameTimeUpdate,\n}"
    )
    mod_cs = mod_cs.replace(
        "public record GetCharactersWhoHearResponsePayload",
        "public record GameTimeUpdatePayload(string GameTime);\n\npublic record GetCharactersWhoHearResponsePayload"
    )

PROTO_MOD.write_text(mod_cs)
print("[OK] Protocol Mod.cs patched")

# ─── 3. Protocol PayloadJsonContext.cs ────────────────────────────────────────
ctx = PROTO_CTX.read_text()

if "GameTimeUpdatePayload" not in ctx:
    ctx = ctx.replace(
        "[JsonSerializable(typeof(RequestTextInputPayload))]",
        "[JsonSerializable(typeof(RequestTextInputPayload))]\n[JsonSerializable(typeof(GameTimeUpdatePayload))]"
    )

PROTO_CTX.write_text(ctx)
print("[OK] Protocol PayloadJsonContext.cs patched")

# ─── 4. C# ClientApplication.cs ───────────────────────────────────────────────
app = CLIENT_APP.read_text()

if "_lastGameTime" not in app:
    app = app.replace(
        "    private string? _lastTargetNpcId;",
        "    private string? _lastTargetNpcId;\n    private string? _lastGameTime;"
    )

if "GameTimeUpdate" not in app:
    app = app.replace(
        '''            case nameof(ModToServerMessageType.CellChange): {
                    var e = msg.Json?.DeserializeSafe(PayloadJsonContext.Default.CellChangePayload);
                    if (e != null) {
                        Log.Info("Cell: {CellName}", e.CellName);
                    }

                    break;
                }''',
        '''            case nameof(ModToServerMessageType.CellChange): {
                    var e = msg.Json?.DeserializeSafe(PayloadJsonContext.Default.CellChangePayload);
                    if (e != null) {
                        Log.Info("Cell: {CellName}", e.CellName);
                    }

                    break;
                }
            case nameof(ModToServerMessageType.GameTimeUpdate): {
                    var e = msg.Json?.DeserializeSafe(PayloadJsonContext.Default.GameTimeUpdatePayload);
                    if (e != null) {
                        _lastGameTime = e.GameTime;
                        Log.Debug("Game time: {GameTime}", _lastGameTime);
                    }

                    break;
                }'''
    )

app = app.replace(
    '''        var payload = new PlayerStartSpeakPayload(
            _localPlayerId ?? "player",
            _lastTargetNpcId,
            "0"
        );''',
    '''        var payload = new PlayerStartSpeakPayload(
            _localPlayerId ?? "player",
            _lastTargetNpcId,
            _lastGameTime ?? "0"
        );'''
)

app = app.replace(
    '''                var payload = new PlayerSpeaksTextPayload(
                    _localPlayerId ?? "player",
                    line.Trim(),
                    _lastTargetNpcId,
                    DateTime.UtcNow.ToString("O"));''',
    '''                var payload = new PlayerSpeaksTextPayload(
                    _localPlayerId ?? "player",
                    line.Trim(),
                    _lastTargetNpcId,
                    _lastGameTime ?? DateTime.UtcNow.ToString("O"));'''
)

app = app.replace(
    '''            var payload = new PlayerSpeaksTextPayload(
                playerId,
                text,
                npcId,
                DateTime.UtcNow.ToString("O"));''',
    '''            var payload = new PlayerSpeaksTextPayload(
                playerId,
                text,
                npcId,
                _lastGameTime ?? DateTime.UtcNow.ToString("O"));'''
)

CLIENT_APP.write_text(app)
print("[OK] Client Application.cs patched")
print("\nDone. Run: dotnet build")
