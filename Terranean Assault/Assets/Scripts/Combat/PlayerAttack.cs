using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public LayerMask enemyLayer;     // Assign layer in Inspector
    public Hotbar hotbar;            // Assign Hotbar reference in Inspector

    public int damage = 5;           // Damage amount
    public float attackRange = 1f;   // Distance to hit units

    public bool isAttacker = true;   // true = attacker, false = defender

    void Update()
    {
        if (Input.GetMouseButtonDown(1) && IsCorrectPhase())
        {
            Attack();
        }
    }

    bool IsCorrectPhase() =>
        (isAttacker && (hotbar.phase - 3) % 4 == 0) ||
        (!isAttacker && (hotbar.phase - 5) % 4 == 0);

    void Attack()
    {
        Collider2D[] hitUnits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);

        foreach (Collider2D unitCollider in hitUnits)
        {
            if (unitCollider.TryGetComponent<UnitHealth>(out UnitHealth health))
            {
                health.TakeDamage(damage);
                Debug.Log("Hit unit: " + unitCollider.name);
            }
        }
    }
}
