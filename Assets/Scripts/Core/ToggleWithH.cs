using UnityEngine;

public class ToggleSpriteRendererWithH : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.H;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey) && spriteRenderer != null)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
        }
    }
}