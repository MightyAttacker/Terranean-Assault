using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public LayerMask enemyLayer;
    public Hotbar hotbar;
    public float attackRange = 1f;
    public bool isAttacker = true;

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
        if (!TryGetComponent<CharacterPathfindingMovementHandler>(out var movementHandler))
            return;

        int actualDamage = Mathf.RoundToInt(movementHandler.MeleeDamage);

        Collider2D[] hitUnits = Physics2D.OverlapCircleAll(transform.position, attackRange);

        foreach (Collider2D unitCollider in hitUnits)
        {
            if (!unitCollider.CompareTag(isAttacker ? hotbar.defenderTag : hotbar.attackerTag))
                continue;

            if (unitCollider.TryGetComponent<UnitHealth>(out UnitHealth health))
            {
                health.TakeDamage(actualDamage);
                Debug.Log($"Hit unit: {unitCollider.name} for {actualDamage} damage");
            }
        }
    }
}
