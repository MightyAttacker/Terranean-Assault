using UnityEngine;
using System.Collections.Generic;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("Which layer contains enemy units.")]
    public LayerMask enemyLayer;

    [Tooltip("Reference to the Hotbar for phase tracking.")]
    public Hotbar hotbar;

    [Tooltip("Attack range in world units.")]
    public float attackRange = 1f;

    [Tooltip("Set true if this unit belongs to the attacker team.")]
    public bool isAttacker = true;

    [Tooltip("Enable to visualize the attack range when clicking.")]
    public bool showAttackRangeGizmo = true;

    // ✅ Phase arrays (for fight phases only)
    private readonly int[] attackerFightPhases = { 3, 7, 11, 15, 19 };
    private readonly int[] defenderFightPhases = { 5, 9, 13, 17, 21 };

    private void Update()
    {
        if (Input.GetMouseButtonDown(1)) // Right-click
        {
            if (IsCorrectPhase())
            {
                Attack();
            }
            else
            {
                Debug.Log("❌ Not the correct phase to attack!");
            }
        }
    }

    // ✅ Checks whether it's currently a valid attack phase
    private bool IsCorrectPhase()
    {
        if (hotbar == null)
        {
            Debug.LogWarning("⚠️ Hotbar reference missing on PlayerAttack!");
            return false;
        }

        if (isAttacker)
            return System.Array.Exists(attackerFightPhases, p => p == hotbar.phase);
        else
            return System.Array.Exists(defenderFightPhases, p => p == hotbar.phase);
    }

    // ✅ Performs melee attack
    private void Attack()
    {
        if (!TryGetComponent<CharacterPathfindingMovementHandler>(out var movementHandler))
            return;

        int damage = Mathf.RoundToInt(movementHandler.MeleeDamage);

        // Find all colliders in attack range
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);
        HashSet<GameObject> hitObjects = new HashSet<GameObject>();

        foreach (Collider2D col in hitColliders)
        {
            GameObject target = col.gameObject;
            if (hitObjects.Contains(target)) continue;

            bool isEnemy = target.CompareTag(isAttacker ? hotbar.defenderTag : hotbar.attackerTag);
            if (!isEnemy) continue;

            if (target.TryGetComponent<UnitHealth>(out UnitHealth health))
            {
                health.TakeDamage(damage);
                Debug.Log($"🗡️ {name} hit {target.name} for {damage} damage");
                hitObjects.Add(target);
            }
        }
    }
}
