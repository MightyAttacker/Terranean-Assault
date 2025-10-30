using UnityEngine;
using System.Collections.Generic;

public class PlayerAttack : MonoBehaviour
{
    public LayerMask enemyLayer;
    public Hotbar hotbar;
    public float attackRange = 1f;
    public bool isAttacker = true;

    // Phase arrays
    int[] attackerFightPhases = { 2, 6, 10, 14, 18 };
    int[] defenderFightPhases = { 4, 8, 12, 16, 20 };

    // void Update()
    // {
    //     if (Input.GetMouseButtonDown(1) && IsCorrectPhase())
    //     {
    //         Attack();
    //     }
    // }

    // bool IsCorrectPhase()
    // {
    //     if (isAttacker)
    //         return System.Array.Exists(attackerFightPhases, p => p == hotbar.phase);
    //     else
    //         return System.Array.Exists(defenderFightPhases, p => p == hotbar.phase);
    // }

    void Attack()
    {
        if (!TryGetComponent<CharacterPathfindingMovementHandler>(out var movementHandler))
            return;

        int actualDamage = Mathf.RoundToInt(movementHandler.MeleeDamage);

        Collider2D[] hitUnits = Physics2D.OverlapCircleAll(transform.position, attackRange);

        HashSet<GameObject> hitObjects = new HashSet<GameObject>();

        foreach (Collider2D unitCollider in hitUnits)
        {
            GameObject unitObj = unitCollider.gameObject;

            if (hitObjects.Contains(unitObj)) 
                continue; // Already processed this unit

            if (!unitObj.CompareTag(isAttacker ? hotbar.defenderTag : hotbar.attackerTag))
                continue;

            if (unitObj.TryGetComponent<UnitHealth>(out UnitHealth health))
            {
                health.TakeDamage(actualDamage);
                Debug.Log($"Hit unit: {unitObj.name} for {actualDamage} damage");
                hitObjects.Add(unitObj);
            }
        }
    }
}
