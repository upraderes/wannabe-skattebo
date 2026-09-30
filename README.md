Create a mobile game where you start as a kid watched from behind, the goal is to navigate in a colorful backyard filled with obstacles. 
Title : "Wannabe Skattebo"
# Wannabe Skattebo
## Overview
"Wannabe Skattebo" is a mobile game that puts players in the shoes of a young child exploring a vibrant backyard filled with various obstacles. This is a kaizen game that encourages players to improve their skills. The goal is to became the number 44 in a NFL team. 
## Gameplay
Players control a young child character from a third-person perspective, navigating through a backyard environment. The backyard is filled with colorful obstacles such as trash cans, garden tools, playful animals, telephone posts and other interactive elements. The player must hit the obstacles to score points and avoid getting caught by the teacher or the neighbors. The game features multiple levels, each with increasing difficulty and new challenges to overcome. Players can collect power-ups and bonuses to enhance their equipments, abilities and score higher points.



## Development

**Stack:** Unity 6 LTS (C#), iOS first. Endless 3-lane runner, low-poly placeholder art built from code.

### Setup
1. Install **Unity Hub** → **Unity 6 LTS (6000.x)** with the **iOS Build Support** module.
2. In Unity Hub: *Add → Add project from disk* → pick this folder, and open it with Unity 6.
3. On first open, `ProjectSetup` creates `Assets/Scenes/Run_Backyard.unity`, adds it to Build Settings and applies the iOS player settings. You can re-run it from **Wannabe Skattebo → Setup Project**.
4. Press **Play**. Keyboard: arrows or WASD, Space to jump. Mouse drag acts as a swipe.

### Build for iOS
- Menu: **Wannabe Skattebo → Build iOS (Simulator | Device)**. This exports an Xcode project to `Builds/`.
- CLI:
  ```bash
  /Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath . -buildTarget iOS -executeMethod WannabeSkattebo.EditorTools.BuildScript.BuildiOS -simulator
  ```

### Tests
Use *Window → General → Test Runner → EditMode*, or:
```bash
Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml
```

### Code map (`Assets/_Project/Scripts`)
| Folder | What |
|---|---|
| `Core/` | `GameManager` (run loop, collisions, power-ups), `GameBootstrap` (auto-creates the game), `ScoreKeeper`, `Sfx` |
| `Player/` | `PlayerController` (lanes/jump/slide), `SwipeInput`, `PlayerStats` |
| `World/` | `TrackSpawner` (endless track, floating origin, row generator), `SpawnPool`, `ModelFactory` (procedural low-poly models) |
| `Obstacles/` | `Smashable`: every obstacle breaks when hit and scores points. Animals scatter away unharmed. |
| `Enemies/` | `Enemy`: neighbors, teachers, opposing players, police, agents. Getting caught ends the run. |
| `PowerUps/` | `Pickup`: footballs, Magnet, Helmet (absorbs one catch), Beast Mode, 2x Points |
| `Progression/` | `GameCatalog` (gear + career stages), `Economy`, `SaveSystem` |
| `UI/` | `GameUI`: menu, HUD, pause, results, gear shop and career screens |

### Core rules
- **Smash** everything: points × combo (up to x5 within 2 s). When an obstacle is tougher than your Power, you stumble (brief lane lock, combo reset).
- **Avoid** people: they can't be smashed. Helmet absorbs one catch; Beast Mode jukes through.
- **Kaizen loop:** your score is banked as points → buy gear (Cleats/Speed, Pads/Power, Gloves/Magnet, Juke/Agility) → beat your best → unlock the next career stage (Backyard → Youth League → High School → College → The League, #44).
