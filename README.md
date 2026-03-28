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

## Build the starter platformer scene

After Unity finishes importing scripts:

1. Open the repo-root project in Unity Hub.
2. Open `Assets/Scenes/Main.unity` if it is not already open.
3. In the Unity menu bar, click `Vibe` -> `Build Platformer Starter Scene`.
4. Press Play.

The scene builder will create a simple placeholder level, a player with `Rigidbody2D` movement, and a camera follow setup.

## Default controls

- move: `A` / `D` or left / right arrow
- jump: `Space`

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
