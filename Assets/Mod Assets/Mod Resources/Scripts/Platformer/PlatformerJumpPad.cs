using UnityEngine;
using Platformer.Mechanics;

public class PlatformerJumpPad : MonoBehaviour
{
    public float verticalVelocity;

    void OnTriggerEnter2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;
        var player = rb.GetComponent<PlayerController>();
        if (player == null) return;
        AddVelocity(player);
    }

    void AddVelocity(PlayerController player)
    {
        Vector2 v = player.velocity;   // 1) copies el vector
        v.y = verticalVelocity;        // 2) el modifiques
        player.velocity = v;           // 3) el tornes a assignar
    }

}
