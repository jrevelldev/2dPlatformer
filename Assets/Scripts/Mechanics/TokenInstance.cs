using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// One-file token: animates (optionally), scores for the touching player, plays SFX,
    /// and disables itself. No Simulation.Tick or TokenController required.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SimpleToken : MonoBehaviour
    {
        [Header("Scoring")]
        public int points = 1; // points awarded to the player that touches it

        [Header("Audio (optional)")]
        public AudioClip collectSfx;
        [Range(0f, 1f)] public float sfxVolume = 1f;

        public enum PickupBehavior { HideInstantly, PlayCollectedAnimationThenHide }

        [Header("Animation (optional)")]
        public PickupBehavior onPickup = PickupBehavior.HideInstantly;
        [Tooltip("Frames per second for sprite animation.")]
        public float frameRate = 12f;
        [Tooltip("If true, idle animation starts at a random frame.")]
        public bool randomStartFrame = false;
        [Tooltip("Idle frames (looped while not collected).")]
        public Sprite[] idleFrames;
        [Tooltip("Collected frames (played once, then the token hides).")]
        public Sprite[] collectedFrames;

        // runtime
        SpriteRenderer sr;
        Collider2D col;
        Sprite[] current;
        int frame;
        float t;
        bool collected;

        void Reset()
        {
            // default to trigger so OnTriggerEnter2D fires
            var c = GetComponent<Collider2D>();
            if (c) c.isTrigger = true;
        }

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            col = GetComponent<Collider2D>();

            current = (idleFrames != null && idleFrames.Length > 0) ? idleFrames : System.Array.Empty<Sprite>();
            frame = (randomStartFrame && current.Length > 0) ? Random.Range(0, current.Length) : 0;

            if (sr && current.Length > 0) sr.sprite = current[frame];
        }

        void Update()
        {
            // animate current frames (idle or collected)
            if (current == null || current.Length <= 1) return;

            t += Time.deltaTime;
            float step = 1f / Mathf.Max(0.0001f, frameRate);
            if (t >= step)
            {
                t -= step;
                frame++;

                // end of current sequence?
                if (frame >= current.Length)
                {
                    if (collected && onPickup == PickupBehavior.PlayCollectedAnimationThenHide)
                    {
                        gameObject.SetActive(false);
                        return;
                    }
                    frame = 0; // loop idle
                }

                if (sr) sr.sprite = current[frame];
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;

            // find player even if collider is on a child
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
                frame = 0;
                t = 0f;
                if (sr) sr.sprite = current[0];
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
