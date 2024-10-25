using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEditor;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    #region Misc. Variables
    // Components
    private Rigidbody2D rb;
    private CapsuleCollider2D col;

    // Masks
    LayerMask tileLayerMask;

    // Velocity Variabes
    private float horizontalVelocity = 0f;
    private float verticalVelocity = 0f;
    private float playerFacingDirection = 1f; // 1 indicates facing right, -1 indicates facing left
    #endregion

    #region Startup
    // Awake is called when the object is initialised
    void Awake()
    {
        col = gameObject.GetComponent<CapsuleCollider2D>();
        rb = gameObject.GetComponent<Rigidbody2D>();
        tileLayerMask = LayerMask.GetMask("Tile");
        groundedDistance = 0.1f; //col.size.y/2 + 0f;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Calculate the acceleration required to go from 0 to playerRunSpeed in playerAccelerationTime
        playerAcceleration = playerRunSpeed/playerAccelerationTime;
        // Calculate the acceleration required to go from playerRunSpeed to 0 in playerDecclerationTime
        playerDecceleration = playerRunSpeed/playerDeccelerationTime;
        // Calculate the additional acceleration required to go from playerRunSpeed to 0 while turning around in playerTurnAroundTime
        playerTurnAroundSpeed = Mathf.Abs(playerRunSpeed/playerTurnAroundTime - playerAcceleration);
        // Calculate ...
        playerAirFrictionDecceleration = (maxHorizontalVelocityAirborne-playerAirSpeed)/playerAirFrictionDeccelerationTime;
        // Calculate the acceleration required to go from playerRunSpeed to 0 in playerDecclerationAirborneTime
        playerDeccelerationAirborne = playerRunSpeed/playerDeccelerationAirborneTime;
        // Calculate the additional acceleration required to go from playerRunSpeed to 0 while turning around in playerTurnAroundTime
        playerTurnAroundAirborneSpeed = Mathf.Abs(playerRunSpeed/playerTurnAroundAirborneTime - playerAcceleration);
        dashVelocity = dashDistance/dashLength;
    }
    #endregion

    #region Functions
    // Returns the direction the player is moving in horizontally i.e. 1 = right, -1 = left, 0 = not moving
    float GetHorizontalDirection()
    {
        if (horizontalVelocity != 0) return (horizontalVelocity)/(Mathf.Abs(horizontalVelocity));
        else return 0;
    }

    // Returns the direction the player is moving in vertically i.e. 1 = up, -1 = down, 0 = not moving
    float GetVerticalDirection()
    {
        if (verticalVelocity != 0) return (verticalVelocity)/(Mathf.Abs(verticalVelocity));
        else return 0;
    }
    #endregion

    #region Inputs
    //      Inputs
    private float horizontalInputDirection = 0f;
    private float verticalInputDirection = 0f;
    // Gets the input from the player
    void GetInputs()
    {
        // Get left/right input direction from the player
        horizontalInputDirection = Input.GetAxisRaw("Horizontal");
        if (horizontalInputDirection != 0) playerFacingDirection = horizontalInputDirection;

        // Get up/down input direction from the player
        verticalInputDirection = Input.GetAxisRaw("Vertical");

        // Determine if the jump button has been pressed and is being held
        jumpPressed = Input.GetButtonDown("Jump");
        if (jumpPressed) timeJumpLastPressed = Time.time;
        jumpHeld = Input.GetButton("Jump");

        // Determine if the dash button was pressed
        dashPressed = Input.GetButtonDown("Dash");
        if (dashPressed) timeDashLastPressed = Time.time;
    }
    #endregion

    #region Collisions
    void CheckCollisions() {
        // Cat a capsule ray straight down
        RaycastHit2D groundCheck = Physics2D.CapsuleCast(col.bounds.center, col.size, col.direction, 0, Vector2.down, groundedDistance, tileLayerMask);
        // Check if a ground is hit
        if (groundCheck.collider != null) {
            playerOnGround = true;
            jumpAvailable = true;
            doubleJumpAvailable = true;
            dashCooldownLength = 0.5f;
            if (playerOnGround == false) {
                timeTouchedGround = Time.time;
            }
        } else {
            if (playerOnGround == true) {
                timeLeftGround = Time.time;
            }
            playerOnGround = false;
            dashCooldownLength = 0.1f;
        }

        // Cast a capsule ray straight up
         RaycastHit2D ceilingCheck = Physics2D.CapsuleCast(col.bounds.center, col.size, col.direction, 0, Vector2.up, groundedDistance, tileLayerMask);
        // Check if a ceiling is hit
        if (ceilingCheck.collider != null) {
            jumpEndedEarly = true;
            if (verticalVelocity > 0) {
                verticalVelocity = 0f;
            }
        }

        // Cast a ray from centre of the player to just outside
        RaycastHit2D wallCheckLeft = Physics2D.Raycast(transform.position, Vector2.left, col.bounds.extents.x + touchingWallDistance, tileLayerMask);
        RaycastHit2D wallCheckRight = Physics2D.Raycast(transform.position, Vector2.right, col.bounds.extents.x + touchingWallDistance, tileLayerMask);
        // Check if the player is touching a wall
        if (wallCheckLeft.collider != null) {
            playerTouchingWall = true;
            touchingWallDirection = -1f;
        }
        else if (wallCheckRight.collider != null) {
            playerTouchingWall = true;
            touchingWallDirection = 1f;
        }
        else {
            playerTouchingWall = false;
            touchingWallDirection = 0f;
        }
    }
    #endregion

    #region Gravity
    // Gravity fields
    private bool playerOnGround = false;
    private float groundedDistance;
    private float gravityStrength = -50f;
    private float maxGravityAmount = -250f;     
    private float fastFallStrength = 1f; // Percentage of gravity to add when holding down in the air
    private float maxFastFallAmount = 1.5f; // Multiplier to max gravity while fast falling
    private bool fastFallActive = false; 
    void Gravity()
    {
        if (dashActive) return;

        // Check if in air
        if (!playerOnGround) {
            // Acceleration of gravity
            verticalVelocity += gravityStrength * Time.deltaTime;

            // Fast fall
            if (verticalInputDirection == -1) {
                fastFallActive = true;
                verticalVelocity += fastFallStrength * gravityStrength * Time.deltaTime;
            } else {
                fastFallActive = false;
            }

            // Jump Increased Gravity
            if ((jumpActive && jumpEndedEarly) || (jumpActive && !jumpEndedEarly && (verticalVelocity < -jumpPeakSlowDownThreshold))) {
            verticalVelocity += jumpEndForceStrength * gravityStrength * Time.deltaTime;
            }
        } else {
            verticalVelocity = 0f;
        }
    }
    #endregion

    #region Run
    // Run Variables
    //      Grounded
    private float playerRunSpeed = 12f; // Maximum horizontal velocity in units per second
    private float playerAccelerationTime = 0.2f; // Time taken to reach maxiumum velocity in seconds
    private float playerAcceleration;
    private float playerDeccelerationTime = -0.1f; // Time taken to reach 0 from maximum velocity
    private float playerDecceleration;
    private float playerTurnAroundTime = -0.1f; // Time taken to reach 0 from maximum velocity while moving in opposite direction
    private float playerTurnAroundSpeed;
    //      Airborne
    private float playerAirSpeed = 15f;
    private float playerDeccelerationAirborneTime = -0.25f; // Time taken to reach 0 from maximum velocity in air
    private float playerDeccelerationAirborne;
    private float playerTurnAroundAirborneTime = -0.25f; // Time taken to reach 0 from maximum velocity while moving in opposite direction
    private float playerTurnAroundAirborneSpeed;
    private float playerAirFrictionDeccelerationTime = -10f; // Time taken to reach playerAirSpeed from max Air Speed
    private float playerAirFrictionDecceleration;
    void Run()
    {
        // Skip all if dash is active
        if (dashActive) return;

        float playerHorizontalDirection = GetHorizontalDirection();

        // Check if the player wants to move
        if (Mathf.Abs(horizontalInputDirection) > 0) {
            // Check if the player is moving slower than the max run speed
            if (Mathf.Abs(horizontalVelocity) < playerRunSpeed) {
                // Apply acceleration
                horizontalVelocity += horizontalInputDirection * playerAcceleration * Time.deltaTime;
            }
        } else {
            // Apply decceleration
            if (Mathf.Abs(horizontalVelocity) > 0.01f) {
                if (playerOnGround) horizontalVelocity += playerHorizontalDirection * playerDecceleration * Time.deltaTime;
                else horizontalVelocity += playerHorizontalDirection * playerDeccelerationAirborne * Time.deltaTime;
            } else {
                horizontalVelocity = 0f;
            }
        }

        // Check if the player is trying to change directions
        if ((horizontalInputDirection != playerHorizontalDirection) && (playerHorizontalDirection != 0)) {
            if (playerOnGround) horizontalVelocity += horizontalInputDirection * playerTurnAroundSpeed * Time.deltaTime;
            else horizontalVelocity += horizontalInputDirection * playerTurnAroundAirborneSpeed * Time.deltaTime;
        }

        // Slow the player down while airborne if going too fast
        /*
        if (!playerOnGround && Mathf.Abs(horizontalVelocity) > playerAirSpeed) {
            horizontalVelocity += playerHorizontalDirection * playerAirFrictionDecceleration * Time.deltaTime;
        }
        */
    }
    #endregion

    #region Jump
        // Jump fields

    //      Player State
    private bool jumpAvailable = false; // Can the player jump
    private bool jumpActive = false; // Is the player currently jumping
    //      Inputs
    private bool jumpPressed = false; // Was the jump button pressed this frame
    private bool jumpHeld = false; // Is the jump button being pressed this frame
    private float timeJumpLastUsed = 0f;
    //      Jump Force
    private float jumpStrength = 25f; // Force to apply as jump
    //      Jump Peak Anti Gravity
    private float jumpPeakSlowDownStrength = 0.5f; // What percentage of gravity to negate at jump peak
    private float jumpPeakSlowDownThreshold = 0.2f; // What velocity to determine as jump peak
    //      Jump End Early
    private bool jumpEndedEarly = false; // Was the jump button released mid jump
    private float jumpEndForceStrength = 1f; // What multiplier of gravity should be added after ending jump early
    //      Jump Buffer
    private float timeJumpLastPressed = 0f; // Time since the last jump was pressed
    private float jumpBufferLength = 0.05f; // Time in seconds that can pass between jump press in air and allowing a jump on floor
    //      Coyote Time
    private float timeLeftGround = 0f; // Time the player left the ground
    private float timeTouchedGround = 0f;
    private float coyoteTimeLenth = 0.1f; // Time a player is allowed to be airborne and still jump

    // Double jump
    private bool doubleJumpUnlocked = false;
    private bool doubleJumpAvailable = false;
    private float doubleJumpUsableTime = 0.1f;

    // Wall Slide and Wall Jump
    private bool playerTouchingWall = false; // Is the player touching a wall?
    private float touchingWallDistance = 0.1f;
    private float touchingWallDirection = 0f;
    private float wallTerminalVelocity = -5f; // Terminal velocity while touching a wall
    private float wallJumpCooldown = 0.1f; // Time required to refresh wall jump
    private float wallJumpVerticalStrength = 20f;
    private float wallJumpHorizontalStrength = 20f;

    // Dash Cancel
    private float dashCancelMultiplier = 1.2f;

    void Jump()
    {
        Debug.Log("Jump available: " + jumpAvailable + "Player on Ground: " + playerOnGround);
        
        // Check if the player can jump
        if (jumpAvailable && (playerOnGround || (Time.time - timeLeftGround) <= coyoteTimeLenth) && (Time.time - timeJumpLastPressed) <= jumpBufferLength) {

            // Change jump related variables
            timeJumpLastUsed = Time.time;
            jumpAvailable = false;
            jumpActive = true;
            jumpEndedEarly = false;

            // Cancel a dash
            if (dashActive) {
                dashActive = false;
                timeDashEnded = Time.time;
                horizontalVelocity *= dashCancelMultiplier;
            }

            // Add vertical velocity
            if (verticalVelocity < jumpStrength) {
                verticalVelocity = jumpStrength;
            }
        }

        // Check if the player can double jump
        if (doubleJumpUnlocked && jumpPressed && doubleJumpAvailable && (Time.time - timeLeftGround) >= doubleJumpUsableTime && !playerTouchingWall) {

            // Change jump related variables
            timeJumpLastUsed = Time.time;
            doubleJumpAvailable = false;
            jumpActive = true;
            jumpEndedEarly = false;

            // Cancel a dash
            if (dashActive) {
                dashActive = false;
                timeDashEnded = Time.time;
            }

            // Add vertical velocity
            if (verticalVelocity < jumpStrength) {
                verticalVelocity = jumpStrength;
            }

        }
    }

    void WallJump() {
        // Let player jump while touching a wall
        if ( !playerOnGround && playerTouchingWall && ((Time.time - timeJumpLastPressed) <= jumpBufferLength) && ((Time.time - timeJumpLastUsed) > wallJumpCooldown)) {

            // Change jump/wall jump related variables
            timeJumpLastUsed = Time.time;
            jumpActive = true;
            jumpEndedEarly = false;

            // Add vertical velocity
            verticalVelocity = wallJumpVerticalStrength;

            // Add horizontal velocity
            horizontalVelocity = -1f * touchingWallDirection * wallJumpHorizontalStrength;
        }
    }

    void HandleJump()
    {
        // Apply an upwards force while at the peak of jump
        if (jumpActive && !jumpEndedEarly && jumpHeld && Mathf.Abs(verticalVelocity) < jumpPeakSlowDownThreshold) {
            verticalVelocity += -1f * jumpPeakSlowDownStrength * gravityStrength * Time.deltaTime;
        }

        // Detetect if the player releases jump early
        if (jumpActive && !jumpEndedEarly && !jumpHeld) {
            jumpEndedEarly = true;
        }

        // Apply downwards force if the player releases the jump button early or after peak of jump
        // OR
        // If the player is still holding jump after peak add extra downwards force
        // This is the same force as if the jump had been ended early
        /*
        if ((jumpActive && jumpEndedEarly) || (jumpActive && !jumpEndedEarly && (verticalVelocity < -jumpPeakSlowDownThreshold))) {
            verticalVelocity += jumpEndForceStrength * gravityStrength * Time.deltaTime;
        }
        */
    }
    #endregion

    #region Dash
    // Dash Fields
    private bool dashAvailable = false;
    private float dashCooldownLength = 0.5f;
    private bool dashActive = false;
    private bool dashPressed = false;
    private float timeDashLastPressed = 0f;
    private float dashBufferLength = 0.1f;
    private float timeDashActivated = 0f;
    private float timeDashEnded = 0f;
    private float dashDirectionHorizontal = 1f; // Direction of the dash (1=right, 0 = none, -1=left)
    private float dashDirectionVertical = 0f; // Direction of the dash (1=up, 0 = none, -1=down)
    private float dashDistance = 5f; // Distance covered by a dash
    private float dashLength = 0.25f; // Time taken for the dash to complete
    private float dashVelocity; // Velocity of a dash
    private float dashVelocityMultiplier = 1.2f;
    
    void Dash()
    {
        // Start dash
        if ((Time.time - timeDashLastPressed) <= dashBufferLength && dashAvailable && !dashActive) {
            dashAvailable = false;
            dashActive = true;
            // Dash direction
            if (horizontalInputDirection == 0 && verticalInputDirection == 0) {
                dashDirectionHorizontal = playerFacingDirection;
            } else {
                dashDirectionHorizontal = horizontalInputDirection;
                dashDirectionVertical = verticalInputDirection;
            }
            timeDashActivated = Time.time;

            // Add velocity to player to get them to dash speed
            // These need to check if the velocity they are currently moving is lower than dash speed or in the opposite direction of dash speed, it if is then change it
            // If the player is moving faster than dash speed, then increase their velocity
            Vector2 dashDirectionNormalised = new Vector2(dashDirectionHorizontal, dashDirectionVertical).normalized * dashVelocity;

            // Moving slower than dash speed
            if (Mathf.Abs(horizontalVelocity) < Mathf.Abs(dashDirectionNormalised.x)) {
                horizontalVelocity = dashDirectionNormalised.x;
            }
            // Moving faster than dash speed
            else {
                // Moving in the same direction as dash
                if (GetHorizontalDirection() == dashDirectionHorizontal) {
                    horizontalVelocity *= dashVelocityMultiplier;
                } 
                // Moving in the opposite direction as dash
                else {
                    horizontalVelocity *= -1f * dashVelocityMultiplier;
                }
            }
            
            // Upwards dash
            if (dashDirectionVertical == 1f ) {
                // Slower than dash speed
                if (verticalVelocity < dashDirectionNormalised.y) {
                    verticalVelocity = dashDirectionNormalised.y;
                } 
                // Faster than dash speed
                else {
                    verticalVelocity *= dashVelocityMultiplier;
                }
            }
            else if (dashDirectionVertical == 0f) {
                verticalVelocity = 0f;
            }
            // Downwards dash
            else {
                verticalVelocity += dashDirectionNormalised.y;
            }
        }

        // Movement of the dash (OLD)
        if (dashActive) {
            // Dash movement
            /*
            Vector2 dashDirectionNormalised = new Vector2(dashDirectionHorizontal, dashDirectionVertical).normalized * dashVelocity;
            horizontalVelocity = dashDirectionNormalised.x;
            verticalVelocity = dashDirectionNormalised.y;
            */
        }

        // Stop the dash
        if (dashActive && (Time.time - timeDashActivated ) > dashLength) {
            dashActive = false;
            timeDashEnded = Time.time;
        }

        // Dash cooldown (refresh if it has been the cooldown length and sometime in that time the player has touched the ground or is on the ground)
        if (!dashActive && !dashAvailable && ((Time.time - timeDashEnded) > dashCooldownLength) && ((Time.time - timeLeftGround < dashCooldownLength) || playerOnGround)) {
            dashAvailable = true;
        }

    }
    #endregion

    #region Apply Velocities
    private float maxHorizontalVelocityGrounded = 10f;
    private float overMaxGroundedDeceleration = 100f;
    private float maxHorizontalVelocityAirborne = 50f;
    //private float overMaxAirborneDeceleration = 50f;
    void ApplyVelocities() {
        // Max horizontal Speed
        if (!dashActive) {
            float absoluteHorizontalVelocity = Mathf.Abs(horizontalVelocity);
            float playerHorizontalDirection = GetHorizontalDirection();
            // Grounded Max Velocity
            if (playerOnGround && absoluteHorizontalVelocity > maxHorizontalVelocityGrounded && (Time.time - timeTouchedGround) > 0.05f) {
                horizontalVelocity += -1f * playerHorizontalDirection * overMaxGroundedDeceleration * Time.deltaTime;
                if (absoluteHorizontalVelocity < maxHorizontalVelocityGrounded) horizontalVelocity = playerHorizontalDirection * maxHorizontalVelocityGrounded;
            }
            // Airbornoe Max Velocity
            /*
            else if (!playerOnGround && absoluteHorizontalVelocity > maxHorizontalVelocityAirborne) {
                horizontalVelocity += -1f * playerHorizontalDirection * overMaxAirborneDeceleration * Time.deltaTime;
                if (absoluteHorizontalVelocity < maxHorizontalVelocityAirborne) horizontalVelocity = playerHorizontalDirection * maxHorizontalVelocityAirborne; // Velocity Cap
            }
            */
        }
        // Max Vertical Velocities
        float maxGravityVelocity = maxGravityAmount;
        
        // Check for terminal velocity in air
        if (fastFallActive) {
            maxGravityVelocity *= maxFastFallAmount;
        }
        if (verticalVelocity < maxGravityVelocity) {
            verticalVelocity = maxGravityVelocity; // This should probably be a deccerlation instead so that when the player stops fast falling it will have a smoother transition
        }

        // Check if touching a wall and going faster than wall terminal
        if (playerTouchingWall && (verticalVelocity <= wallTerminalVelocity)) {
            verticalVelocity = wallTerminalVelocity;
        }

        // Update the rigidbody
        rb.velocity = new Vector2(horizontalVelocity, verticalVelocity);
    }
    #endregion

    #region Update
    // Update is called once per frame
    void Update()
    {
        GetInputs();
        CheckCollisions();
        Gravity();
        Run();
        Jump();
        WallJump();
        HandleJump();
        Dash();
        ApplyVelocities();
    }

    void FixedUpdate()
    {

    }
    #endregion
}
