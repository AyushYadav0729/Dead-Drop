using UnityEngine;

/// The file. Needs a Collider2D (set to Trigger automatically). Touching it as the player
/// starts the escape phase and sets off the alarm.
[RequireComponent(typeof(Collider2D))]
public class ObjectivePickup : MonoBehaviour
{
    [SerializeField] float pulseAmount = 0.08f;
    [SerializeField] float pulseSpeed = 3f;

    Vector3 baseScale;
    bool collected;

    void Awake()
    {
        baseScale = transform.localScale;
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Update()
    {
        transform.localScale = baseScale * (1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || other.GetComponentInParent<PlayerController>() == null) return;
        collected = true;

        if (GameManager.Instance != null) GameManager.Instance.CollectObjective(transform.position);
        Destroy(gameObject);
    }
}
