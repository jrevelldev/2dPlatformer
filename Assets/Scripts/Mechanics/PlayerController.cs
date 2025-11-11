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
        [Header("Identity / Audio")]
        public int playerId = 1;
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        [Header("Movement (legacy names kept)")]
        public float maxSpeed = 7f;              // horitzontal
        public float jumpTakeOffSpeed = 7f;      // impuls vertical

        [Header("Animator / Components")]
        public Collider2D collider2d;
        public AudioSource audioSource;
        public Health health;
        internal Animator animator;
        SpriteRenderer spriteRenderer;

        [Header("Enable/Disable Control")]
        public bool controlEnabled = true;

        [Header("Input (old Input Manager)")]
        public string horizontalAxis = "Horizontal";
        public string jumpButton = "Jump";

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
        [Tooltip("Retalla l'alçada si deixes el botó durant la pujada")]
        public float jumpCutMultiplier = 0.5f;

        // Estat de salt (manté el teu enum i flux)
        public JumpState jumpState = JumpState.Grounded;

        // Interns
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();
        public Bounds Bounds => collider2d ? collider2d.bounds : new Bounds(transform.position, Vector3.one);

        bool stopJump;            // en deixar anar el botó
        bool jump;                // sol·licitud de salt
        Vector2 move;             // entrada horitzontal

        float coyoteTimer;
        float jumpBufferTimer;

        bool wasGrounded;
        bool _isGrounded;         // estat actual de terra

        // --- COMPAT: algunes lògiques poden llegir si estem a terra
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
            // --- Ground check simple
            _isGrounded = (groundCheck && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer));

            // Timers
            if (_isGrounded) coyoteTimer = coyoteTime; else coyoteTimer -= Time.deltaTime;
            jumpBufferTimer -= Time.deltaTime;

            // Input
            if (controlEnabled)
            {
                move.x = Input.GetAxis(horizontalAxis);

                if (Input.GetButtonDown(jumpButton))
                    jumpBufferTimer = jumpBufferTime;

                if (Input.GetButtonUp(jumpButton))
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

            // Animator (paràmetres originals)
            if (animator)
            {
                animator.SetBool("grounded", _isGrounded);
                float vx = rb ? rb.linearVelocity.x : 0f;
                animator.SetFloat("velocityX", Mathf.Abs(vx) / Mathf.Max(0.01f, maxSpeed));
            }

            // Event d’aterratge (equivalent Microgame)
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

            // Moviment horitzontal amb accel/decel
            float targetSpeed = move.x * maxSpeed;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
            float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

            // Jump cut en deixar el botó
            if (stopJump)
            {
                stopJump = false;
                if (rb.linearVelocity.y > 0f)
                {
                    // respecta model.jumpDeceleration
                    float cutFactor = Mathf.Clamp01(1f - jumpCutMultiplier);
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * model.jumpDeceleration * (cutFactor <= 0f ? 0.5f : cutFactor));
                }
            }

            // Aplicació del salt
            if (jump)
            {
                jump = false;

                // neteja caiguda per un salt net
                float vy = rb.linearVelocity.y;
                if (vy < 0f) vy = 0f;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, vy);

                float impulse = jumpTakeOffSpeed * Mathf.Max(0f, model.jumpModifier);
                rb.AddForce(Vector2.up * impulse, ForceMode2D.Impulse);

                // Event de salt (moment equiparable)
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
                    // l'aterratge es resol a Update (flanc)
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

        // -----------------------------------------------------------
        // COMPATIBILITAT AMB KinematicObject (Platformer Microgame)
        // -----------------------------------------------------------

        // Propietat 'velocity' que altres scripts llegeixen/escriuen
        public Vector2 velocity
        {
            get => rb ? rb.linearVelocity : Vector2.zero;
            set { if (rb) rb.linearVelocity = value; }
        }

        // Setter 'targetVelocity' (molts scripts l'usen per moure el player)
        public Vector2 targetVelocity
        {
            set
            {
                if (rb)
                    rb.linearVelocity = new Vector2(value.x, rb.linearVelocity.y);
            }
        }

        // Rebot vertical (JumpPads, enemics, etc.)
        public void Bounce(float amount)
        {
            if (!rb) return;
            float vy = rb.linearVelocity.y;
            if (vy < 0f) vy = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, vy);
            rb.AddForce(Vector2.up * amount, ForceMode2D.Impulse);
        }

        // Teletransport (spawn / checkpoints)
        public void Teleport(Vector3 position)
        {
            transform.position = position;
            if (rb) rb.linearVelocity = Vector2.zero;
        }
    }
}
