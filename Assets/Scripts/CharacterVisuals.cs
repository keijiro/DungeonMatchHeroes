using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class CharacterVisuals : MonoBehaviour
{
    private SpriteRenderer sr;
    private Material defaultMaterial;
    private static Material overlayMaterial;
    private MaterialPropertyBlock mpb;
    private static readonly int _OverlayColorId = Shader.PropertyToID("_OverlayColor");

    private Vector3 originalLocalPos;
    private Coroutine currentFlashCoroutine;
    private Coroutine currentShakeCoroutine;

    private bool hasPersistentColor = false;
    private Color persistentColor;

    private void Awake()
    {
sr = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
        if (sr != null)
        {
            defaultMaterial = sr.sharedMaterial;
        }
        originalLocalPos = transform.localPosition;

        if (overlayMaterial == null)
        {
            overlayMaterial = Resources.Load<Material>("EnemyOverlay");
        }
    }

    public IEnumerator TriggerDamageEffect()
    {
        // Parallel execution
        var flash = StartCoroutine(FlashRoutine(new Color(1f, 0f, 0f, 0.8f), 0.2f));
        var shake = StartCoroutine(ShakeRoutine(0.15f, 0.2f));
        
        yield return flash;
        yield return shake;
    }

    public IEnumerator TriggerAttackEffect()
    {
        yield return StartCoroutine(FlashRoutine(new Color(1f, 1f, 1f, 0.8f), 0.1f));
    }

    public IEnumerator TriggerFlash(Color color, float duration)
    {
        yield return StartCoroutine(FlashRoutine(color, duration));
    }

    public IEnumerator TriggerSoftDoubleFlash(Color color, float duration)
    {
        yield return StartCoroutine(SoftDoubleFlashRoutine(color, duration));
    }

    public void Flash(Color color, float duration)
    {
        if (currentFlashCoroutine != null) StopCoroutine(currentFlashCoroutine);
        currentFlashCoroutine = StartCoroutine(FlashRoutine(color, duration));
    }

    public void SoftFlash(Color color, float duration)
    {
        if (currentFlashCoroutine != null) StopCoroutine(currentFlashCoroutine);
        currentFlashCoroutine = StartCoroutine(SoftDoubleFlashRoutine(color, duration));
    }

    public void Shake(float amount, float duration)
    {
        if (currentShakeCoroutine != null) StopCoroutine(currentShakeCoroutine);
        currentShakeCoroutine = StartCoroutine(ShakeRoutine(amount, duration));
    }

    public void SetPersistentColor(Color color)
    {
        if (currentFlashCoroutine != null) StopCoroutine(currentFlashCoroutine);
        hasPersistentColor = true;
        persistentColor = color;
        ApplyPersistentColor();
    }

    public void ClearPersistentColor()
    {
        hasPersistentColor = false;
        ResetMaterial();
    }

    private void ApplyPersistentColor()
    {
        if (sr == null || overlayMaterial == null) return;

        sr.sharedMaterial = overlayMaterial;
        sr.GetPropertyBlock(mpb);
        mpb.SetColor(_OverlayColorId, persistentColor);
        sr.SetPropertyBlock(mpb);
    }

    private void ResetMaterial()
    {
        if (sr == null) return;

        if (hasPersistentColor)
        {
            ApplyPersistentColor();
        }
        else
        {
            sr.SetPropertyBlock(null);
            sr.sharedMaterial = defaultMaterial;
        }
    }

    public IEnumerator FlashRoutine(Color color, float duration)
    {
        if (sr == null || overlayMaterial == null) yield break;

        sr.sharedMaterial = overlayMaterial;
        sr.GetPropertyBlock(mpb);
        mpb.SetColor(_OverlayColorId, color);
        sr.SetPropertyBlock(mpb);

        yield return new WaitForSeconds(duration);

        ResetMaterial();
        currentFlashCoroutine = null;
    }

    public IEnumerator SoftDoubleFlashRoutine(Color color, float duration)
    {
        if (sr == null || overlayMaterial == null) yield break;

        sr.sharedMaterial = overlayMaterial;
        float pulseDuration = duration / 2f;
        float baseAlpha = color.a;

        for (int i = 0; i < 2; i++)
        {
            float elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pulseDuration;
                float pulse = Mathf.Sin(t * Mathf.PI);

                sr.GetPropertyBlock(mpb);
                Color c = color;
                c.a = baseAlpha * pulse;
                mpb.SetColor(_OverlayColorId, c);
                sr.SetPropertyBlock(mpb);
                yield return null;
            }
        }

        ResetMaterial();
        currentFlashCoroutine = null;
    }

    public IEnumerator FadeFlashRoutine(Color color, float duration)
    {
        if (sr == null || overlayMaterial == null) yield break;

        sr.sharedMaterial = overlayMaterial;
        float elapsed = 0f;
        float baseAlpha = color.a;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Smoothly fade out the overlay
            float alpha = Mathf.Lerp(baseAlpha, 0f, t);

            sr.GetPropertyBlock(mpb);
            Color c = color;
            c.a = alpha;
            mpb.SetColor(_OverlayColorId, c);
            sr.SetPropertyBlock(mpb);
            yield return null;
        }

        ResetMaterial();
        currentFlashCoroutine = null;
    }

    public IEnumerator ShakeRoutine(float amount, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Only horizontal shake
            float xOffset = Random.Range(-amount, amount);
            transform.localPosition = originalLocalPos + new Vector3(xOffset, 0, 0);
            yield return null;
        }
        transform.localPosition = originalLocalPos;
        currentShakeCoroutine = null;
    }
}
