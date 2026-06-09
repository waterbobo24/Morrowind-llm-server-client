using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZdoRpgAi.Database;

namespace ZdoRpgAi.Repository;

public class LocalDatabaseSaveGameRepository : ISaveGameRepository, IDisposable {
    private readonly string _basePath;
    private SaveGameDatabase? _db;
    private readonly object _lock = new();
    private string? _currentSaveId;

    public LocalDatabaseSaveGameRepository(string basePath) {
        _basePath = basePath;
    }

    public void SetSaveContext(string? saveId) {
        lock (_lock) {
            if (_currentSaveId == saveId) return;
            _db?.Dispose();
            _db = null;
            var path = string.IsNullOrEmpty(saveId) ? _basePath : GetSaveDbPath(saveId);
            _db = new SaveGameDatabase(path);
            _db.Open();
            _currentSaveId = saveId;
        }
    }

    private string GetSaveDbPath(string saveId) {
        var dir = Path.GetDirectoryName(_basePath) ?? ".";
        Directory.CreateDirectory(dir);
        var baseName = Path.GetFileNameWithoutExtension(_basePath);
        var ext = Path.GetExtension(_basePath);
        return Path.Combine(dir, $"{baseName}_{saveId}{ext}");
    }

    private T WithDb<T>(Func<SaveGameDatabase, T> action) {
        lock (_lock) {
            if (_db == null) SetSaveContext(null);
            return action(_db!);
        }
    }

    private void WithDb(Action<SaveGameDatabase> action) {
        lock (_lock) {
            if (_db == null) SetSaveContext(null);
            action(_db!);
        }
    }

    public long AddStoryEvent(string gameTime, string realTime, string type, string dataJson) {
        return WithDb(db => {
            using var cmd = db.Connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO story_event (gameTime, realTime, type, dataJson)
                VALUES ($gameTime, $realTime, $type, $dataJson)
                RETURNING id
                """;
            cmd.Parameters.AddWithValue("$gameTime", gameTime);
            cmd.Parameters.AddWithValue("$realTime", realTime);
            cmd.Parameters.AddWithValue("$type", type);
            cmd.Parameters.AddWithValue("$dataJson", dataJson);
            return (long)cmd.ExecuteScalar()!;
        });
    }

    public void AddStoryEventObservers(long storyEventId, string[] characterIds) {
        if (characterIds.Length == 0) return;
        WithDb(db => {
            using var tx = db.Connection.BeginTransaction();
            foreach (var characterId in characterIds) {
                using var cmd = db.Connection.CreateCommand();
                cmd.CommandText = "INSERT INTO story_event_observer (storyEventId, characterId) VALUES ($eventId, $charId)";
                cmd.Parameters.AddWithValue("$eventId", storyEventId);
                cmd.Parameters.AddWithValue("$charId", characterId);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        });
    }

    public List<RawStoryEvent> GetActiveStoryEventsForCharacter(string characterId) {
        return WithDb(db => {
            using var cmd = db.Connection.CreateCommand();
            cmd.CommandText = """
                SELECT DISTINCT se.id, se.gameTime, se.realTime, se.type, se.dataJson
                FROM story_event se
                LEFT JOIN story_event_observer seo ON se.id = seo.storyEventId
                WHERE se.archivedTo IS NULL
                  AND (
                    json_extract(se.dataJson, '$.playerCharacterId') = $charId
                    OR json_extract(se.dataJson, '$.targetCharacterId') = $charId
                    OR json_extract(se.dataJson, '$.npcCharacterId') = $charId
                    OR seo.characterId = $charId
                  )
                ORDER BY se.id
                """;
            cmd.Parameters.AddWithValue("$charId", characterId);

            var results = new List<RawStoryEvent>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                results.Add(new RawStoryEvent(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4)));
            }
            return results;
        });
    }

    public List<RawStoryEventSummary> GetActiveSummariesForCharacter(string characterId) {
        return WithDb(db => {
            using var cmd = db.Connection.CreateCommand();
            cmd.CommandText = """
                SELECT DISTINCT s.id, s.summary, s.realTime
                FROM story_event_summary s
                JOIN story_event_summary_observer so ON s.id = so.summaryId
                WHERE so.characterId = $charId AND s.archivedTo IS NULL
                ORDER BY s.id
                """;
            cmd.Parameters.AddWithValue("$charId", characterId);

            var results = new List<RawStoryEventSummary>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                results.Add(new RawStoryEventSummary(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetString(2)));
            }
            return results;
        });
    }

    public long AddStoryEventSummary(string summary, string realTime, string[] characterIds) {
        return WithDb(db => {
            using var tx = db.Connection.BeginTransaction();

            using var cmd = db.Connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO story_event_summary (summary, realTime)
                VALUES ($summary, $realTime)
                RETURNING id
                """;
            cmd.Parameters.AddWithValue("$summary", summary);
            cmd.Parameters.AddWithValue("$realTime", realTime);
            var summaryId = (long)cmd.ExecuteScalar()!;

            foreach (var characterId in characterIds) {
                using var obsCmd = db.Connection.CreateCommand();
                obsCmd.CommandText = "INSERT INTO story_event_summary_observer (summaryId, characterId) VALUES ($summaryId, $charId)";
                obsCmd.Parameters.AddWithValue("$summaryId", summaryId);
                obsCmd.Parameters.AddWithValue("$charId", characterId);
                obsCmd.ExecuteNonQuery();
            }

            tx.Commit();
            return summaryId;
        });
    }

    public void ArchiveStoryEvents(long[] eventIds, long summaryId) {
        if (eventIds.Length == 0) return;
        WithDb(db => {
            using var tx = db.Connection.BeginTransaction();
            foreach (var eventId in eventIds) {
                using var cmd = db.Connection.CreateCommand();
                cmd.CommandText = "UPDATE story_event SET archivedTo = $summaryId WHERE id = $eventId";
                cmd.Parameters.AddWithValue("$summaryId", summaryId);
                cmd.Parameters.AddWithValue("$eventId", eventId);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        });
    }

    public void ArchiveStoryEventSummaries(long[] summaryIds, long newSummaryId) {
        if (summaryIds.Length == 0) return;
        WithDb(db => {
            using var tx = db.Connection.BeginTransaction();
            foreach (var summaryId in summaryIds) {
                using var cmd = db.Connection.CreateCommand();
                cmd.CommandText = "UPDATE story_event_summary SET archivedTo = $newSummaryId WHERE id = $summaryId";
                cmd.Parameters.AddWithValue("$newSummaryId", newSummaryId);
                cmd.Parameters.AddWithValue("$summaryId", summaryId);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        });
    }

    public RawNpcInfo? GetNpcInfo(string npcId) {
        return WithDb(db => {
            using var cmd = db.Connection.CreateCommand();
            cmd.CommandText = "SELECT dataJson FROM npc_new WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", npcId);
            var json = cmd.ExecuteScalar() as string;
            if (json == null) return null;

            var data = JsonSerializer.Deserialize(json, NpcDataJsonContext.Default.NpcDataJson);
            if (data == null) return null;

            return new RawNpcInfo(npcId, data.Name, data.Race, data.Sex, data.ClassName, data.Faction, data.FactionRank, data.Level);
        });
    }

    public void SaveNpcInfo(RawNpcInfo info) {
        WithDb(db => {
            var json = JsonSerializer.Serialize(new NpcDataJson(info.Name, info.Race, info.Sex, info.ClassName, info.Faction, info.FactionRank, info.Level), NpcDataJsonContext.Default.NpcDataJson);
            using var cmd = db.Connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO npc_new (id, dataJson) VALUES ($id, $data)
                ON CONFLICT(id) DO UPDATE SET dataJson = $data
                """;
            cmd.Parameters.AddWithValue("$id", info.Id);
            cmd.Parameters.AddWithValue("$data", json);
            cmd.ExecuteNonQuery();
        });
    }

    public void Dispose() {
        lock (_lock) {
            _db?.Dispose();
            _db = null;
        }
    }
}

internal record NpcDataJson(string Name, string Race, string Sex, string? ClassName = null, string? Faction = null, string? FactionRank = null, int? Level = null);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(NpcDataJson))]
internal partial class NpcDataJsonContext : JsonSerializerContext;