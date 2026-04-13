using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    [Header("Stats")]
    public int HP = 30;
    public int MaxHP = 30;
    public int AttackPower = 5;
    public bool IsMagic = false;
    public bool IsDead = false;

    [Header("Attack Timer")]
    public float AttackInterval = 4.0f;
    private float timer;

    private Animator animator;
    private SpriteRenderer sr;
    private MaterialPropertyBlock mpb;
    private Material defaultMaterial;
    private static Material overlayMaterial;
    private static readonly int _OverlayColorId = Shader.PropertyToID("_OverlayColor");

    private void Start()
    {
        HP = MaxHP;
        timer = Random.Range(1.0f, AttackInterval); // Random start offset

        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
        
        if (sr != null)
        {
            defaultMaterial = sr.sharedMaterial;
        }

        if (overlayMaterial == null)
        {
            overlayMaterial = Resources.Load<Material>("EnemyOverlay");
            // If Resources.Load fails, fallback to specific path via script
            #if UNITY_EDITOR
            if (overlayMaterial == null)
            {
                overlayMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/EnemyOverlay.mat");
            }
            #endif
        }

        // Detect if I am a ZombieMage based on name
if (gameObject.name.Contains("ZombieMage"))
        {
            IsMagic = true;
            AttackPower = 3; // Magic is slightly weaker but ignores Shield
        }
    }

    private void Update()
    {
        if (IsDead) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            CombatManager.Instance.AddEnemyAction(this, AttackPower, IsMagic);
            timer = AttackInterval;
        }
    }

    public void Attack()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        HP -= damage;
        Debug.Log($"{name} took {damage} damage. HP: {HP}");
        
        // Add simple visual feedback (shake or color shift)
        StartCoroutine(HitFeedback());

        if (HP <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator HitFeedback()
    {
        if (sr != null && mpb != null && overlayMaterial != null)
        {
            // Swap to overlay material
            sr.sharedMaterial = overlayMaterial;
            
            sr.GetPropertyBlock(mpb);
            mpb.SetColor(_OverlayColorId, Color.red);
            sr.SetPropertyBlock(mpb);

            yield return new WaitForSeconds(0.1f);

            // Revert to original material and clear property block
            sr.SetPropertyBlock(null);
            sr.sharedMaterial = defaultMaterial;
        }
    }

    private void Die()
    {
        IsDead = true;
        Debug.Log($"{name} died!");
        Destroy(gameObject, 0.2f);
    }
}
