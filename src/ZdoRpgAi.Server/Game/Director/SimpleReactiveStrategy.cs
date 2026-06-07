using ZdoRpgAi.Core;
using ZdoRpgAi.Protocol.Messages;
using ZdoRpgAi.Protocol.Rpc;
using ZdoRpgAi.Server.Game.Npc;
using ZdoRpgAi.Server.Game.Story;
using ZdoRpgAi.Server.Bootstrap;
using ZdoRpgAi.Server.Llm;

namespace ZdoRpgAi.Server.Game.Director;

public class SimpleReactiveStrategy : IDirectorStrategy {
    private static readonly ILog Log = Logger.Get<SimpleReactiveStrategy>();

    private readonly ILlm _mainLlm;
    private readonly ILlm _simpleLlm;
    private readonly IStory _story;
    private readonly NpcRepository _npcRepo;
    private readonly IRpcChannel _rpc;
    private readonly PlayerStateTracker _playerState;
    private readonly PlayerPersonaSection _playerPersonaConfig;

    public SimpleReactiveStrategy(ILlm mainLlm, ILlm simpleLlm, IStory story, NpcRepository npcRepo, IRpcChannel rpc, PlayerStateTracker playerState, PlayerPersonaSection playerPersonaConfig) {
        _mainLlm = mainLlm;
        _simpleLlm = simpleLlm;
        _story = story;
        _npcRepo = npcRepo;
        _rpc = rpc;
        _playerState = playerState;
        _playerPersonaConfig = playerPersonaConfig;
    }

    public async Task<List<StoryEvent>> ProcessStoryEventsAsync(List<StoryEvent> events) {
        Log.Trace("Processing {Count} events", events.Count);

        var playerIds = events.OfType<StoryEvent.PlayerSpeak>()
            .Select(ps => ps.PlayerCharacterId)
            .ToHashSet();
        Log.Trace("Found {Count} player IDs: {Ids}", playerIds.Count, string.Join(", ", playerIds));

        var (npcId, gameTime) = await FindLastTargetedNpcAsync(_rpc, events, playerIds);
        if (npcId == null) {
            Log.Info("No target NPC found in events");
            return [];
        }

        Log.Trace("Target NPC: {NpcId}, game time: {GameTime}", npcId, gameTime);

        try {
            var npcInfo = await _npcRepo.GetNpcInfoAsync(npcId);
            if (npcInfo == null) {
                Log.Warn("Could not get info for NPC {NpcId}", npcId);
                return [];
            }

            Log.Trace("NPC info: {Name} ({Race} {Sex})", npcInfo.Name, npcInfo.Race, npcInfo.Sex);
            var (history, summaries) = await _story.GetHistoryForCharacterAsync(npcId);
            Log.Trace("History: {HistoryCount} events, {SummaryCount} summaries", history.Count, summaries.Count);
            var (responseText, actions) = await GenerateNpcResponseAsync(npcInfo, history, summaries, playerIds.FirstOrDefault());
            if (responseText == null && (actions == null || actions.Count == 0)) {
                Log.Warn("LLM returned no response for NPC {NpcId}", npcId);
                return [];
            }

            // Execute any game actions the LLM requested
            if (actions != null && actions.Count > 0) {
                foreach (var action in actions) {
                    await ExecuteActionAsync(npcId, action, playerIds.FirstOrDefault());
                }
            }

            if (string.IsNullOrWhiteSpace(responseText)) {
                return [];
            }

            Log.Trace("Generated response for NPC {NpcId}: {ResponseLength} chars", npcId, responseText.Length);
            var npcSpeak = StoryEvent.Create(new StoryEvent.NpcSpeak {
                NpcCharacterId = npcId,
                TargetCharacterId = playerIds.FirstOrDefault(),
                GameTime = gameTime!,
                Text = responseText,
            });
            return [npcSpeak];
        }
        catch (Exception ex) {
            Log.Error("Failed to generate NPC response: {Error}", ex.Message);
            return [];
        }
    }

    private async Task ExecuteActionAsync(string npcId, LlmToolCall toolCall, string? defaultTargetId) {
        try {
            Log.Info("Executing action {Action} for NPC {NpcId}", toolCall.Name, npcId);

            var args = toolCall.Arguments;

            switch (toolCall.Name) {
                case "spawn_item": {
                    var itemId = GetArg<string>(args, "itemId");
                    var count = GetArg<int?>(args, "count") ?? 1;
                    if (itemId != null) {
                        _ = await _rpc.CallAsync(
                            nameof(ServerToModMessageType.SpawnOnGroundInFrontOfCharacter),
                            JsonExtensions.SerializeToObject(
                                new SpawnOnGroundInFrontOfCharacterPayload(npcId, itemId, count),
                                PayloadJsonContext.Default.SpawnOnGroundInFrontOfCharacterPayload));
                    }
                    break;
                }
                case "start_follow": {
                    var targetId = GetArg<string>(args, "targetId") ?? defaultTargetId;
                    if (targetId != null) {
                        _ = await _rpc.CallAsync(
                            nameof(ServerToModMessageType.NpcStartFollowCharacter),
                            JsonExtensions.SerializeToObject(
                                new NpcStartFollowCharacterPayload(npcId, targetId),
                                PayloadJsonContext.Default.NpcStartFollowCharacterPayload));
                    }
                    break;
                }
                case "stop_follow": {
                    _ = await _rpc.CallAsync(
                        nameof(ServerToModMessageType.NpcStopFollowCharacter),
                        JsonExtensions.SerializeToObject(
                            new NpcStopFollowCharacterPayload(npcId),
                            PayloadJsonContext.Default.NpcStopFollowCharacterPayload));
                    break;
                }
                case "attack": {
                    var targetId = GetArg<string>(args, "targetId") ?? defaultTargetId;
                    if (targetId != null) {
                        _ = await _rpc.CallAsync(
                            nameof(ServerToModMessageType.NpcAttack),
                            JsonExtensions.SerializeToObject(
                                new NpcAttackPayload(npcId, targetId),
                                PayloadJsonContext.Default.NpcAttackPayload));
                    }
                    break;
                }
                case "stop_attack": {
                    _ = await _rpc.CallAsync(
                        nameof(ServerToModMessageType.NpcStopAttack),
                        JsonExtensions.SerializeToObject(
                            new NpcStopAttackPayload(npcId),
                            PayloadJsonContext.Default.NpcStopAttackPayload));
                    break;
                }
                case "play_sound": {
                    var sound = GetArg<string>(args, "sound");
                    if (sound != null) {
                        _ = await _rpc.CallAsync(
                            nameof(ServerToModMessageType.PlaySound3dOnCharacter),
                            JsonExtensions.SerializeToObject(
                                new PlaySound3dOnCharacterPayload(npcId, sound),
                                PayloadJsonContext.Default.PlaySound3dOnCharacterPayload));
                    }
                    break;
                }
                case "transfer_item": {
                    var itemId = GetArg<string>(args, "itemId");
                    var count = GetArg<int?>(args, "count") ?? 1;
                    var fromId = GetArg<string>(args, "fromCharacterId") ?? npcId;
                    var toId = GetArg<string>(args, "toCharacterId") ?? defaultTargetId;
                    var isService = GetArg<bool?>(args, "isServicePayment") ?? false;
                    if (itemId != null && fromId != null && toId != null) {
                        _ = await _rpc.CallAsync(
                            nameof(ServerToModMessageType.TransferItem),
                            JsonExtensions.SerializeToObject(
                                new TransferItemPayload(fromId, toId, itemId, count, isService),
                                PayloadJsonContext.Default.TransferItemPayload));
                    }
                    break;
                }
                case "show_message": {
                    var message = GetArg<string>(args, "message");
                    if (message != null) {
                        _ = await _rpc.CallAsync(
                            nameof(ServerToModMessageType.ShowMessageBox),
                            JsonExtensions.SerializeToObject(
                                new ShowMessageBoxPayload(message),
                                PayloadJsonContext.Default.ShowMessageBoxPayload));
                    }
                    break;
                }
                default:
                    Log.Warn("Unknown action requested: {Action}", toolCall.Name);
                    break;
            }
        }
        catch (Exception ex) {
            Log.Error("Failed to execute action {Action}: {Error}", toolCall.Name, ex.Message);
        }
    }

    private static T? GetArg<T>(Dictionary<string, object?>? args, string key) {
        if (args == null) return default;
        if (!args.TryGetValue(key, out var value) || value == null) return default;
        if (value is T t) return t;
        try {
            return (T?)Convert.ChangeType(value, typeof(T));
        }
        catch {
            return default;
        }
    }

    private async Task<(string? NpcId, string? GameTime)> FindLastTargetedNpcAsync(
        IRpcChannel rpc, List<StoryEvent> events, HashSet<string> playerIds) {
        Log.Trace("Finding last targeted NPC from {Count} events", events.Count);
        for (var i = events.Count - 1; i >= 0; i--) {
            switch (events[i]) {
                case StoryEvent.PlayerSpeak ps:
                    Log.Trace("Checking PlayerSpeak event, explicit target: {Target}", ps.TargetCharacterId ?? "none");
                    var npcId = ps.TargetCharacterId ?? await DetermineTargetNpcAsync(rpc, ps);
                    if (npcId != null) {
                        return (npcId, ps.GameTime);
                    }

                    break;
                case StoryEvent.NpcSpeak ns when ns.TargetCharacterId != null && !playerIds.Contains(ns.TargetCharacterId):
                    Log.Trace("Found NpcSpeak targeting non-player: {Target}", ns.TargetCharacterId);
                    return (ns.TargetCharacterId, ns.GameTime);
            }
        }
        return (null, null);
    }

    private async Task<string?> DetermineTargetNpcAsync(IRpcChannel rpc, StoryEvent.PlayerSpeak evt) {
        Log.Trace("Determining target NPC for player {PlayerId}", evt.PlayerCharacterId);
        var hearResponse = await rpc.CallAsync(
            nameof(ServerToModMessageType.GetCharactersWhoHear),
            JsonExtensions.SerializeToObject(
                new GetCharactersWhoHearRequestPayload(evt.PlayerCharacterId),
                PayloadJsonContext.Default.GetCharactersWhoHearRequestPayload));

        var payload = hearResponse.Json?.DeserializeSafe(PayloadJsonContext.Default.GetCharactersWhoHearResponsePayload);
        if (payload == null) {
            Log.Info("GetCharactersWhoHear deserialization failed. Raw JSON: {Raw}", hearResponse.Json?.ToJsonString() ?? "null");
        }
        var nearby = payload?.Characters
            .Where(c => c.CharacterId != evt.PlayerCharacterId)
            .OrderBy(c => c.DistanceMeters)
            .ToArray() ?? [];

        Log.Trace("Found {Count} nearby characters", nearby.Length);
        if (nearby.Length == 0) {
            Log.Info("No nearby NPCs to respond. Raw JSON was: {Raw}", hearResponse.Json?.ToJsonString() ?? "null");
            return null;
        }

        if (nearby.Length == 1) {
            Log.Trace("Single nearby NPC: {NpcId}", nearby[0].CharacterId);
            return nearby[0].CharacterId;
        }

        var npcInfos = new List<(string Id, NpcInfo Info)>();
        foreach (var npc in nearby) {
            var info = await _npcRepo.GetNpcInfoAsync(npc.CharacterId);
            if (info != null) {
                npcInfos.Add((npc.CharacterId, info));
            }
        }

        Log.Trace("Resolved {Count} NPC infos out of {Total} nearby", npcInfos.Count, nearby.Length);
        if (npcInfos.Count == 0) {
            return null;
        }

        if (npcInfos.Count == 1) {
            Log.Trace("Single NPC with info: {NpcId}", npcInfos[0].Id);
            return npcInfos[0].Id;
        }

        var npcList = string.Join("\n", npcInfos.Select((n, i) =>
            $"- {n.Id}: {n.Info.Name} ({n.Info.Race} {n.Info.Sex}), distance: {nearby.First(c => c.CharacterId == n.Id).DistanceMeters:F1} meters"));

        Log.Trace("Asking simple LLM to choose among {Count} NPCs", npcInfos.Count);
        var request = new LlmRequest {
            SystemPrompt = "You are deciding which NPC a player is talking to. " +
                           "Respond with ONLY the character ID of the most likely target. " +
                           "Consider the speech content and NPC proximity. " +
                           "If unsure, pick the closest NPC.",
            Messages = [
                new LlmMessage {
                    Role = LlmRole.User,
                    Text = $"Nearby NPCs:\n{npcList}\n\nPlayer said: \"{evt.Text}\"\n\nWhich NPC ID is the player addressing?",
                },
            ],
        };

        var response = await _simpleLlm.ChatAsync(request);
        var chosenId = response.Text?.Trim();

        if (chosenId != null && npcInfos.Any(n => n.Id == chosenId)) {
            Log.Debug("Simple LLM chose NPC {NpcId} as target", chosenId);
            return chosenId;
        }

        Log.Debug("Simple LLM response '{Response}' did not match any NPC, falling back to closest", response.Text ?? "");
        return nearby[0].CharacterId;
    }

    private static readonly Dictionary<string, string> s_npcPersonalities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fargoth"] = "You are Fargoth, a nervous and secretive Wood Elf living in Seyda Neen. You are paranoid that people will find your hidden ring.",
        ["caius cosades"] = "You are Caius Cosades, the grizzled Imperial spymaster of the Blades in Balmora. You speak bluntly and have little patience for fools.",
    };

    private string? BuildPlayerPersonaBlock(string? playerId) {
        if (playerId == null) {
            Log.Info("PlayerPersonaBlock skipped: playerId is null");
            return null;
        }

        var state = _playerState.GetPlayerState(playerId);
        string? resolvedId = playerId;

        if (state == null) {
            var fallbackId = _playerState.ListPlayerIds().FirstOrDefault();
            if (fallbackId != null) {
                Log.Info("PlayerState not found for '{PlayerId}', falling back to '{FallbackId}'", playerId, fallbackId);
                state = _playerState.GetPlayerState(fallbackId);
                resolvedId = fallbackId;
            } else {
                Log.Info("PlayerState not found for '{PlayerId}' and no fallback players exist", playerId);
            }
        } else {
            Log.Info("PlayerState found for '{PlayerId}': {Race} {Sex} Level {Level}", playerId, state.Race, state.Sex, state.Level);
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_playerPersonaConfig?.Backstory)) {
            parts.Add($"BACKSTORY: {_playerPersonaConfig.Backstory}");
        }
        if (state != null && (_playerPersonaConfig?.IncludeStats ?? false)) {
            var deadMarker = state.IsDead ? " (DEAD)" : "";
            parts.Add($"NAME: {state.Name} | CLASS: {state.ClassName}{deadMarker} | STATS: Level {state.Level} {state.Race} {state.Sex}. " +
                      $"HP {state.HealthCurrent}/{state.HealthMax}, " +
                      $"Magicka {state.MagickaCurrent}/{state.MagickaMax}, " +
                      $"Fatigue {state.FatigueCurrent}/{state.FatigueMax}.");
        }
        if (state != null && (_playerPersonaConfig?.IncludeLocation ?? false) && !string.IsNullOrWhiteSpace(state.CellName)) {
            parts.Add($"LOCATION: Currently in {state.CellName}.");
        }

        if (parts.Count == 0) return null;

        var inner = string.Join(" ", parts);
        // Make it crystal clear this describes the PLAYER, not the NPC
        return $"--- INFORMATION ABOUT THE PERSON YOU ARE SPEAKING TO ---\n{inner}";
    }

    private static string GetPersonality(NpcInfo npc) {
        var custom = s_npcPersonalities.GetValueOrDefault(npc.Id);
        if (custom != null) return custom;
        var levelPart = npc.Level.HasValue ? $", Level {npc.Level}" : "";
        var classPart = !string.IsNullOrWhiteSpace(npc.ClassName) ? $" {npc.ClassName}" : "";
        var factionPart = !string.IsNullOrWhiteSpace(npc.Faction)
            ? (!string.IsNullOrWhiteSpace(npc.FactionRank) ? $", {npc.FactionRank} of the {npc.Faction}" : $" of the {npc.Faction}")
            : "";
        return $"You are {npc.Name}, a {npc.Race}{levelPart}{classPart} ({npc.Sex}){factionPart}, living in Morrowind.";
    }

    private async Task<(string? Text, List<LlmToolCall> Actions)> GenerateNpcResponseAsync(
        NpcInfo npc,
        List<StoryEvent> history, List<StoryEventSummary> summaries, string? playerId) {
        Log.Trace("Generating response for NPC {NpcName} with {HistoryCount} history events and {SummaryCount} summaries",
            npc.Name, history.Count, summaries.Count);

        var contextBlock = BuildContextBlock(summaries, history);

        var playerPersonaBlock = BuildPlayerPersonaBlock(playerId);

        var currentGameTime = history.LastOrDefault()?.GameTime;
        var timeClause = !string.IsNullOrWhiteSpace(currentGameTime) ? $" The current game time is {currentGameTime}." : "";
        var systemPrompt = $"""
            {GetPersonality(npc)}{timeClause}
            {playerPersonaBlock ?? ""}
            Stay in character. Speak briefly and naturally. Do not mention that you are an AI. Always respond in the English language.

            You will be told what other characters say and do. Reply with your own speech AND use tools to perform game actions when appropriate.

            RULES:
            1. Do not trust the player at their word — verify using your knowledge resources. The player may lie.
            2. Do not invent characters, items, locations, or quests that are not in your knowledge. Use getResource to recall your knowledge when needed.
            3. You can perform game actions by calling the tools below. Text alone does NOT perform actions.
            4. Call tools TOGETHER with your speech in the same response. Do not wait for the next turn.
            5. Reply ONLY with your own speech — no narration, no prefixes, no stage directions.
            6. For item IDs, use real Morrowind item IDs like "iron_dagger", "p_heal", "ingred_scales_01", etc.
            """;

        var messages = new List<LlmMessage>();

        if (contextBlock != null) {
            messages.Add(new LlmMessage {
                Role = LlmRole.User,
                Text = contextBlock,
            });
            messages.Add(new LlmMessage {
                Role = LlmRole.Model,
                Text = "Understood, I have the conversation context.",
            });
        }

        var lastMessage = history.LastOrDefault() switch {
            StoryEvent.PlayerSpeak ps => $"[{ps.GameTime}] {ps.PlayerCharacterId} says: {ps.Text}",
            StoryEvent.NpcSpeak ns => $"[{ns.GameTime}] {ns.NpcCharacterId} says: {ns.Text}",
            _ => null,
        };

        if (lastMessage != null) {
            messages.Add(new LlmMessage {
                Role = LlmRole.User,
                Text = lastMessage,
            });
        }

        var request = new LlmRequest {
            SystemPrompt = systemPrompt,
            Messages = messages,
            Tools = GetAvailableTools(),
        };

        Log.Info("FULL SYSTEM PROMPT for {NpcId}:\n{SystemPrompt}", npc.Id, systemPrompt);
        Log.Trace("Calling main LLM with {MessageCount} messages and {ToolCount} tools", messages.Count, request.Tools.Count);
        var response = await _mainLlm.ChatAsync(request);
        var text = response.Text?.Trim();
        var actions = response.ToolCalls ?? [];

        Log.Trace("Main LLM response: {TextLength} chars, {ActionCount} actions", text?.Length ?? 0, actions.Count);
        return (text, actions);
    }

    private static List<LlmTool> GetAvailableTools() {
        return new List<LlmTool> {
            new() {
                Name = "spawn_item",
                Description = "Spawn an item on the ground in front of the NPC. Use this to give items to the player.",
                Parameters = new List<LlmToolParameter> {
                    new() { Name = "itemId", Type = "string", Description = "Morrowind item ID, e.g. 'iron_dagger', 'p_heal', 'ingred_scales_01'" },
                    new() { Name = "count", Type = "integer", Description = "Quantity (default 1)", Required = false },
                }
            },
            new() {
                Name = "start_follow",
                Description = "Make the NPC start following a target. Defaults to following the player if no target is specified.",
                Parameters = new List<LlmToolParameter> {
                    new() { Name = "targetId", Type = "string", Description = "Character ID to follow. Omit to follow the player.", Required = false },
                }
            },
            new() {
                Name = "stop_follow",
                Description = "Make the NPC stop following anyone.",
                Parameters = new List<LlmToolParameter>()
            },
            new() {
                Name = "attack",
                Description = "Make the NPC attack a target. Defaults to attacking the player if no target is specified.",
                Parameters = new List<LlmToolParameter> {
                    new() { Name = "targetId", Type = "string", Description = "Character ID to attack. Omit to attack the player.", Required = false },
                }
            },
            new() {
                Name = "stop_attack",
                Description = "Make the NPC stop attacking and calm down.",
                Parameters = new List<LlmToolParameter>()
            },
            new() {
                Name = "play_sound",
                Description = "Play a sound effect from the NPC's position.",
                Parameters = new List<LlmToolParameter> {
                    new() { Name = "sound", Type = "string", Description = "Sound ID, e.g. 'Swim Left', 'Item Armor Heavy Updown'" },
                }
            },
            new() {
                Name = "transfer_item",
                Description = "Move an item from one character to another. Use for giving items, buying, selling, or paying for services. Gold uses itemId 'Gold_001'.",
                Parameters = new List<LlmToolParameter> {
                    new() { Name = "itemId", Type = "string", Description = "Morrowind item ID, e.g. 'Gold_001', 'iron_dagger', 'p_restore_health_c'" },
                    new() { Name = "count", Type = "integer", Description = "Quantity (default 1)", Required = false },
                    new() { Name = "fromCharacterId", Type = "string", Description = "Character ID giving the item. Omit if the NPC is giving it.", Required = false },
                    new() { Name = "toCharacterId", Type = "string", Description = "Character ID receiving the item. Omit if the player is receiving it.", Required = false },
                    new() { Name = "isServicePayment", Type = "boolean", Description = "True if this is payment for a service (training, healing, etc.)", Required = false },
                }
            },
            new() {
                Name = "show_message",
                Description = "Show a message box to the player.",
                Parameters = new List<LlmToolParameter> {
                    new() { Name = "message", Type = "string", Description = "Message text to display" },
                }
            },
        };
    }

    private static string? BuildContextBlock(List<StoryEventSummary> summaries, List<StoryEvent> events) {
        var parts = new List<string>();

        if (summaries.Count > 0) {
            parts.Add("PREVIOUS CONVERSATION SUMMARIES:");
            foreach (var summary in summaries) {
                parts.Add(summary.Summary);
            }
        }

        // Include all events except the very last one (which is the current player message)
        var contextEvents = events.Count > 1 ? events[..^1] : [];
        if (contextEvents.Count > 0) {
            parts.Add("RECENT EVENTS:");
            foreach (var evt in contextEvents) {
                parts.Add(evt switch {
                    StoryEvent.PlayerSpeak ps => $"[{ps.GameTime}] {ps.PlayerCharacterId} says: {ps.Text}",
                    StoryEvent.NpcSpeak ns => $"[{ns.GameTime}] {ns.NpcCharacterId} says: {ns.Text}",
                    _ => evt.ToString()!,
                });
            }
        }

        return parts.Count > 0 ? string.Join("\n", parts) : null;
    }
}
