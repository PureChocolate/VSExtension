# VSExtend

Personal Discord Rich Presence for Visual Studio (v17/v18, SDK 17.x API).

Shows: active file, project, solution, git branch, modified-file count, debug state and an elapsed timer.
No buttons, no URLs, no GitHub links.

## First-time setup

1. **Discord app** (done): discord.com/developers/applications -> your app -> copy the numeric Application ID.
2. **Icons**: `python tools/build_icons.py` (needs `pip install resvg-py`, python >= 3.10).
   Produces `assets_pack/*.png` (1024x1024, key name = asset key). Upload **all** of them in
   discord.com/developers/applications -> your app -> Rich Presence -> Art Assets
   (image names must stay the same as the file names; Discord lowercases them, which matches already).
3. **Build/run**: open `VSExten.sln` in VS with the "Visual Studio extension development" workload,
   press F5 (deploys to the Experimental instance).
4. **Configure**: Tools > Options > **VSExtend > General**:
   - Enabled: on
   - Application ID: paste your ID
   Then check the **VSExtend** pane in the Output window ("connected to Discord").

Tools > **Discord Rich Presence** (toggle on the Tools menu) enables/disables at runtime.

## Customization

- File-type -> asset key mapping: `src/VSExtend/Presence/AssetMap.cs`
- Presence text formatting: `src/VSExtend/Presence/PresenceBuilder.cs`
- To add icons: add a key to `tools/icons.json`, put a `tools/svg/<key>.svg` or a devicon slug,
  re-run `build_icons.py`, upload the new PNG, and map extensions to it in `AssetMap.cs`.

## Notes

- Git state is plain text only (branch, modified count) — repo paths are never sent.
- `Debugging`/`Paused` small icon uses the `debugging` asset.
- An icon that is not uploaded falls back to the generic `visualstudio` asset.
- Your Discord Application ID is stored in the Windows registry (Tools > Options), never in
  this repo — nothing secret gets committed here.
