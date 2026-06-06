using ZdoRpgAi.Core;
using ZdoRpgAi.Server.Llm.Gemini;
using ZdoRpgAi.Server.Llm.OpenAi;
using ZdoRpgAi.Server.SpeechToText.Deepgram;
using ZdoRpgAi.Server.TextToSpeech.ElevenLabs;
using ZdoRpgAi.Server.TextToSpeech.Pocket;
using ZdoRpgAi.Server.Util.Mp3;

namespace ZdoRpgAi.Server.Bootstrap;

public class ServerConfig {
    public LogConfig Log { get; set; } = new();
    public required DatabaseSection Database { get; set; }
    public HttpServerSection HttpServer { get; set; } = new();
    public required TtsSection Tts { get; set; }
    public required SttSection Stt { get; set; }
    public required LlmSection Llm { get; set; }
    public DirectorSection Director { get; set; } = new();
    public PlayerPersonaSection PlayerPersona { get; set; } = new();
}

public class PlayerPersonaSection {
    /// <summary>
    /// Static backstory injected into every LLM prompt, e.g. "A mysterious outlander who arrived by boat."
    /// </summary>
    public string Backstory { get; set; } = "";

    /// <summary>
    /// If true, inject current health/magicka/fatigue/level into LLM prompt.
    /// </summary>
    public bool IncludeStats { get; set; } = true;

    /// <summary>
    /// If true, inject current cell name into LLM prompt.
    /// </summary>
    public bool IncludeLocation { get; set; } = true;
}

public class DirectorSection {
    public int CompactThreshold { get; set; } = 30;
    public int CompactKeepRecent { get; set; } = 10;
}

public class DatabaseSection {
    public required string MainDbPath { get; set; }
    public required string SaveGameDbPath { get; set; }
}

public class HttpServerSection {
    public string Host { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 8080;
    public int MaxMessageSize { get; set; } = 10_485_760;
    public int RpcTimeoutMs { get; set; } = 5000;
    public string ClientToken { get; set; } = "";
}

public class TtsSection {
    public required string Provider { get; set; }
    public ElevenLabsConfig? ElevenLabs { get; set; }
    public PocketTtsConfig? Pocket { get; set; }
    public Mp3SpeedConfig Mp3Speed { get; set; } = new();
}

public class SttSection {
    public required string Provider { get; set; }
    public DeepgramConfig? Deepgram { get; set; }
}

public class LlmSection {
    public required LlmProviderSection Main { get; set; }
    public required LlmProviderSection Simple { get; set; }
}

public class LlmProviderSection {
    public required string Provider { get; set; }
    public GeminiConfig? Gemini { get; set; }
    public OpenAiConfig? OpenAi { get; set; }
}
