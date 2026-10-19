using UnityEngine;
using UnityEngine.Rendering.Universal;

/// Turns the scene's Global Light 2D red and makes it pulse when the alarm goes off.
/// This is the "Last Light" mood: calm and cool before, red and urgent after the file is taken.
public class AlarmLighting : MonoBehaviour
{
    [SerializeField] Light2D globalLight;
    [SerializeField] Color alarmColor = new Color(1f, 0.25f, 0.2f);
    [SerializeField] float alarmIntensity = 0.65f;
    [SerializeField] float pulseAmount = 0.15f;
    [SerializeField] float pulseSpeed = 3f;
    [SerializeField] float fadeSpeed = 2f;

    bool alarm;

    void OnEnable() => AlarmSystem.Raised += OnAlarm;
    void OnDisable() => AlarmSystem.Raised -= OnAlarm;

    void Start() { if (AlarmSystem.Active) alarm = true; }

    void OnAlarm() => alarm = true;

    void Update()
    {
        if (!alarm) return;

        globalLight.color = Color.Lerp(globalLight.color, alarmColor, fadeSpeed * Time.deltaTime);
        globalLight.intensity = alarmIntensity + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
    }
}
