namespace ZdoRpgAi.Server.TextToSpeech.Pocket;

public sealed class PocketTtsConfig {
    public string ExecutablePath { get; set; } = "/usr/local/bin/pockettts";
    public string OutputFormat { get; set; } = "wav";
    public Dictionary<string, string> VoiceMap { get; set; } = new();
    public string FallbackVoice { get; set; } = "default";
}
