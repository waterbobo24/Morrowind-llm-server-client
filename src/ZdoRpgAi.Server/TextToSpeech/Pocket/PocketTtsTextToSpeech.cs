using System.Diagnostics;
using ZdoRpgAi.Core;

namespace ZdoRpgAi.Server.TextToSpeech.Pocket;

public class PocketTtsTextToSpeech : ITextToSpeech {
    private static readonly ILog Log = Logger.Get<PocketTtsTextToSpeech>();
    private readonly PocketTtsConfig _config;

    public PocketTtsTextToSpeech(PocketTtsConfig config) {
        _config = config;
    }

    public async Task<ITextToSpeechOutput> GenerateAsync(ITextToSpeechInput input) {
        // Normalize voice key: "Dark Elf" + "male" → "darkelfmale"
        var voiceKey = $"{input.npcRace}{input.npcSex}".Replace(" ", "").ToLowerInvariant();
        
        if (!_config.VoiceMap.TryGetValue(voiceKey, out var voicePath)) {
            Log.Warn("No voice mapping for '{VoiceKey}' (NPC: {Npc}, Race: {Race}, Sex: {Sex}), using fallback",
                voiceKey, input.npcName, input.npcRace, input.npcSex);
            voicePath = _config.FallbackVoice;
        }

        var outFile = Path.Combine(Path.GetTempPath(), $"pockettts_{Guid.NewGuid()}.mp3");

        Log.Debug("Running PocketTTS: {Exe} --text \"...\" --voice \"{Voice}\" --output \"{Out}\"",
            _config.ExecutablePath, voicePath, outFile);

        var psi = new ProcessStartInfo {
            FileName = _config.ExecutablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add("--text");
        psi.ArgumentList.Add(input.text);
        psi.ArgumentList.Add("--voice");
        psi.ArgumentList.Add(voicePath);
        psi.ArgumentList.Add("--output");
        psi.ArgumentList.Add(outFile);

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start pockettts process");

        await proc.WaitForExitAsync();

        if (proc.ExitCode != 0) {
            var err = await proc.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"PocketTTS failed (exit {proc.ExitCode}): {err}");
        }

        if (!File.Exists(outFile)) {
            throw new FileNotFoundException("PocketTTS did not create output file", outFile);
        }

        var bytes = await File.ReadAllBytesAsync(outFile);
        try { File.Delete(outFile); } catch { }

        return new ITextToSpeechOutput {
            Mp3Bytes = bytes,
        };
    }
}
