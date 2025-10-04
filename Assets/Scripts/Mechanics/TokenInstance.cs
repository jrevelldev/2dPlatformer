using System; // ← keep
using UnityEngine;

namespace Platformer.Mechanics
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SimpleToken : MonoBehaviour
    {
        [Header("Scoring")]
        public int points = 1;

        [Header("Audio (optional)")]
        public AudioClip collectSfx;
        [Range(0f, 1f)] public float sfxVolume = 1f;

        public enum PickupBehavior { HideInstantly, PlayCollectedAnimationThenHide }

        [Header("Animation (optional)")]
        public PickupBehavior onPickup = PickupBehavior.HideInstantly;
        public float frameRate = 12f;
        public bool randomStartFrame = false;
        public Sprite[] idleFrames;
        public Sprite[] collectedFrames;

        // NEW: optional startup effect
        [Tooltip("If true, when this object enables it will play the collected animation backwards once, then switch to idle.")]
        public bool playCollectedBackwardsOnEnable = false;

        // NEW: fired when the token hides itself (before disabling)
        public event Action<SimpleToken> Hidden;

        // runtime
        SpriteRenderer sr;
        Collider2D col;
        Sprite[] current;
        int frame;
        float t;
        bool collected;

        // NEW: animation control
        int dir = 1;                        // +1 forward, -1 backward
        bool startupReverseActive = false;  // are we doing the "play backwards on enable" one-shot?

        void Reset()
        {
            var c = GetComponent<Collider2D>();
            if (c) c.isTrigger = true;
        }

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            col = GetComponent<Collider2D>();

            current = (idleFrames != null && idleFrames.Length > 0) ? idleFrames : Array.Empty<Sprite>();
            frame = (randomStartFrame && current.Length > 0) ? UnityEngine.Random.Range(0, current.Length) : 0;
            dir = 1;

            if (sr && current.Length > 0) sr.sprite = current[frame];
        }

        // NEW: set up the startup reverse play (runs on first enable and any re-enable)
        void OnEnable()
        {
            if (playCollectedBackwardsOnEnable && collectedFrames != null && collectedFrames.Length > 0)
            {
                current = collectedFrames;
                dir = -1;
                startupReverseActive = true;
                t = 0f;
                frame = current.Length - 1; // start at the last collected frame
                if (sr) sr.sprite = current[frame];
            }
            else
            {
                // ensure we're ready to idle if not doing the reverse intro
                current = (idleFrames != null && idleFrames.Length > 0) ? idleFrames : Array.Empty<Sprite>();
                dir = 1;
                startupReverseActive = false;

                frame = (randomStartFrame && current.Length > 0) ? UnityEngine.Random.Range(0, current.Length) : 0;
                t = 0f;
                if (sr && current.Length > 0) sr.sprite = current[frame];
            }
        }

        void Update()
        {
            if (current == null || current.Length <= 1) return;

            t += Time.deltaTime;
            float step = 1f / Mathf.Max(0.0001f, frameRate); // time per frame (rate always positive)
            if (t >= step)
            {
                t -= step;
                frame += dir;

                if (dir > 0)
                {
                    // forward playback
                    if (frame >= current.Length)
                    {
                        if (collected && onPickup == PickupBehavior.PlayCollectedAnimationThenHide)
                        {
                            HideAndNotify();
                            return;
                        }
                        frame = 0; // loop (idle)
                    }
                }
                else
                {
                    // backward playback
                    if (frame < 0)
                    {
                        if (startupReverseActive)
                        {
                            // finished the reverse intro → switch to idle loop
                            current = (idleFrames != null && idleFrames.Length > 0) ? idleFrames : Array.Empty<Sprite>();
                            dir = 1;
                            startupReverseActive = false;
                            frame = (current.Length > 0) ? 0 : 0;
                        }
                        else
                        {
                            // if ever playing something else backwards, just loop backwards
                            frame = current.Length - 1;
                        }
                    }
                }

                if (sr && frame >= 0 && frame < current.Length)
                    sr.sprite = current[frame];
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            HandlePickup(player);
        }

        void HandlePickup(PlayerController player)
        {
            collected = true;

            // stop future triggers immediately
            if (col) col.enabled = false;

            // score
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.Add(player.playerId, points);
            else
                Debug.LogWarning("[SimpleToken] ScoreManager.Instance not found. No score added.");

            // SFX (prefer player's audio source)
            if (collectSfx != null)
            {
                if (player.audioSource != null) player.audioSource.PlayOneShot(collectSfx, sfxVolume);
                else AudioSource.PlayClipAtPoint(collectSfx, transform.position, sfxVolume);
            }

            // visuals on pickup
            if (onPickup == PickupBehavior.PlayCollectedAnimationThenHide &&
                collectedFrames != null && collectedFrames.Length > 0)
            {
                current = collectedFrames;
                dir = 1;            // CHANGED: ensure forward when collected
                frame = 0;
                t = 0f;
                startupReverseActive = false;
                if (sr) sr.sprite = current[0];
            }
            else
            {
                HideAndNotify();
            }
        }

        // centralize hide + event
        void HideAndNotify()
        {
            Hidden?.Invoke(this);      // notify spawner first
            gameObject.SetActive(false); // then actually hide
        }

        // let a spawner reset the token before re-enabling
        public void ResetForRespawn()
        {
            collected = false;
            if (col)
            {
                col.enabled = true;
                col.isTrigger = true;
            }

            current = (idleFrames != null && idleFrames.Length > 0) ? idleFrames : Array.Empty<Sprite>();
            frame = (randomStartFrame && current.Length > 0) ? UnityEngine.Random.Range(0, current.Length) : 0;
            t = 0f;
            dir = 1;
            startupReverseActive = false;

            if (sr && current.Length > 0) sr.sprite = current[frame];
        }
    }
}
