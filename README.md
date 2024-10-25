# fca_unity_platformer_player_controller
A basic platformer player controller for unity.

## Some Features
- Custom gravity physics
- Variable jump height
- Player controlled fast fall
- Teriminal velocity
- Wall jump
- Double jump (commented out)
- Dash
- Dash cancel
- Coyote time
- Input buffers

## Additional Features
- Dash cancel tech: horizontal velocity is multiplied when cancelling a dash
- Horizontal velocity preservation: air speed can be preserved by jumping within a few frames of touching the ground

## How to Use
- Player Game Object:
  - Rigidbody 2D
    - Body Type: Dynamic
    - Material: needs to be frictionless
    - Collision Detection: Continuous
    - Interpolate: Interpolate
  - Capsule Collider 2D
- Ground/Wall/Collisions
  - Objects to be collided with need to be on a layer named "Tile"
- Camera:
  - (optional) Size: 12
