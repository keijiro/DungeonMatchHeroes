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

    private void Start()
    {
        HP = MaxHP;
        timer = Random.Range(1.0f, AttackInterval); // Random start offset

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
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color original = sr.color;
            sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            sr.color = original;
        }
    }

    private void Die()
    {
        IsDead = true;
        Debug.Log($"{name} died!");
        Destroy(gameObject, 0.2f);
    }
}
