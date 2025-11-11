using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;

namespace Platformer.Mechanics
{
    public class PlayerController : KinematicObject
    {
        public int playerId = 1;

        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        public float maxSpeed = 7;
        public float jumpTakeOffSpeed = 7;

        public JumpState jumpState = JumpState.Grounded;
        bool stopJump;
        public Collider2D collider2d;
        public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        bool jump;
        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public Bounds Bounds => collider2d.bounds;

        [Header("Input (old Input Manager)")]
        public string horizontalAxis = "Horizontal";
        public string jumpButton = "Jump";

        // ✅ COYOTE TIME + JUMP BUFFER ONLY
        [Header("Jump Forgiveness")]
        [Tooltip("Time you can still jump after leaving ground.")]
        public float coyoteTime = 0.10f;

        [Tooltip("Allows pressing jump slightly before landing.")]
        public float jumpBufferTime = 0.10f;

        float coyoteTimer;
        float jumpBufferTimer;


        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

        protected override void Update()
        {
            // Update timers
            if (IsGrounded) coyoteTimer = coyoteTime;
            else coyoteTimer -= Time.deltaTime;

            jumpBufferTimer -= Time.deltaTime;

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

                if (jumpBufferTimer > 0 && (IsGrounded || coyoteTimer > 0))
                {
                    jumpState = JumpState.PrepareToJump;
                    jumpBufferTimer = 0;
                }
            }
            else move.x = 0;

            UpdateJumpState();
            base.Update();
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;

                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;

                case JumpState.InFlight:
                    if (IsGrounded && velocity.y <= 0)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;

                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump && (IsGrounded || coyoteTimer > 0))
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
                coyoteTimer = 0;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0) velocity.y *= model.jumpDeceleration;
            }

            if (move.x > 0.01f) spriteRenderer.flipX = false;
            else if (move.x < -0.01f) spriteRenderer.flipX = true;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}
