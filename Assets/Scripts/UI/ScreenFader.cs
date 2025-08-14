using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("Setup")]
    [SerializeField] private CanvasGroup canvasGroup;   // assign in Inspector (the one on this GameObject)
    [SerializeField] private Image overlayImage;        // optional: only if you want to tweak color via code

    [Header("Behavior")]
    [SerializeField] private bool fadeInOnStart = true; // fade from black when scene loads
    [SerializeField] private float defaultDuration = 0.5f;
    [SerializeField] private bool persistAcrossScenes = true; // DontDestroyOnLoad

    public bool IsFading { get; private set; }

    Coroutine currentRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (persistAcrossScenes) DontDestroyOnLoad(gameObject);

        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Start fully black so we can fade in
        canvasGroup.alpha = 1f;

        // Ensure input is blocked while visible
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        if (fadeInOnStart)
            StartCoroutine(FadeIn(defaultDuration));
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (fadeInOnStart)
        {
            // If you keep this as a persistent object, make sure we’re opaque right after load
            canvasGroup.alpha = 1f;
            StartCoroutine(FadeIn(defaultDuration));
        }
    }

    public IEnumerator FadeOut(float duration = -1f)
    {
        if (duration < 0f) duration = defaultDuration;
        yield return StartFade(1f, duration); // target alpha 1 (black)
    }

    public IEnumerator FadeIn(float duration = -1f)
    {
        if (duration < 0f) duration = defaultDuration;
        yield return StartFade(0f, duration); // target alpha 0 (transparent)
    }

    public IEnumerator FadeOutIn(System.Func<IEnumerator> midRoutine, float outDuration = -1f, float inDuration = -1f)
    {
        if (outDuration < 0f) outDuration = defaultDuration;
        if (inDuration < 0f) inDuration = defaultDuration;

        yield return FadeOut(outDuration);
        if (midRoutine != null)
            yield return midRoutine();
        yield return FadeIn(inDuration);
    }

    public void SetColor(Color color)
    {
        if (overlayImage) overlayImage.color = color;
    }

    IEnumerator StartFade(float target, float duration)
    {
        // cancel any ongoing fade
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(FadeRoutine(target, duration));
        yield return currentRoutine;
    }

    IEnumerator FadeRoutine(float target, float duration)
    {
        IsFading = true;
        float start = canvasGroup.alpha;
        float t = 0f;

        // Always block input during fade
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        if (Mathf.Approximately(duration, 0f))
        {
            canvasGroup.alpha = target;
        }
        else
        {
            while (t < duration)
            {
                t += Time.unscaledDeltaTime; // unaffected by timescale (e.g., during pauses)
                float p = Mathf.Clamp01(t / duration);
                canvasGroup.alpha = Mathf.Lerp(start, target, p);
                yield return null;
            }
            canvasGroup.alpha = target;
        }

        // If fully transparent, allow clicks again
        if (Mathf.Approximately(canvasGroup.alpha, 0f))
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        IsFading = false;
        currentRoutine = null;
    }
}
