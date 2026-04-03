# Unity Vibe Code

This repo is set up as a clean home for building a small 2D rigidbody platformer together in Unity.

## Project location

The Unity project now lives at the repo root:

`/Users/david/vibe-with-ben/unity-vibe-code`

Open this folder from Unity Hub going forward so the Unity project and the Git repo stay in the same place.

## Current starter setup

The project includes:

- a Unity-safe root `.gitignore`
- text-friendly Unity `.gitattributes`
- a `Main` scene in `Assets/Scenes/Main.unity`
- starter scripts under `Assets/Scripts/`

## Build the Gravity Garden slice

After Unity finishes importing scripts:

1. Open the repo-root project in Unity Hub.
2. Open `Assets/Scenes/Main.unity` if it is not already open.
3. In the Unity menu bar, click `Vibe` -> `Build Platformer Starter Scene`.
4. Press Play.

The scene builder will refresh the current Gravity Garden playable slice with placeholder art, player movement, seeds, a HUD, an exit portal, and a fall-respawn zone.

## Default controls

- move: `A` / `D` or left / right arrow
- jump: `Space`

## Gravity Garden gameplay loop

This first slice is a short one-screen platforming run:

- start on the glowing garden patch at the left side of the level
- touch the checkpoint marker as you move through the level to update your respawn spot
- collect enough `energy seeds` to power the exit portal
- avoid falling into the kill zone below the level or you will respawn at the start or your latest checkpoint
- once the HUD says the portal is ready, reach the exit on the right to win the slice

## Manual movement feel checklist

- Hold jump for a full hop, then tap jump for a shorter hop. The difference should feel easy to control.
- Run, let go, and reverse direction. The player should speed up smoothly but stop and turn without feeling slippery.
- Watch the placeholder body states: idle on the ground, bouncy run while moving, and jump visuals while airborne.
- Jump and land a few times, then fall into the kill zone after touching the checkpoint. You should see simple dust feedback and respawn at the checkpoint instead of the start.

## What should be committed

Keep these in Git:

- `Assets/`
- `Packages/`
- `ProjectSettings/`

Do not commit Unity's generated folders such as `Library`, `Temp`, `Obj`, `Build`, `Logs`, or `UserSettings`. The root `.gitignore` already covers those.

## Good next steps

Once you have the starter scene running, I can help you:

- turn the placeholder blocks into proper sprites or tilemaps
- add collectibles, enemies, and hazards
- split code into player, world, and game systems
- add simple tests and a cleaner CI-friendly repo workflow

## Automation

The repo now includes local Unity CLI wrappers and a starter GitHub Actions workflow:

- [build_win64.sh](/Users/david/vibe-with-ben/unity-vibe-code/scripts/build_win64.sh)
- [build_osx.sh](/Users/david/vibe-with-ben/unity-vibe-code/scripts/build_osx.sh)
- [test_editmode.sh](/Users/david/vibe-with-ben/unity-vibe-code/scripts/test_editmode.sh)
- [test_playmode.sh](/Users/david/vibe-with-ben/unity-vibe-code/scripts/test_playmode.sh)
- [unity-ci.yml](/Users/david/vibe-with-ben/unity-vibe-code/.github/workflows/unity-ci.yml)
- [BuildScripts.cs](/Users/david/vibe-with-ben/unity-vibe-code/Assets/Scripts/Editor/BuildScripts.cs)

### Local usage

If Unity is not installed in the default Hub path, set:

`export UNITY_EDITOR_PATH="/absolute/path/to/Unity.app/Contents/MacOS/Unity"`

Then you can run:

- `./scripts/test_editmode.sh`
- `./scripts/test_playmode.sh`
- `./scripts/build_osx.sh`
- `./scripts/build_win64.sh`
- `./scripts/run_osx.sh`

Local logs and test results are written under `artifacts/`.

### Verified local status on this Mac

These commands have already been verified locally with Unity `6000.4.0f1`:

- `./scripts/test_editmode.sh`
- `./scripts/test_playmode.sh`
- `./scripts/build_osx.sh`

The macOS build output is:

- `Builds/StandaloneOSX/unity-vibe-code.app`

To launch that macOS build from the shell:

- `./scripts/run_osx.sh`

If you want to run the built app directly without the helper script, the current executable path is:

- `Builds/StandaloneOSX/unity-vibe-code.app/Contents/MacOS/Game`

Windows `.exe` builds are wired up in the repo, but this Mac does not currently have the Unity Windows Build Support module installed, so `./scripts/build_win64.sh` is not locally verified yet.

### Troubleshooting local batch runs

If a local PlayMode batch run appears to hang before tests start, and the Unity log shows licensing messages such as `Unsupported protocol version '1.18.1'`, the usual fix is:

1. Close any open Unity editor for this project.
2. Stop the lingering `Unity.Licensing.Client` process.
3. Rerun the script.

That issue turned out to be a stale local licensing client, not a problem in the project itself.

### GitHub Actions setup

The workflow is designed around [GameCI](https://game.ci/). Before it can build or test, add these repository secrets in GitHub:

- `UNITY_EMAIL`
- `UNITY_PASSWORD`
- `UNITY_LICENSE` for a personal Unity license, or `UNITY_SERIAL` for a professional license

After secrets are set:

- pull requests run EditMode and PlayMode tests
- pushes to `main` run tests and produce a Windows build artifact
- manual runs are available through GitHub Actions `workflow_dispatch`

### Starter tests

The repo also includes starter Unity Test Framework coverage:

- [PlatformerEditModeTests.cs](/Users/david/vibe-with-ben/unity-vibe-code/Assets/Tests/EditMode/PlatformerEditModeTests.cs)
- [MainScenePlayModeTests.cs](/Users/david/vibe-with-ben/unity-vibe-code/Assets/Tests/PlayMode/MainScenePlayModeTests.cs)

These are intentionally simple smoke checks so we have a working pipeline first, then we can grow them into movement, collision, and gameplay regression tests.
