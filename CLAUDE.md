# MyGame

**The Ferryman's Apprentice.** Design brief: `docs/brief.md`. Glossary (use these terms): `CONTEXT.md`.
Decision history: `.scratch/game-brief/` (wayfinder map + tickets).

3D game: Unity 6.6 (6000.6.3f1, URP) + Blender 5.2 for models. Driven mostly from the CLI.

## Layout

```
art/              Blender sources (.blend) — NOT inside the Unity project
  models/         one .blend per asset; file name = exported FBX name
  textures/       source textures (.psd/.kra/high-res), exported copies go to game/
  reference/      concept art, refs
tools/
  export-models.sh      art/models/*.blend -> game/Assets/_Project/Art/Models/*.fbx
  blender/export_fbx.py FBX export settings (Z-up -> Y-up, 1 unit = 1 m)
  unity.sh              open | import | build-mac | build-webgl | test
game/             Unity project
  Assets/_Project/      all our content (keeps it apart from third-party packages)
    Art/{Models,Materials,Textures}  Audio  Prefabs  Scenes  Scripts/{Runtime,Editor}
    Scripts/Runtime/      game code, assembly MyGame.Runtime (namespace MyGame)
    Tests/PlayMode/       PlayMode tests, assembly MyGame.Tests.PlayMode
  Assets/Settings/      URP pipeline assets (from template)
builds/           build output and CLI logs (not versioned)
```

## Workflow

- Model in Blender, save to `art/models/<name>.blend`, run `tools/export-models.sh <name>`.
  Never edit the FBX in `game/`; it's generated and gets overwritten.
- Blender conventions: apply transforms (Ctrl+A) before export, origin at the model's base,
  1 unit = 1 metre.
- Headless Unity commands (`tools/unity.sh import|build-*|test`) fail if the editor has the
  project open. Close the editor first, or make the change in the editor.
- Unity logs from the CLI end up in `builds/unity-<command>.log`.
- Main scene: `Assets/_Project/Scenes/Main.unity` (the only scene in Build Settings).
- Tests: `tools/unity.sh test` runs EditMode + PlayMode headless; results in `builds/test-results-*.xml`.
  New game code goes in `Scripts/Runtime/` so tests can reference it.
- Version control: git + LFS. Binaries (.blend, .fbx, images, audio, fonts) go through LFS via
  `.gitattributes`; add new binary extensions there before committing them.
- Input: new Input System (`Assets/InputSystem_Actions.inputactions`).
