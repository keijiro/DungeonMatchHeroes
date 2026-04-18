using UnityEngine;
using System.Collections;

public class EnemyUnit : MonoBehaviour
{
    [Header("Stats")]
    public int Level = 1;
    public int HP = 30;
    public int MaxHP = 30;
    public float AttackPower = 5f;
    public bool IsMagic = false;
    public bool IsDead = false;

    [Header("Attack Timer")]
    public float AttackInterval = 4.0f;
    private float timer;

    private Animator animator;
    public CharacterVisuals visuals;

    private void Start()
    {
        // Stats are now initialized by CombatManager during Spawn
        timer = Random.Range(1.0f, AttackInterval); // Random start offset

        animator = GetComponent<Animator>();
        visuals = GetComponent<CharacterVisuals>();
        if (visuals == null) visuals = gameObject.AddComponent<CharacterVisuals>();
    }

    private void Update()
    {
        if (IsDead) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            CombatManager.Instance.AddEnemyAction(this, Mathf.RoundToInt(AttackPower), IsMagic);
            
            // Formation-based frequency
            float effectiveInterval = AttackInterval;
            if (!IsMagic && CombatManager.Instance != null)
            {
                float factor = (CombatManager.Instance.balanceData != null) ? 
                    CombatManager.Instance.balanceData.FormationPenaltyFactor : 0.75f;
                
                int index = CombatManager.Instance.ActiveEnemies.IndexOf(this);
                if (index > 0)
                {
                    // Back row enemies attack slower: Interval / factor^index
                    effectiveInterval /= Mathf.Pow(factor, index);
                }
            }
timer = effectiveInterval;
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
        
        CombatManager.Instance.ShowCombatNumber(damage, Color.red, transform.position);

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
        
        // Grant XP to player
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.AddExperience(5 * Level);
        }

        Destroy(gameObject, 0.1f);
    }
}
