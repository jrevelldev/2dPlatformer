using System.Collections;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// Optional interface for a token "appear" animation.
    /// Implement this on any component on the token if you want a typed hook.
    /// </summary>
    public interface ITokenAppear
    {
        void PlayAppear(Sprite[] explicitAppearFrames, bool reverseIfMissing, bool disableColliderDuringAppear);
    }

    /// <summary>
    /// Spawns and respawns a SimpleToken after it hides itself.
    /// - Reuses or instantiates a token.
    /// - Optionally triggers an "appear" animation via ITokenAppear or SendMessage("PlayAppear").
    /// - Uses a SpriteRenderer preview to show the spawn spot; visible in Editor, auto-hides in Play.
    /// </summary>
    [ExecuteAlways]
    public class TokenSpawner : MonoBehaviour
    {
        [Header("Setup")]
        public SimpleToken tokenPrefab;
        public Transform spawnPoint;                  // optional; if null, uses this object's transform

        [Header("Respawn")]
        [Min(0f)] public float respawnDelay = 5f;
        public bool spawnOnStart = true;
        public bool reuseInstance = true;             // re-enable same token vs. Instantiate new

        [Header("Appear Animation (optional)")]
        public bool playAppearOnSpawn = false;        // safe default: off (no dependency)
        public bool appearUsesReverseOfCollected = true;
        public Sprite[] appearFramesOverride;         // optional custom frames
        public bool disableColliderDuringAppear = true;

        [Header("Preview")]
        public bool previewEditorOnly = true;         // editor shows preview; play hides it instantly
        [SerializeField] SpriteRenderer previewSprite; // assign in inspector (auto-finds if missing)

        SimpleToken instance;
        bool respawning;

        void Awake()
        {
            if (previewSprite == null)
                previewSprite = GetComponentInChildren<SpriteRenderer>(true);
        }

        void Start()
        {
            if (!Application.isPlaying)
            {
                UpdatePreview();
                return;
            }

            if (spawnOnStart) SpawnNow();
            UpdatePreview();
        }

        void OnEnable()
        {
            if (Application.isPlaying && instance != null) Subscribe(instance);
            UpdatePreview();
        }

        void OnDisable()
        {
            if (Application.isPlaying && instance != null) Unsubscribe(instance);
            UpdatePreview();
        }

        // Keep editor preview correct when values change.
        void OnValidate() => UpdatePreview();

        void Subscribe(SimpleToken t) => t.Hidden += OnTokenHidden;
        void Unsubscribe(SimpleToken t) => t.Hidden -= OnTokenHidden;

        void OnTokenHidden(SimpleToken t)
        {
            if (!Application.isPlaying) return;

            // Show preview while waiting to respawn
            SetPreview(true);
            if (!respawning) StartCoroutine(RespawnAfterDelay());
        }

        IEnumerator RespawnAfterDelay()
        {
            respawning = true;
            yield return new WaitForSeconds(respawnDelay);

            var (pos, rot) = GetSpawnPose();

            if (reuseInstance && instance != null)
            {
                instance.transform.SetPositionAndRotation(pos, rot);
                instance.ResetForRespawn();
                instance.gameObject.SetActive(true);
                MaybePlayAppear(instance);
            }
            else
            {
                if (instance != null) Unsubscribe(instance);

                instance = Instantiate(tokenPrefab, pos, rot);
                Subscribe(instance);

                instance.ResetForRespawn();
                MaybePlayAppear(instance);
            }

            // token is live -> hide preview
            SetPreview(false);
            respawning = false;
        }

        [ContextMenu("Spawn Now")]
        public void SpawnNow()
        {
            var (pos, rot) = GetSpawnPose();

            if (instance == null)
            {
                instance = Instantiate(tokenPrefab, pos, rot);
                if (Application.isPlaying) Subscribe(instance);
            }
            else if (!reuseInstance || !instance.gameObject.activeInHierarchy)
            {
                if (!reuseInstance)
                {
                    if (Application.isPlaying) Unsubscribe(instance);
                    instance = Instantiate(tokenPrefab, pos, rot);
                    if (Application.isPlaying) Subscribe(instance);
                }
                else
                {
                    instance.transform.SetPositionAndRotation(pos, rot);
                    instance.gameObject.SetActive(true);
                }
            }

            instance.ResetForRespawn();
            MaybePlayAppear(instance);

            // token is live -> hide preview
            SetPreview(false);
        }

        void MaybePlayAppear(SimpleToken t)
        {
            if (!Application.isPlaying || !playAppearOnSpawn || t == null) return;

            // Preferred: call any component that implements the typed interface.
            var behaviours = t.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ITokenAppear appear)
                {
                    appear.PlayAppear(appearFramesOverride, appearUsesReverseOfCollected, disableColliderDuringAppear);
                    return;
                }
            }

            // Fallback: fire-and-forget message (no params). Safe if nobody implements it.
            t.gameObject.SendMessage("PlayAppear", SendMessageOptions.DontRequireReceiver);
        }

        (Vector3 pos, Quaternion rot) GetSpawnPose()
        {
            if (spawnPoint != null) return (spawnPoint.position, spawnPoint.rotation);
            return (transform.position, transform.rotation);
        }

        // --- preview helpers ---
        void UpdatePreview()
        {
            if (previewSprite == null) return;

            if (!Application.isPlaying)
            {
                // In the editor, always show the preview to help placement.
                previewSprite.enabled = true;
                return;
            }

            // In Play: hide immediately if editor-only; otherwise show only while token is inactive.
            bool tokenActive = instance != null && instance.gameObject.activeInHierarchy;
            previewSprite.enabled = !previewEditorOnly && !tokenActive;
        }

        void SetPreview(bool visibleWhenRuntime)
        {
            if (previewSprite == null) return;

            if (!Application.isPlaying)
            {
                previewSprite.enabled = true; // editor: always on
            }
            else
            {
                // runtime: respect the editor-only toggle
                previewSprite.enabled = !previewEditorOnly && visibleWhenRuntime;
            }
        }
    }
}
