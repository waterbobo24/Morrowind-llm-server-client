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


## 🚀 Installation & Running

You need **two** .NET processes running simultaneously: the **Server** (handles LLM APIs, memory, and voice) and the **Client Console** (bridges OpenMW ↔ Server).

### 1. Clone and enter the repo

```bash
git clone https://github.com/waterbobo24/Morrowind-llm-server-client.git
cd Morrowind-llm-server-client

2. Configure credentials

cp example/server-config.example.yaml .tmp/server-config.yaml
# Edit .tmp/server-config.yaml with your API keys

3. Terminal 1 — Start the Server

dotnet run --project src/ZdoRpgAi.Server

4. Terminal 2 — Start the Client

dotnet run --project src/ZdoRpgAi.Client.Console -- --config .tmp/client-config.yaml

    Tip — Background the client in one terminal:

    nohup dotnet run --project src/ZdoRpgAi.Client.Console -- --config .tmp/client-config.yaml > /tmp/client.log 2>&1 &
    sleep 2


---



```markdown
## ⚙️ Quick Start

1. Start the **Server** (`dotnet run --project src/ZdoRpgAi.Server` in the server repo).
2. Start the **Client** (`dotnet run --project src/ZdoRpgAi.Client.Console -- --config .tmp/client-config.yaml`).
   - Or background it: `nohup dotnet run ... > /tmp/client.log 2>&1 &`
3. Launch **OpenMW** with this mod enabled.
4. Approach any NPC and talk (push-to-talk or text input via Zenity).

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
