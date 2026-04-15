using UnityEngine;
using System.Collections;

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
    public CharacterVisuals visuals;

    private void Start()
    {
        HP = MaxHP;
        timer = Random.Range(1.0f, AttackInterval); // Random start offset

        animator = GetComponent<Animator>();
        visuals = GetComponent<CharacterVisuals>();
        if (visuals == null) visuals = gameObject.AddComponent<CharacterVisuals>();

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

    public IEnumerator Attack()
    {
        if (visuals != null)
        {
            yield return StartCoroutine(visuals.TriggerAttackEffect());
        }

        if (animator != null)
        {
            animator.SetTrigger("Attack");
            // Wait for animation is handled by CombatManager via Helper
        }
    }

    public IEnumerator TakeDamage(int damage)
    {
        if (IsDead) yield break;

        HP -= damage;
        Debug.Log($"{name} took {damage} damage. HP: {HP}");
        
        CombatManager.Instance.ShowCombatNumber(damage, Color.red, transform.position + Vector3.up);

        if (AudioManager.Instance != null)
{
            AudioManager.Instance.PlaySEWithRandomPitch(SEType.Hit, 0.8f);
        }

        if (visuals != null)
{
            yield return StartCoroutine(visuals.TriggerDamageEffect());
        }

        if (HP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(SEType.EnemyDie, 0.7f);
        }
        Debug.Log($"{name} died!");
        Destroy(gameObject, 0.1f);
    }
}
