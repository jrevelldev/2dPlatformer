using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;

namespace Platformer.Mechanics
{
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        // ====== Inspector Key Bindings only (no legacy axes) ======
        [System.Serializable]
        public struct KeyPair
        {
            public KeyCode primary;
            public KeyCode secondary;

            public bool Held() => Input.GetKey(primary) || Input.GetKey(secondary);
            public bool Down() => Input.GetKeyDown(primary) || Input.GetKeyDown(secondary);
            public bool Up() => Input.GetKeyUp(primary) || Input.GetKeyUp(secondary);
        }

        [System.Serializable]
        public class KeyBindings
        {
            [Header("Left / Right")]
            public KeyPair left = new KeyPair { primary = KeyCode.A, secondary = KeyCode.LeftArrow };
            public KeyPair right = new KeyPair { primary = KeyCode.D, secondary = KeyCode.RightArrow };
            [Header("Jump")]
            public KeyPair jump = new KeyPair { primary = KeyCode.Space, secondary = KeyCode.JoystickButton0 };
        }

        [Header("Keys (Inspector)")]
        public KeyBindings keys = new KeyBindings();

        [Header("Identity / Audio")]
        public int playerId = 1;
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        [Header("Movement")]
        public float maxSpeed = 7f;              // horizontal speed
        public float jumpTakeOffSpeed = 7f;      // vertical impulse

        [Header("Animator / Components")]
        public Collider2D collider2d;
        public AudioSource audioSource;
        public Health health;
        internal Animator animator;
        SpriteRenderer spriteRenderer;

        [Header("Enable/Disable Control")]
        public bool controlEnabled = true;

        [Header("Jump Forgiveness")]
        [Tooltip("Time you can still jump after leaving ground.")]
        public float coyoteTime = 0.10f;
        [Tooltip("Allows pressing jump slightly before landing.")]
        public float jumpBufferTime = 0.10f;

        [Header("Physics (simple motor)")]
        public Rigidbody2D rb;
        public Transform groundCheck;
        public LayerMask groundLayer;
        public float groundCheckRadius = 0.2f;
        public float acceleration = 60f;
        public float deceleration = 60f;
        [Tooltip("Cuts jump height if you release the button on the way up.")]
        public float jumpCutMultiplier = 0.5f;

        // Jump state
        public JumpState jumpState = JumpState.Grounded;

        // Internals
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();
        public Bounds Bounds => collider2d ? collider2d.bounds : new Bounds(transform.position, Vector3.one);

        bool stopJump;            // when releasing jump
        bool jump;                // queued jump
        Vector2 move;             // horizontal input

        float coyoteTimer;
        float jumpBufferTimer;

        bool wasGrounded;
        bool _isGrounded;         // current grounded state
        public bool IsGrounded => _isGrounded;

        void Reset()
        {
            collider2d = GetComponent<Collider2D>();
            audioSource = GetComponent<AudioSource>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();

            rb = GetComponent<Rigidbody2D>();
            if (!rb) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;

            if (!collider2d) gameObject.AddComponent<BoxCollider2D>();
        }

        void Awake()
        {
            health = GetComponent<Health>();
            if (!audioSource) audioSource = GetComponent<AudioSource>();
            if (!collider2d) collider2d = GetComponent<Collider2D>();
            if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
            if (!animator) animator = GetComponent<Animator>();
            if (!rb) rb = GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            // Ground check
            _isGrounded = (groundCheck && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer));

            // Timers
            if (_isGrounded) coyoteTimer = coyoteTime; else coyoteTimer -= Time.deltaTime;
            jumpBufferTimer -= Time.deltaTime;

            // -------- INPUT (KeyCodes only) --------
            if (controlEnabled)
            {
                float horiz = 0f;
                bool leftHeld = keys.left.Held();
                bool rightHeld = keys.right.Held();
                if (leftHeld && !rightHeld) horiz = -1f;
                else if (rightHeld && !leftHeld) horiz = 1f;
                move.x = horiz;

                if (keys.jump.Down())
                    jumpBufferTimer = jumpBufferTime;

                if (keys.jump.Up())
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }

                // Buffer + Coyote → PrepareToJump
                if (jumpBufferTimer > 0f && (_isGrounded || coyoteTimer > 0f))
                {
                    jumpState = JumpState.PrepareToJump;
                    jumpBufferTimer = 0f;
                }
            }
            else
            {
                move.x = 0f;
            }

            UpdateJumpState();

            // Flip
            if (spriteRenderer)
            {
                if (move.x > 0.01f) spriteRenderer.flipX = false;
                else if (move.x < -0.01f) spriteRenderer.flipX = true;
            }

            // Animator
            if (animator)
            {
                animator.SetBool("grounded", _isGrounded);
                float vx = rb ? rb.linearVelocity.x : 0f; // user's preference: linearVelocity
                animator.SetFloat("velocityX", Mathf.Abs(vx) / Mathf.Max(0.01f, maxSpeed));
            }

            // Landed event
            if (_isGrounded && !wasGrounded && (jumpState == JumpState.InFlight || jumpState == JumpState.Jumping))
            {
                Schedule<PlayerLanded>().player = this;
                jumpState = JumpState.Landed;
            }
            wasGrounded = _isGrounded;
        }

        void FixedUpdate()
        {
            if (!rb) return;

            // Horizontal move with accel/decel
            float targetSpeed = move.x * maxSpeed;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
            float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

            // Jump cut on release
            if (stopJump)
            {
                stopJump = false;
                if (rb.linearVelocity.y > 0f)
                {
                    float cutFactor = Mathf.Clamp01(1f - jumpCutMultiplier);
                    rb.linearVelocity = new Vector2(
                        rb.linearVelocity.x,
                        rb.linearVelocity.y * model.jumpDeceleration * (cutFactor <= 0f ? 0.5f : cutFactor)
                    );
                }
            }

            // Apply jump
            if (jump)
            {
                jump = false;

                // clear downward velocity for clean jump
                float vy = rb.linearVelocity.y;
                if (vy < 0f) vy = 0f;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, vy);

                float impulse = jumpTakeOffSpeed * Mathf.Max(0f, model.jumpModifier);
                rb.AddForce(Vector2.up * impulse, ForceMode2D.Impulse);

                Schedule<PlayerJumped>().player = this;
            }
        }

        void UpdateJumpState()
        {
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    coyoteTimer = 0f;
                    break;

                case JumpState.Jumping:
                    if (!_isGrounded)
                        jumpState = JumpState.InFlight;
                    break;

                case JumpState.InFlight:
                    // landing handled on Update() rising edge
                    break;

                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;

                case JumpState.Grounded:
                    // idle
                    break;
            }
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }

        void OnDrawGizmosSelected()
        {
            if (groundCheck)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
        }

        // -------- Compatibility helpers (kept for external scripts) --------
        public Vector2 velocity
        {
            get => rb ? rb.linearVelocity : Vector2.zero;
            set { if (rb) rb.linearVelocity = value; }
        }

        public Vector2 targetVelocity
        {
            set
            {
                if (rb)
                    rb.linearVelocity = new Vector2(value.x, rb.linearVelocity.y);
            }
        }

        public void Bounce(float amount)
        {
            if (!rb) return;
            float vy = rb.linearVelocity.y;
            if (vy < 0f) vy = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, vy);
            rb.AddForce(Vector2.up * amount, ForceMode2D.Impulse);
        }

        public void Teleport(Vector3 position)
        {
            transform.position = position;
            if (rb) rb.linearVelocity = Vector2.zero;
        }
    }
}
