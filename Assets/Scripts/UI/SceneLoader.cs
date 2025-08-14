using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor; // for SceneAsset + AssetDatabase in OnValidate
#endif

[AddComponentMenu("Game/UI/Fade On Trigger 2D")]
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class FadeOnTrigger2D : MonoBehaviour
{
    [Header("Who can trigger it")]
    [SerializeField] private string playerTag = "Player";

    [Header("Collision Mode")]
    [Tooltip("If true: uses OnTriggerEnter2D (collider must be IsTrigger). If false: uses OnCollisionEnter2D.")]
    [SerializeField] private bool useTrigger = true;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private bool fadeBackInAfterLoad = true;

    [Header("Scene To Load")]
    [Tooltip("Prefer setting via the 'Next Scene (Asset)' field in the Editor, which auto-fills this name.")]
    [SerializeField] private string nextSceneName = "";

#if UNITY_EDITOR
    [Tooltip("Drag a Scene asset here in the Editor. The scene name will auto-fill into 'nextSceneName'.")]
    [SerializeField] private SceneAsset nextSceneAsset;
#endif

    [Header("Debug")]
    [SerializeField] private bool logDebug = true;

    private bool triggered;

    // ---------- Unity Hooks ----------

    private void Reset()
    {
        // Keep collider mode consistent with 'useTrigger'
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = useTrigger;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep collider mode consistent when you toggle the option
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = useTrigger;

        // Sync scene name from dragged SceneAsset
        if (nextSceneAsset != null)
        {
            string path = AssetDatabase.GetAssetPath(nextSceneAsset);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrEmpty(name))
                nextSceneName = name;

            // Warn if the scene might not be in the active build profile / shared list
            // (Legacy check: EditorBuildSettings.scenes; Build Profiles vary per profile)
            bool inLegacyBuildList = false;
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.enabled && s.path == path) { inLegacyBuildList = true; break; }
            }

            if (!inLegacyBuildList)
            {
                Debug.LogWarning(
                    $"[FadeOnTrigger2D] Scene '{name}' may not be included in the active Build Profile or legacy Build Settings. " +
                    $"Add it via File → Build Profiles (or File → Build Settings).", this);
            }
        }
    }
#endif

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTrigger) return;
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;
        TriggerSequence();
    }

    private void OnCollisionEnter2D(Collision2D c)
    {
        if (useTrigger) return;
        if (triggered) return;
        if (!c.collider.CompareTag(playerTag)) return;
        TriggerSequence();
    }

    // ---------- Public (optional) ----------
    public void TriggerNow()
    {
        if (triggered) return;
        TriggerSequence();
    }

    // ---------- Internals ----------

    private void TriggerSequence()
    {
        triggered = true;
        StartCoroutine(FadeThenLoad());
    }

    private IEnumerator FadeThenLoad()
    {
        // Fade out if we have a fader; otherwise warn and continue
        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeOut(fadeDuration);
        }
        else if (logDebug)
        {
            Debug.LogWarning("[FadeOnTrigger2D] No ScreenFader found. Loading without fade.", this);
        }

        // If no scene set, just stop after fade out
        if (string.IsNullOrEmpty(nextSceneName))
        {
            if (logDebug) Debug.Log("[FadeOnTrigger2D] No scene name set; staying in current scene (fade-out only).", this);
            yield break;
        }

        // Sanity check: is this scene actually available in the active build content?
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"[FadeOnTrigger2D] Scene '{nextSceneName}' is not in the active Build Profile/shared scene list (or the name is wrong). " +
                           $"Add it via File → Build Profiles (or Build Settings) and ensure it's enabled.", this);
            yield break;
        }

        // Load by NAME (safer across Build Profiles)
        if (logDebug) Debug.Log($"[FadeOnTrigger2D] Loading by name '{nextSceneName}'", this);
        var async = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
        if (async == null)
        {
            Debug.LogError("[FadeOnTrigger2D] LoadSceneAsync returned null. Check the scene name and build profile.", this);
            yield break;
        }

        while (!async.isDone) yield return null;

        // Fade back in on the next scene (if the fader is persistent or present)
        if (fadeBackInAfterLoad && ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeIn(fadeDuration);
    }
}
