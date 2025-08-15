// Checkpoint.cs (auto-spawn only, no persistence)
// Put this on each checkpoint (needs a 2D trigger collider set to Is Trigger).
// When the player reaches a higher-Order checkpoint, the global spawnPoint moves here,
// optional VFX/SFX play, and an ActivationPrefab (if set) is spawned and auto-destroys.

using System.Collections;                 // for coroutines
using System.Collections.Generic;
using Platformer.Core;                    // Simulation.GetModel<T>()
using Platformer.Model;                   // PlatformerModel (has spawnPoint)
using Platformer.Mechanics;               // PlayerController
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[AddComponentMenu("Platformer/Checkpoint")]
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("Order / Progression")]
    [Tooltip("Checkpoints with higher Order supersede earlier ones. 0 for the first near level start.")]
    public int Order = 0;

    [Header("Spawn Behavior")]
    [Tooltip("If this is Order 0, optionally set SpawnPoint here on scene start.")]
    public bool SetSpawnIfFirstOnStart = true;

    [Header("Visuals (all optional)")]
    [Tooltip("SpriteRenderer to tint/swap when active/inactive. Auto-found if left empty.")]
    public SpriteRenderer FlagRenderer;
    public Color InactiveColor = new Color(1f, 1f, 1f, 0.6f);
    public Color ActiveColor = Color.white;
    [Tooltip("Optional sprite to use when this checkpoint is active.")]
    public Sprite ActiveSprite;
    [Tooltip("Optional sprite to use when this checkpoint is inactive.")]
    public Sprite InactiveSprite;
    [Tooltip("Optional Animator with a bool parameter named 'Active'.")]
    public Animator Animator;
    [Tooltip("Optional particle system to play when this checkpoint becomes active.")]
    public ParticleSystem ActivateFX;

    [Header("Audio (optional)")]
    [Tooltip("If assigned, will play the clip here; otherwise a temporary one-shot source is used.")]
    public AudioSource Audio;
    [Tooltip("Clip played when this checkpoint becomes active.")]
    public AudioClip ActivateSfx;
    [Range(0f, 1f)] public float ActivateSfxVolume = 1f;
    [Tooltip("If no AudioSource is assigned, play SFX as 2D (non-spatial) instead of 3D.")]
    public bool PlaySfxAs2D = false;

    [Header("Auto-Spawn Prefab (no events needed)")]
    [Tooltip("Prefab to spawn automatically when this checkpoint is activated.")]
    public GameObject ActivationPrefab;
    [Tooltip("Offset from checkpoint position where the prefab spawns.")]
    public Vector3 ActivationSpawnOffset = Vector3.zero;
    [Tooltip("Parent the spawned prefab to this checkpoint (useful on moving platforms).")]
    public bool ParentActivationToCheckpoint = false;
    [Tooltip("If the prefab has no ParticleSystems, destroy after this many seconds.")]
    public float FallbackDestroyDelay = 5f;

    // ---- static, per-scene state ----
    static int s_highestOrder = -1;
    static Checkpoint s_active;
    static readonly List<Checkpoint> s_instances = new List<Checkpoint>();
    static string s_sceneId = "";

    // cache
    PlatformerModel model;

    void Awake()
    {
        model = Simulation.GetModel<PlatformerModel>();

        // Reset static state when the scene changes.
        var currentSceneId = SceneManager.GetActiveScene().path;
        if (s_sceneId != currentSceneId)
        {
            s_sceneId = currentSceneId;
            s_highestOrder = -1;
            s_active = null;
            s_instances.Clear();
        }

        // Auto-find a SpriteRenderer if none assigned.
        if (FlagRenderer == null) FlagRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable()
    {
        if (!s_instances.Contains(this)) s_instances.Add(this);
        EnsureTriggerCollider();
        UpdateVisual();
    }

    void OnDisable()
    {
        s_instances.Remove(this);
        if (s_active == this) s_active = null;
    }

    void Start()
    {
        // Fresh scene start: snap spawn to the first checkpoint if desired.
        if (Order == 0 && SetSpawnIfFirstOnStart && s_highestOrder < 0)
        {
            ApplyAsSpawn();
            s_highestOrder = 0;
        }
        else
        {
            UpdateVisual();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Only the player can activate
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        // Ignore if this checkpoint is not later than the best we've reached
        if (Order <= s_highestOrder) return;

        s_highestOrder = Order;
        ApplyAsSpawn();
    }

    void ApplyAsSpawn()
    {
        // Move the global spawn point here
        if (model != null && model.spawnPoint != null)
        {
            model.spawnPoint.transform.position = transform.position;
        }
        else
        {
            Debug.LogWarning("[Checkpoint] PlatformerModel.spawnPoint is missing.");
        }

        // Mark active and update visuals
        s_active = this;

        // VFX
        if (ActivateFX != null) ActivateFX.Play();

        // SFX (no AudioSource required)
        if (ActivateSfx != null)
        {
            if (Audio != null)
            {
                Audio.PlayOneShot(ActivateSfx, ActivateSfxVolume);
            }
            else
            {
                if (PlaySfxAs2D)
                    StartCoroutine(Play2DOneShot(ActivateSfx, ActivateSfxVolume));
                else
                    AudioSource.PlayClipAtPoint(ActivateSfx, transform.position, ActivateSfxVolume);
            }
        }

        // Auto-spawn prefab (no events)
        if (ActivationPrefab != null)
        {
            SpawnActivationPrefab();
        }

        UpdateAllVisuals();
    }

    void EnsureTriggerCollider()
    {
        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger) col.isTrigger = true;
    }

    void UpdateVisual()
    {
        bool isActive = (s_active == this);

        // Tint + optional sprite swap
        if (FlagRenderer != null)
        {
            FlagRenderer.color = isActive ? ActiveColor : InactiveColor;

            if (isActive && ActiveSprite != null) FlagRenderer.sprite = ActiveSprite;
            else if (!isActive && InactiveSprite != null) FlagRenderer.sprite = InactiveSprite;
        }

        // Animator bool "Active" (optional)
        if (Animator != null) Animator.SetBool("Active", isActive);
    }

    static void UpdateAllVisuals()
    {
        for (int i = 0; i < s_instances.Count; i++)
        {
            if (s_instances[i] != null) s_instances[i].UpdateVisual();
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Editor-only helpers for clarity
        Gizmos.color = (s_active == this) ? ActiveColor : InactiveColor;
        Gizmos.DrawWireSphere(transform.position, 0.25f);
        Gizmos.DrawSphere(transform.position, 0.08f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.25f, $"#{Order}");
    }
#endif

    // -----------------------------
    // Internal: spawn + self-destroy
    // -----------------------------
    void SpawnActivationPrefab()
    {
        var pos = transform.position + ActivationSpawnOffset;
        var parent = ParentActivationToCheckpoint ? transform : null;
        var instance = Instantiate(ActivationPrefab, pos, Quaternion.identity, parent);

        var systems = instance.GetComponentsInChildren<ParticleSystem>(true);
        if (systems.Length > 0)
        {
            foreach (var ps in systems)
            {
                var main = ps.main;
                if (main.loop) main.loop = false; // ensure completion
                ps.Play(true);
            }
            StartCoroutine(DestroyWhenParticlesDone(instance));
        }
        else
        {
            Destroy(instance, Mathf.Max(0.1f, FallbackDestroyDelay));
        }
    }

    IEnumerator DestroyWhenParticlesDone(GameObject go)
    {
        var systems = go.GetComponentsInChildren<ParticleSystem>(true);
        if (systems.Length == 0) { Destroy(go); yield break; }

        // Wait until ALL particle systems are completely done
        while (true)
        {
            bool anyAlive = false;
            for (int i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                if (ps != null && ps.IsAlive(true)) { anyAlive = true; break; }
            }
            if (!anyAlive) break;
            yield return null;
        }
        Destroy(go);
    }

    // -----------------------------
    // 2D (non-spatial) SFX helper
    // -----------------------------
    IEnumerator Play2DOneShot(AudioClip clip, float volume)
    {
        var go = new GameObject("Checkpoint OneShot 2D");
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialBlend = 0f; // 0 = 2D
        src.Play();
        Destroy(go, clip.length);
        yield break;
    }
}
