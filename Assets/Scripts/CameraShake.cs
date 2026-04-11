using UnityEngine;

/// <summary>
/// Simple camera shake script for micro-impacts.
/// </summary>
public class CameraShake : MonoBehaviour
{
    private Vector3 originalPos;
    private float shakeTimer = 0f;
    private float currentIntensity = 0f;

    private void Awake()
    {
        originalPos = transform.localPosition;
    }

    private void Update()
    {
        if (shakeTimer > 0)
        {
            transform.localPosition = originalPos + Random.insideUnitSphere * currentIntensity;
            shakeTimer -= Time.deltaTime;
        }
        else
        {
            transform.localPosition = originalPos;
        }
    }

    /// <summary>
    /// Triggers a camera shake.
    /// </summary>
    /// <param name="intensity">The strength of the shake.</param>
    /// <param name="duration">Duration in seconds.</param>
    public void Shake(float intensity, float duration)
    {
        currentIntensity = intensity;
        shakeTimer = duration;
    }
}
