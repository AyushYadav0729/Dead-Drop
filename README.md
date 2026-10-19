# DEAD DROP

### 2D Top-Down Stealth Shooter --- Unity Project

> **Dead Drop** is a top-down stealth shooter in which a private agent
> infiltrates secure facilities, steals a classified file, and escapes
> while armed guards and security systems respond to the intrusion.

------------------------------------------------------------------------

## Table of Contents

-   [Project Overview](#project-overview)
-   [Story and Objective](#story-and-objective)
-   [Core Gameplay Loop](#core-gameplay-loop)
-   [Features and Mechanics](#features-and-mechanics)
-   [Controls](#controls)
-   [Levels](#levels)
-   [Enemies and Security Systems](#enemies-and-security-systems)
-   [Weapons and Combat](#weapons-and-combat)
-   [HUD and Menus](#hud-and-menus)
-   [Visual Direction](#visual-direction)
-   [Technology and Requirements](#technology-and-requirements)
-   [Project Structure](#project-structure)
-   [Important Scripts and
    Responsibilities](#important-scripts-and-responsibilities)
-   [Running the Project](#running-the-project)
-   [Unity Setup and Troubleshooting](#unity-setup-and-troubleshooting)
-   [Known Development Notes](#known-development-notes)
-   [Future Improvements](#future-improvements)
-   [Credits and Asset Notes](#credits-and-asset-notes)

------------------------------------------------------------------------

## Project Overview

**Dead Drop** is a Windows-focused, 2D top-down stealth shooter
developed in Unity using the **Universal Render Pipeline (URP) 2D**
workflow.

The player controls a private agent who must enter a guarded facility,
locate and steal a classified file, and reach an escape point. The
player can avoid detection by moving carefully, using cover, hiding, and
planning a route---or use weapons and movement abilities when stealth
breaks down.

The game is designed around readable top-down environments, clear enemy
awareness indicators, compact tactical UI, and two progressively
challenging missions.

### Project at a glance

  Item                      Description
  ------------------------- --------------------------------
  Title                     Dead Drop
  Genre                     2D top-down stealth shooter
  Engine                    Unity
  Rendering                 Universal Render Pipeline (2D)
  Target platform           Windows PC
  Reference aspect ratio    16:9
  Reference UI resolution   1920 × 1080
  Main menu scene           `MainMenu`
  Mission scenes            `Level1`, `Level2`

------------------------------------------------------------------------

## Story and Objective

You are a private agent sent to infiltrate secure buildings and retrieve
sensitive information. Guards patrol the facilities, cameras monitor
corridors, and security systems restrict access to important areas.

Each mission has two main goals:

1.  **Steal the classified file.**
2.  **Escape the facility.**

Taking the file triggers the alarm. Guards become more aggressive and
converge on the location where the file was taken, but they do not
automatically know the player's exact position. The player can still use
cover, break line of sight, hide, and choose an alternate escape route.

------------------------------------------------------------------------

## Core Gameplay Loop

1.  **Infiltrate:** Enter the facility and assess patrols, cameras,
    cover, and possible routes.
2.  **Avoid detection:** Walk quietly, hide in suitable objects, and use
    the environment to break line of sight.
3.  **Retrieve the file:** Collect the objective in the office or unlock
    the locker in the vault.
4.  **Survive the alarm:** Guards move to a higher-alert state and
    investigate the objective location.
5.  **Escape:** Reach the marked exit while avoiding or fighting
    threats.
6.  **Review the result:** Mission results can include time, kills,
    stealth-related bonuses, and total score.

The intended design includes a no-kill stealth route in each level,
while also allowing a louder combat-oriented approach.

------------------------------------------------------------------------

## Features and Mechanics

The project is designed around the following gameplay systems. The exact
availability of individual mechanics may depend on the current Unity
scene and implementation state.

-   **Top-down movement:** Move through rooms and corridors from a
    top-down perspective.
-   **Walking and running:** Walking is intended to be quiet; running
    creates noise and can attract attention.
-   **Dash:** A quick movement burst with a five-second cooldown.
-   **Weapon switching:** Switch between a pistol and a rifle using the
    number keys.
-   **Aiming and shooting:** Aim with the mouse and fire with the left
    mouse button.
-   **Laser sight:** Toggle an alternate laser-sight mode.
-   **Reloading:** Reload the currently equipped weapon.
-   **Hiding:** Use crates and almirahs as hiding places.
-   **Lockpicking:** Interact with a secured locker or lock where
    supported by the mission.
-   **Enemy awareness:** Guards use awareness states and visible vision
    cones.
-   **Security cameras:** Cameras monitor areas and rotate or sweep
    where configured.
-   **Laser tripwires:** Security lasers create hazardous or restricted
    routes.
-   **Alarm response:** Stealing the file triggers a higher-alert phase.
-   **Mission scoring:** Results can account for time, kills, stealth
    bonuses, and escape bonuses.
-   **Pause, restart, and scene navigation:** Pause the mission, restart
    the current level, or return to the main menu.
-   **Mission completion and failure:** The game displays a result state
    when the mission is completed or the player fails.

------------------------------------------------------------------------

## Controls

The following is the intended control scheme. If a control does not
respond, check the relevant component bindings and input configuration
in the Unity project.

  Input                   Action
  ----------------------- -----------------------------
  `W` / `A` / `S` / `D`   Move
  `Shift`                 Run (noisier movement)
  `Space`                 Dash (five-second cooldown)
  Mouse movement          Aim
  Left mouse button       Fire
  Right mouse button      Toggle laser sight
  `R`                     Reload
  `1` / `2`               Switch weapon slot
  `E`                     Hide or interact
  `Q`                     Cancel lockpick interaction
  `Esc`                   Pause or resume

**Note:** The weapon-name HUD label has been under debugging. The actual
weapon and ammunition display were reported to switch correctly, while
the separate gun-name label did not update reliably. This is a known UI
issue to verify in the current project version.

------------------------------------------------------------------------

## Levels

### Level 1 --- Corporate Office

The introductory mission teaches the player to read patrols, use cover,
and approach the objective without being detected.

**Intended level contents:**

-   Four standard guards (grunts)
-   One security camera
-   One always-on laser tripwire
-   Classified file placed on a desk
-   Five hiding spots made from crates and almirahs
-   Two ammunition pickups
-   One medkit
-   An escape point on the opposite side from the entrance

**Intended layout:** A lobby and starting area lead into a main corridor
and an open-plan office. The file is located in an executive office,
while the exit is in a service area on the other side of the level. A
laser-guarded side route offers a shortcut at the cost of stealth.

**Gameplay focus:** Learning movement, awareness, cover, and the
retrieve-then-escape objective loop.

### Level 2 --- Secure Data Vault

The second mission is a more heavily secured facility that tests the
player's use of stealth, timing, and route planning.

**Intended level contents:**

-   Three standard guards (grunts)
-   Two riflemen
-   Three security cameras, including overlapping coverage along a
    corridor
-   One blinking laser near the exit
-   Classified file stored inside a locker in the vault core
-   Three hiding spots
-   Three ammunition pickups
-   One medkit
-   An exit in the same hall as the starting area, behind the blinking
    laser

**Visual direction:** Colder metal floors, darker steel walls, and
restrained teal accents distinguish the vault from the office.

**Gameplay focus:** More complex security coverage, a secured objective,
and a challenging escape route.

> These contents describe the level concept brief. Check the current
> Unity scenes to confirm which objects and encounters are already
> placed and functional.

------------------------------------------------------------------------

## Enemies and Security Systems

### Standard guards (grunts)

Standard guards patrol the facility and can detect, investigate, and
engage the player according to their awareness state and configured
behaviour.

### Riflemen

Riflemen are intended as a heavier enemy type with a different
silhouette and a rifle. They are used in the Secure Data Vault mission.

### Vision cones and awareness

Enemy vision cones communicate awareness. The intended color progression
is:

-   **Green:** Low or normal awareness
-   **Yellow:** Suspicious / investigating
-   **Red:** High alert

The game engine is responsible for drawing these signals; they should
not be baked into the character art.

### Alarm system

Taking the objective triggers an alarm. Guards move toward the location
where the file was taken, giving the player a chance to hide or escape
through another route rather than making the player's exact location
automatically known.

### Cameras and laser tripwires

-   **Security cameras** monitor corridors and other areas according to
    their configured sweep or rotation.
-   **Laser tripwires** restrict routes and may be always active or
    blinking depending on the level design.
-   Closed doors are intended to block both sight and bullets, allowing
    players to break line of sight.

------------------------------------------------------------------------

## Weapons and Combat

The player has two intended weapon options:

### Pistol

The standard weapon for controlled, direct engagements.

### Rifle

A longer-range weapon option. The design brief specifies that rifle
bullets can bounce once.

### Weapon HUD

The ammunition display should reflect the currently equipped weapon's
magazine and reserve ammunition. The separate weapon-name label should
read the equipped weapon's `WeaponData` display name.

The current development note is that weapon switching and ammunition
updates have worked, but the separate weapon-name label has remained
stuck on the pistol name in testing. This should be treated as a HUD
binding/update issue until verified, rather than proof that weapon
switching itself is broken.

------------------------------------------------------------------------

## HUD and Menus

### In-game HUD

The intended HUD communicates:

-   Current health and maximum health
-   Current weapon and ammunition
-   Reload progress
-   Mission objective
-   Score and elapsed time
-   Alarm status and hints
-   Dash cooldown and other status indicators where implemented

The newer Canvas-based HUD is intended to handle the health and
ammunition display. A legacy `DebugHUD` script has also been used for
gameplay status and mission panels, including pause, failure, and
completion states. Avoid leaving duplicate UI elements enabled if they
show the same information.

### Main menu

The main menu provides the entry point into the game and is intended to
include:

-   Game title / logo
-   Play
-   Controls
-   Quit

### Pause menu

The pause screen is intended to provide:

-   Resume
-   Restart Level
-   Main Menu

### Mission failure

The failure screen is intended to provide:

-   Retry Mission
-   Main Menu

### Mission completion

A completion screen can display the mission score breakdown and provide
navigation options such as Next Level, Retry Level, and Main Menu.

For the final mission, the intended ending message is **"THANKS FOR
PLAYING"**, accompanied by an operation-complete message and final
score. Confirm the final-level configuration and completion state in
Unity when testing this screen.

------------------------------------------------------------------------

## Visual Direction

Dead Drop uses a clean, readable, flat-vector art style with a tense
spy-thriller mood.

### Color logic

  Role                          Color
  ----------------------------- -----------
  Deep UI background            `#0B101A`
  UI panel                      `#151D2B`
  Raised UI panel               `#1F2A3D`
  Primary UI text               `#E8EEF7`
  Muted UI text                 `#8A97AD`
  Player and objective accent   `#3DD6F5`
  Deep player accent            `#1B8FB0`
  Guard accent                  `#8F6BFF`
  Guard base                    `#2A2F3A`
  Player bullets                `#BFF6FF`
  Enemy bullets                 `#FF9A3D`
  Alarm / danger UI             `#FF3B30`

The world environment should stay cool, dark, and relatively neutral so
that the player, objectives, bullets, and awareness indicators remain
easy to see. Large saturated red, yellow, and green areas should be
avoided in environmental art because those colors carry gameplay
meaning.

### Sprite and environment standards

The art brief specifies:

-   World sprites use a **512 × 512 px canvas**, or an exact multiple.
-   Sprites that move or rotate face right at zero rotation.
-   Character pivots sit at the canvas centre.
-   Rotating sprites should not contain baked directional shadows.
-   Tiles should be seamless and full-bleed.
-   UI reference frames use **1920 × 1080**.
-   Export art as 1× PNG with transparent backgrounds where appropriate.
-   UI panels and buttons should support 9-slicing where needed.

### Typography

The art brief recommends:

-   **Rajdhani** for headings and buttons
-   **Share Tech Mono** for numbers, ammo, score, and timers

Use appropriate fallback fonts if these are not installed or included
with the project.

------------------------------------------------------------------------

## Technology and Requirements

### Technology

-   **Unity** --- game engine
-   **URP 2D** --- rendering pipeline
-   **C#** --- gameplay and UI scripts
-   **TextMeshPro** --- text rendering in the Canvas HUD
-   **Unity UI** --- sliders and other HUD controls

### Requirements

-   A compatible Unity Editor version that can open the project
-   The project's required Unity packages
-   Windows PC for the intended target build
-   Keyboard and mouse

The exact Unity Editor version and package versions should be taken from
`ProjectSettings/ProjectVersion.txt` and the package manifest in the
project, rather than assumed from this README.

------------------------------------------------------------------------

## Project Structure

The current project uses the `Assets/_Project/` directory for its main
game assets and scripts.

``` text
Assets/
└── _Project/
    ├── Scenes/
    │   ├── MainMenu.unity
    │   ├── Level1.unity
    │   └── Level2.unity
    └── Scripts/
        ├── UI/
        │   ├── CanvasHUD.cs
        │   └── DebugHUD.cs
        ├── Menu/
        │   └── MainMenuController.cs
        └── Mission/
            ├── GameManager.cs
            └── RunStats.cs
```

This is a high-level map of the known project structure, not a complete
inventory. Other folders and scripts may exist for player movement,
weapons, enemies, audio, security systems, interactions, and
level-specific behaviour.

------------------------------------------------------------------------

## Important Scripts and Responsibilities

  -----------------------------------------------------------------------
  Script                              Responsibility
  ----------------------------------- -----------------------------------
  `CanvasHUD.cs`                      Updates Canvas-based health,
                                      ammunition, reload, and weapon-name
                                      UI.

  `DebugHUD.cs`                       Legacy/debug gameplay display and
                                      mission-state panels such as pause,
                                      failure, and completion.

  `MainMenuController.cs`             Handles main-menu interactions.

  `GameManager.cs`                    Manages mission state, pausing,
                                      completion/failure, score flow, and
                                      scene navigation.

  `RunStats.cs`                       Holds or tracks run-level
                                      statistics used for mission
                                      results.

  `PlayerController.cs`               Player movement and player-state
                                      access, including the currently
                                      equipped weapon.

  `Health.cs`                         Health state and health-related
                                      events.

  Weapon-related scripts              Current weapon state, ammunition,
                                      reload status, and weapon data.
  -----------------------------------------------------------------------

Names and responsibilities above reflect the project context available
during preparation. Inspect the actual scripts in the current project
before making architectural changes.

------------------------------------------------------------------------

## Running the Project

1.  Open the project folder in Unity Hub.
2.  Open it using the Unity Editor version recorded in
    `ProjectSettings/ProjectVersion.txt`.
3.  Wait for Unity to import assets and compile scripts.
4.  Open `Assets/_Project/Scenes/MainMenu.unity`.
5.  Confirm that the required scenes are included in the project's build
    configuration.
6.  Press **Play** to test the main menu and begin a mission.
7.  Test Level 1, Level 2, pause/resume, restart, mission failure, and
    mission completion.

### Recommended test checklist

-   [ ] Main menu opens without errors.
-   [ ] Play loads the intended first level.
-   [ ] Player movement and aiming work.
-   [ ] Walking, running, and dash behave as expected.
-   [ ] Pistol fires and reloads.
-   [ ] Pressing `2` equips the rifle and changes ammunition values.
-   [ ] Weapon-name label updates to the equipped weapon.
-   [ ] Guards detect and respond to the player.
-   [ ] Objective pickup triggers the alarm.
-   [ ] Hiding and lockpicking work where configured.
-   [ ] Exit triggers mission completion.
-   [ ] Pause/resume works with `Esc`.
-   [ ] Restart returns to the current level.
-   [ ] Main Menu returns to `MainMenu`.
-   [ ] The final level displays the intended ending screen.

------------------------------------------------------------------------

## Unity Setup and Troubleshooting

### Scenes do not load

-   Confirm the scene name in `GameManager` matches the actual scene
    name.
-   Add the required scenes to the build configuration.
-   Check the Unity Console for missing-scene or build-index errors.

### Pause menu does not appear

-   Confirm the `GameManager` exists and is active in the scene.
-   Check that the pause input is enabled.
-   Ensure the pause UI is assigned or drawn by the expected script.
-   Check for more than one manager or UI script controlling pause
    state.

### Duplicate HUD information

If the Canvas HUD and the legacy debug HUD both display health or
ammunition, disable the duplicate display elements. Do not remove a
legacy script until confirming that it is not still responsible for
pause, failure, objective feedback, or mission-completion panels.

### Weapon name stays on "PISTOL"

The current known symptom is that the rifle and ammunition display can
change while the separate weapon-name label remains stuck on the pistol
name.

Check the following:

1.  On the GameObject containing `CanvasHUD`, make sure **Weapon Name
    Text** references the actual visible TextMeshPro label.
2.  Make sure the visible label is not an old or duplicate UI object.
3.  Confirm the `Player` field references the same player instance whose
    weapon and ammunition change.
4.  Confirm the equipped weapon's `WeaponData.displayName` is set
    correctly for both weapon assets.
5.  Check the Console for `NullReferenceException` errors from
    `CanvasHUD.Update()`.
6.  Search the project for any other script assigning `"PISTOL"` to the
    same label.

Use the Console's full stack trace and the Inspector references to
identify the actual null field; do not assume the player or weapon
object is the cause without checking.

### Final "THANKS FOR PLAYING" screen does not appear

-   Confirm the mission reaches the completed state.
-   Confirm the correct final-level scene is being played.
-   If the manager uses an empty `Next Scene Name` to identify the final
    mission, make sure that field is empty in the final level.
-   Check that the completion panel is enabled and that the correct
    version of `DebugHUD.cs` is installed.
-   Check the Console for compile errors or exceptions.

### TextMeshPro text is missing or incorrect

-   Ensure the relevant field references a `TMP_Text` / TextMeshPro UI
    component.
-   Confirm the assigned object is active and visible.
-   Check the text's color, scale, sorting order, and Canvas hierarchy.
-   Make sure no second script overwrites the same text.

------------------------------------------------------------------------

## Known Development Notes

These are issues or configuration details to verify in the current
working copy:

-   The weapon-name label has not reliably changed from "PISTOL" even
    though the rifle and ammunition display change.
-   A `NullReferenceException` was reported in `CanvasHUD.Update()` at
    line 39 during HUD debugging. The exact null reference depends on
    the version of the script and Inspector assignments in the current
    project.
-   The legacy `DebugHUD` may still be responsible for pause, mission
    failure, and completion panels even when the Canvas HUD handles
    health and ammunition.
-   The final mission should be configured so the game can recognise
    that there is no next level and show the final ending.
-   Verify all scene references, UI assignments, and build settings
    after replacing scripts or importing updated files.

------------------------------------------------------------------------

## Future Improvements

Possible improvements, depending on project scope and time:

-   Finish and verify the weapon-name HUD binding.
-   Polish the final ending and pause menus for a consistent visual
    style.
-   Add more feedback for weapon switching, reloading, damage, and
    objective collection.
-   Refine guard patrols and awareness feedback.
-   Improve camera and laser warning cues.
-   Add sound and visual effects for shots, impacts, dash, and alarms.
-   Test all stealth and combat routes for fairness and clarity.
-   Add a final playtest checklist and capture screenshots or a short
    gameplay video for the project presentation.

------------------------------------------------------------------------

## Credits and Asset Notes

Dead Drop is a working project title. The art direction, palette, sprite
dimensions, UI concepts, and level-design requirements in this README
are based on the project's **Dead Drop: Art, UI & Level Concept Brief**.

Use original or properly licensed assets and fonts. The visual
references in the brief are intended to communicate mood and genre only;
do not copy copyrighted artwork, logos, characters, or other protected
assets.

If an asset or feature is unfinished, document it clearly and use a
placeholder rather than implying it is complete.

------------------------------------------------------------------------

## Project Status Disclaimer

This README combines the game's design brief with implementation context
from the development conversation. It describes the intended game and
the known Unity architecture, but it does **not** certify that every
feature listed is fully implemented or tested in the current build.
Verify functionality in the Unity Editor before using this document as a
final release specification.
