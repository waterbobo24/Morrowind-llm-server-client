Morrowind LLM Server Client

**.NET backend for the Morrowind LLM Mod.**  
Proxies OpenMW game events to LLM APIs and handles voice synthesis/speech recognition.

> **Fork Notice:** This is a community fork of [drzdo's zdo-rpg-ai server](https://github.com/drzdo/zdo-rpg-ai), extended with additional features and backends.  
> Original work © [drzdo](https://github.com/drzdo) (GPLv3).

---

## ⚠️ Work in Progress — Experimental

This server is under active development. Configuration formats and endpoints may change. Use at your own risk.

---

## 🤖 AI Disclosure

**Portions of this fork — including code, documentation, and design decisions — were created or significantly modified with assistance from large language models (e.g., Claude, ChatGPT).**  
All generated content has been reviewed, tested, and curated by a human maintainer, but errors or oversights may remain. The original upstream project was human-authored by drzdo.

---

## ✨ Features

- **LLM Support:** OpenAI GPT, Anthropic Claude, Google Gemini
- **Voice Output:** ElevenLabs (cloud) or **Pocket TTS** (local/offline via Python/piper)
- **Speech-to-Text:** Deepgram (optional)
- **Vector Memory:** ChromaDB for persistent NPC recollection across sessions

---

## 📋 Requirements

- [.NET SDK](https://dotnet.microsoft.com/download) (8.0 or later recommended)
- Python 3.x (only if using **Pocket TTS**)
- API keys for your chosen providers

---

## 🚀 Installation

```bash
git clone https://github.com/waterbobo24/Morrowind-llm-server-client.git
cd Morrowind-llm-server-client
# Restore and run (adjust path to .csproj as needed)
dotnet run --project src/ZdoRpgAi.Server

⚙️ Configuration

    Copy the example config template:

    cp example/server-config.example.yaml .tmp/server-config.yaml

    Edit .tmp/server-config.yaml:
        Add your LLM API key (openai_api_key, anthropic_api_key, or google_gemini_api_key)
        Optional: add ElevenLabs API key for cloud voice
        Optional: enable Pocket TTS for local voice synthesis
        Optional: add Deepgram API key for speech-to-text

    Security: .tmp/ is gitignored. Your API keys will never be committed.

💻 Platform Notes
Platform	Status
Linux	✅ Primary development target
Windows	✅ Supported via .NET
macOS	✅ Supported via .NET
📄 License

GNU General Public License v3.0
	
Original author	drzdo(opens in new tab)
Fork author	waterbobo24(opens in new tab)
"""	
with open("README.md", "w") as f:	
