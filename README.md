# Spaceship Game

**Spaceship Game** is a 2D arcade-style game built in Unity where you pilot a spaceship through space, dodging meteors (boulders) and collecting energy orbs to survive. Progress through three levels, each with a unique spaceship model designed in Blender.

## About the Game

Navigate a spaceship in a top-down 2D view, avoiding boulders and collecting orbs to earn points and advance through three increasingly difficult levels. Originally planned as a 3D game, it was switched to 2D using Unity’s 2D tools for timely completion. Features include:

- Three unique spaceship models (Blender).

## Game Rules

- **Objective**: Reach 365 points to complete all levels (165 for Level 2, 265 for Level 3).
- **Controls**:
  - Move: `W`, `A`, `S`, `D` or arrow keys.
  - Shoot single rocket: `Spacebar`.
  - Shoot all rockets: `T`.
  - Collect orbs: `C`.
- **Gameplay**:
  - Destroy boulders (small: 2.5 points, medium: 5, large: 10) and collect orbs (10 points).
  - Energy: Starts at 100%; boulders deduct 40% (Levels 1-2) or 30% (Level 3); orbs restore 20%. Game ends at 0%.
  - Levels: Unique spaceships; boulders move faster (1.3x Level 2, 1.6x Level 3).
- **Game Over**: Energy reaches 0%, miss too many boulders (10/6/4 in Levels 1/2/3), or get hit too often (5/2 times in Levels 1-2/3).
- **Ship Selection**: After 365 points, replay with any spaceship (0 points, 50% energy).

## Play Now

Play the game here: [Spaceship Game](https://m-byteforge.github.io/Spaceship/)

