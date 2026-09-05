# VSExtension

Discord Rich Presence for Visual Studio. Shows the active file, project, solution, git branch,
working-tree state, debug state, and the caret position on your Discord profile — built as a
self-contained, fully editable VSIX extension (no runtime dependencies beyond Discord itself).

## Features

- Active document, project, and solution shown in the presence state
- Git branch and modified-file count (plain text; repository paths are never sent)
- Debug mode indicator with a dedicated icon while a debug session is active
- Cursor position (line/column), refreshed via a lightweight watcher
- Per-language file icons rendered from the asset pack, with a fallback icon
- Per-session elapsed timer
- Debounced updates with reconnection handling (survives Discord restarts)
- No buttons / no URL fields: nothing in the presence is clickable

## How it works

- In-proc VSSDK `AsyncPackage` (`net472`, VS SDK 17.x API — compatible with VS 2022 17.14+ and
  VS 2026, per the VS extension compatibility model)
- DTE2 events (window, document, solution, debugger, selection) feed a `PresenceContext`
- `DiscordService` owns a `DiscordRpcClient` (DiscordRichPresence NuGet, MIT), debounces updates,
  reconnects on pipe loss, and clears presence on shutdown
- `GitService` shells out to `git` on a background thread, cached per solution
- Settings are written from the Tools menu pop-up or the Tools > Options page and are stored
  per-user in the Windows registry — never in the repository

## Baseline install

1. Create a Discord application at <https://discord.com/developers/applications> and copy the
   application (client) ID. Optionally set an application name and icon there — that is the text
   and icon Discord displays next to "Playing".
2. Build and install the VSIX:
   - Open `VSExten.sln` in Visual Studio with the "Visual Studio extension development" workload
     and press F5 (deploys to the Experimental instance), or build and run the generated
     `VSExtend.vsix` from `src\VSExtend\bin\Debug\net472`.
3. Run `python tools\build_icons.py` (requires `pip install resvg-py`, Python 3.10+) to produce
   `assets_pack/*.png` (1024x1024), then upload every PNG under your application's
   **Rich Presence → Art Assets** page. File names are the asset keys.
4. Tools → **Discord Rich Presence Settings...** → paste the application ID.

Check the **VSExtend** pane in the Output window for connection/diagnostic messages.

## Project layout

```
src/VSExtend/
  VSExtendPackage.cs        package lifecycle, DTE event wiring, commands
  Options/                  Tools > Options page (DialogPage) + snapshot
  Settings/                 WPF settings window (Tools menu pop-up)
  Presence/                 DiscordService, PresenceBuilder, AssetMap, context
  Git/                      GitService (branch, modified count)
tools/
  icons.json                asset key -> devicon slug mapping
  build_icons.py            local rendering pipeline (resvg-py)
  svg/                      hand-authored SVGs (latex, toml, bash, debugging)
```

## Customization

- `src/VSExtend/Presence/AssetMap.cs` — file extension → asset key mapping
- `src/VSExtend/Presence/PresenceBuilder.cs` — presence text layout
- `tools/icons.json` + `tools/svg/` — asset pack contents; re-run `build_icons.py` after changes
  and upload the new PNGs

## Privacy

Presence carries only text (file name, project/solution, branch, modified count, line/column)
and asset keys. No repository paths, URLs, secrets, or clickable links are included.

## License

MIT.
