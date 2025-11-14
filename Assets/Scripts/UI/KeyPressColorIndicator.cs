using UnityEngine;
using TMPro;

[RequireComponent(typeof(SpriteRenderer))]
public class KeyPressColorIndicator : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Tecla a monitoritzar")]
    public KeyCode key = KeyCode.Space;

    [Header("Colors")]
    public Color colorUp = Color.white;
    public Color colorDown = Color.red;

    [Header("Etiqueta (Text)")]
    [Tooltip("Si està activat, usa aquest text en lloc del nom de la tecla")]
    public bool useCustomLabel = false;
    [Tooltip("Text personalitzat per a l'etiqueta")]
    public string customLabel = "";
    [Tooltip("Converteix automàticament a MAJÚSCULES")]
    public bool uppercaseAuto = true;
    [Tooltip("Desplaçament local de l'etiqueta (units)")]
    public Vector2 labelOffset = Vector2.zero;
    [Tooltip("Escala de l'etiqueta")]
    public float labelScale = 1f;
    [Tooltip("Offset de sorting perquè el text estigui per sobre del disc")]
    public int sortingOrderOffset = 1;

    [Header("Text Appearance")]
    [Tooltip("Color del text de l'etiqueta")]
    public Color fontColor = Color.black;
    [Tooltip("Mida del text (només si no està autosizing)")]
    public float fontSize = 3f;
    [Tooltip("Permet que TextMeshPro ajusti automàticament la mida del text")]
    public bool enableAutoSize = true;

    [Tooltip("Assigna un TextMeshPro (opcional). Si no n'hi ha, es crearà automàticament com a fill.")]
    public TextMeshPro labelTMP;

    private SpriteRenderer _sr;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        EnsureLabel();
        UpdateLabelVisuals();
        UpdateLabelText();
        ApplyLabelSorting();
        UpdateLabelTransform();
    }

    void Update()
    {
        bool pressed = Input.GetKey(key);
        _sr.color = pressed ? colorDown : colorUp;
    }

    void LateUpdate()
    {
        UpdateLabelTransform();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateLabelVisuals();
            UpdateLabelText();
            ApplyLabelSorting();
            UpdateLabelTransform();
        }
    }

    // --- Helpers ---
    void EnsureLabel()
    {
        if (labelTMP == null)
            labelTMP = GetComponentInChildren<TextMeshPro>(true);

        if (labelTMP == null)
        {
            var go = new GameObject("KeyLabelTMP");
            go.transform.SetParent(transform, false);
            labelTMP = go.AddComponent<TextMeshPro>();
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.raycastTarget = false;
        }
    }

    void UpdateLabelVisuals()
    {
        if (labelTMP == null) return;

        labelTMP.color = fontColor;
        labelTMP.enableAutoSizing = enableAutoSize;
        if (!enableAutoSize)
            labelTMP.fontSize = fontSize;
    }

    void ApplyLabelSorting()
    {
        if (labelTMP != null && _sr != null)
        {
            labelTMP.sortingLayerID = _sr.sortingLayerID;
            labelTMP.sortingOrder = _sr.sortingOrder + sortingOrderOffset;
        }
    }

    void UpdateLabelText()
    {
        if (labelTMP == null) return;
        string t = useCustomLabel && !string.IsNullOrEmpty(customLabel)
            ? customLabel
            : key.ToString();
        labelTMP.text = uppercaseAuto ? t.ToUpperInvariant() : t;
    }

    void UpdateLabelTransform()
    {
        if (labelTMP == null) return;
        labelTMP.transform.localPosition = new Vector3(labelOffset.x, labelOffset.y, -0.01f);
        labelTMP.transform.localScale = Vector3.one * Mathf.Max(0.0001f, labelScale);
    }

    // Per canviar la tecla per codi si vols
    public void SetKey(KeyCode newKey)
    {
        key = newKey;
        UpdateLabelText();
    }
}
