using UnityEngine;

/// The escape point. Needs a Collider2D (set to Trigger automatically).
/// Only completes the level after the file has been taken.
[RequireComponent(typeof(Collider2D))]
public class ExitZone : MonoBehaviour
{
    void Awake() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerStay2D(Collider2D other)
    {
        if (GameManager.Instance == null) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;

        GameManager.Instance.TryExit();
    }
}
