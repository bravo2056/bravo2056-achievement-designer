# ⚔️ BRAVO2056 Achievement System

A custom Twitch achievement system built for [bravo2056](https://twitch.tv/bravo2056), inspired by Dungeon Crawler Carl. Dark humor, absurd lore, and real-time OBS overlays.

> *The Streamer keeps score. The System never forgets.*

---

## What's In The Box

| File | What It Does |
|---|---|
| `twitch-achievement-designer.html` | Design and preview achievements — open in any browser |
| `achievement-overlay.html` | OBS Browser Source — shows animated popups on stream |
| `achievements.html` | Public Achievement Hall — viewers look up what they've earned |
| `achievements-data.json` | Live data file — who earned what |
| `push-achievements.bat` | One-click update — pushes data to the public Hall |

---

## Installation

### 1. Streamer.bot
- Download [Streamer.bot](https://streamer.bot)
- Import the included `.sb` action file (Actions → Import)
- Start the WebSocket server: **Servers/Clients → WebSocket Server → Start** (port 8080)

### 2. OBS
- Add a new **Browser Source**
- Set the URL to the full local path of `achievement-overlay.html`
  - Example: `C:\stream\achievement-overlay.html`
- Width: `1920` Height: `1080`
- Check **Shutdown source when not visible**

### 3. Achievement Hall (public)
- Hosted at: `https://bravo2056.github.io/bravo2056-achievement-designer/achievements.html`
- Updates automatically when you run `push-achievements.bat` after a stream

---

## How To Use

### Designing Achievements
1. Open `twitch-achievement-designer.html` in a browser
2. Fill in name, flavor text, rarity, icon, sound, and XP
3. Click **Fire Test Animation** (or press **T**) to preview
4. Click **Add to Collection** when happy
5. Click **Export JSON** to save your achievement definitions

### Granting Achievements On Stream
- **Manual:** Type `!grant [shortcode] @viewer` in chat (broadcaster/mods only)
- **Automatic:** First Blood fires on a viewer's first chat message

### Updating The Public Hall
After any stream where achievements were earned:
1. Double-click `push-achievements.bat`
2. Done — the Hall updates within 2 minutes

---

## Adding A New Achievement

1. Design it in `twitch-achievement-designer.html`
2. Export JSON and add the new entry to `achievements-data.json`
3. Set up the corresponding action in Streamer.bot
4. Run `push-achievements.bat`

---

## Achievement List

| Achievement | Rarity | Trigger | XP |
|---|---|---|---|
| Annoy The Streamer | Legendary | Manual (`!grant annoy`) | 1000 |
| First Blood | Common | Auto (first chat message) | 25 |
| The Arty Did It | Legendary | Manual (`!grant arty`) | 1000 |
| Unsolicited Financial Advice | Rare | Manual (`!grant advice`) | 300 |
| The Floor Is Chat | Uncommon | Auto | 150 |
| Witnessed A Death | Epic | Auto | 500 |
| Shenanigans Survivor | Uncommon | Auto | 150 |
| Good Vibes Certified | Epic | Auto | 500 |

---

## Tech Stack

- **Streamer.bot** — action engine, WebSocket server, chat commands
- **OBS Browser Source** — transparent overlay, 1920×1080
- **Web Audio API** — generated sound effects, no files needed
- **GitHub Pages** — free static hosting for the Achievement Hall
- **Vanilla HTML/CSS/JS** — no frameworks, no installs, works everywhere

---

## License

MIT License — © 2026 Garrison. Free to use and modify. Credit appreciated.
