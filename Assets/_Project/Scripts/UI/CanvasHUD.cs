
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasHUD : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Health playerHealth;

    [Header("Health UI")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text healthText;

    [Header("Ammo UI")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text reloadText;

    [Header("Weapon UI")]
    [SerializeField] private TMP_Text weaponNameText;

    void Start()
    {
        if (healthBar != null && playerHealth != null)
        {
            healthBar.minValue = 0;
            healthBar.maxValue = playerHealth.Max;
        }

        if (reloadText != null)
            reloadText.text = "";
    }

    void Update()
    {
        // Update health independently.
        if (playerHealth != null)
        {
            if (healthBar != null)
            {
                healthBar.maxValue = playerHealth.Max;
                healthBar.value = playerHealth.Current;
            }

            if (healthText != null)
            {
                healthText.text =
                    $"HEALTH {playerHealth.Current:0}/{playerHealth.Max:0}";
            }
        }

        if (player == null)
            return;

        // Read the currently equipped weapon ONCE.
        Weapon weapon = player.CurrentWeapon;

        if (weapon == null || weapon.Data == null)
        {
            if (weaponNameText != null)
                weaponNameText.text = "NO WEAPON";

            return;
        }

        // Both labels use the exact same weapon data.
        string currentWeaponName = weapon.Data.displayName;

        if (weaponNameText != null)
            weaponNameText.text = currentWeaponName;

        if (ammoText != null)
        {
            ammoText.text =
                $"{currentWeaponName} {weapon.MagAmmo}/{weapon.ReserveAmmo}";
        }

        if (reloadText != null)
        {
            reloadText.text = weapon.IsReloading
                ? $"RELOADING {weapon.ReloadProgress:P0}"
                : "";
        }
    }
}
