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

    // Phase arrays (for fight phases only)
    private readonly int[] attackerFightPhases = { 3, 7, 11, 15, 19 };
    private readonly int[] defenderFightPhases = { 5, 9, 13, 17, 21 };

    private void Update()
    {
        if (Input.GetMouseButtonDown(1)) // Right-click
        {
            if (!IsCorrectPhase()) return;

            // Raycast to find clicked enemy
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;

            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);
            if (hit.collider != null)
            {
                GameObject target = hit.collider.gameObject;

                // Make sure the target has UnitHealth
                if (target.TryGetComponent<UnitHealth>(out _))
                {
                    Attack(target); // Call new method
                }
            }
        }
    }


    // Checks whether it's currently a valid attack phase
    private bool IsCorrectPhase()
    {
        if (hotbar == null)
        {
            return false;
        }

        if (isAttacker)
            return System.Array.Exists(attackerFightPhases, p => p == hotbar.phase);
        else
            return System.Array.Exists(defenderFightPhases, p => p == hotbar.phase);
    }

    // Performs melee attack
    public void Attack(GameObject target)
    {
        if (!TryGetComponent<CharacterPathfindingMovementHandler>(out var movementHandler))
            return;

        // Make sure the target is an enemy
        bool isEnemy = target.CompareTag(isAttacker ? hotbar.defenderTag : hotbar.attackerTag);
        if (!isEnemy) return;

        int damage = Mathf.RoundToInt(movementHandler.MeleeDamage);

        if (target.TryGetComponent<UnitHealth>(out UnitHealth health))
        {
            health.TakeDamage(damage);
            Debug.Log($"🗡️ {name} hit {target.name} for {damage} damage");
        }
    }

}
