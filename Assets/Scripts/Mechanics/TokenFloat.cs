using UnityEngine;

public class TokenFloat : MonoBehaviour
{
    [Header("Float Settings")]
    public float amplitude = 0.25f;     // How high the token moves up/down
    public float frequency = 1f;        // How fast it moves

    [Header("Offset")]
    public float phaseOffset = 0f;      // Time/phase offset between tokens
    public bool randomizeOffset = true; // Optional: auto-random offset

    private Vector3 _startPos;

    void Start()
    {
        _startPos = transform.position;

        // Give each token a slight random offset so they don't float in sync
        if (randomizeOffset)
        {
            phaseOffset += Random.Range(0f, 2f * Mathf.PI);
        }
    }

    void Update()
    {
        // Sine wave = smooth easing motion
        float sin = Mathf.Sin(Time.time * frequency + phaseOffset);
        float newY = _startPos.y + sin * amplitude;

        transform.position = new Vector3(_startPos.x, newY, _startPos.z);
    }
}
