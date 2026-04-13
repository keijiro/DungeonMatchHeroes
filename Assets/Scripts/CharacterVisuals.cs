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

    public void TriggerDamageEffect()
    {
        Flash(new Color(1f, 0f, 0f, 0.8f), 0.2f); // Red flash
        Shake(0.15f, 0.2f); // Left-right shake
    }

    public void TriggerAttackEffect()
    {
        Flash(new Color(1f, 1f, 1f, 0.8f), 0.1f); // White flash
    }

    public void Flash(Color color, float duration)
    {
        if (currentFlashCoroutine != null) StopCoroutine(currentFlashCoroutine);
        currentFlashCoroutine = StartCoroutine(FlashRoutine(color, duration));
    }

    public void Shake(float amount, float duration)
    {
        if (currentShakeCoroutine != null) StopCoroutine(currentShakeCoroutine);
        currentShakeCoroutine = StartCoroutine(ShakeRoutine(amount, duration));
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        if (sr == null || overlayMaterial == null) yield break;

        sr.sharedMaterial = overlayMaterial;
        sr.GetPropertyBlock(mpb);
        mpb.SetColor(_OverlayColorId, color);
        sr.SetPropertyBlock(mpb);

        yield return new WaitForSeconds(duration);

        sr.SetPropertyBlock(null);
        sr.sharedMaterial = defaultMaterial;
        currentFlashCoroutine = null;
    }

    private IEnumerator ShakeRoutine(float amount, float duration)
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
