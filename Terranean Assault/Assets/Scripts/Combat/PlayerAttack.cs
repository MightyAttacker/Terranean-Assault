using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public Hotbar hotbar;

    public float CloseRange = 1f;   // Melee / close range
    public float LongRange = 3f;    // Long range, adjustable per unit

    public bool isAttacker = true;
    public AttackRangeVisual attackVisual;

    private int lastPhase = -1;
    private bool hasAttackedThisPhase = false;
    private int selectedAttackType = 0; // 0 = none, 1 = close, 2 = long

    // Phase arrays (for fight phases only)
    private readonly int[] attackerFightPhases = { 3, 7, 11, 15, 19 };
    private readonly int[] defenderFightPhases = { 5, 9, 13, 17, 21 };

    // Added property to satisfy Testing.cs
    public float attackRange
    {
        get
        {
            return selectedAttackType switch
            {
                1 => CloseRange,
                2 => LongRange,
                _ => 1f // default if no attack type selected
            };
        }
    }

    private void Update()
    {
        // Reset attack availability when phase changes
        if (hotbar.phase != lastPhase)
        {
            hasAttackedThisPhase = false;
            selectedAttackType = 0;
            lastPhase = hotbar.phase;
            attackVisual?.ClearHighlights();
        }

        if (!IsCorrectPhase() || hasAttackedThisPhase) return;

        // Select attack type
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedAttackType = 1;
            attackVisual?.ShowCloseRangeAttack(transform.position, Mathf.RoundToInt(CloseRange));
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedAttackType = 2;
            attackVisual?.ShowLongRangeAttack(transform.position, Mathf.RoundToInt(LongRange));
        }

        // Perform attack on right-click
        if (selectedAttackType != 0 && Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;

            // Raycast without layer filtering
            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);

            if (hit.collider != null)
            {
                GameObject target = hit.collider.gameObject;

                // Only attack valid enemy units
                if (target.TryGetComponent<UnitHealth>(out _) &&
                    target.CompareTag(isAttacker ? hotbar.defenderTag : hotbar.attackerTag))
                {
                    Attack(target);
                    hasAttackedThisPhase = true;
                    attackVisual?.ClearHighlights();
                    selectedAttackType = 0;
                }
            }
        }
    }

    private bool IsCorrectPhase()
    {
        if (hotbar == null) return false;
        if (isAttacker)
            return System.Array.Exists(attackerFightPhases, p => p == hotbar.phase);
        else
            return System.Array.Exists(defenderFightPhases, p => p == hotbar.phase);
    }

    private void Attack(GameObject target)
    {
        if (!TryGetComponent<CharacterPathfindingMovementHandler>(out var movementHandler)) return;

        int damage = Mathf.RoundToInt(movementHandler.MeleeDamage); // Can extend for ranged attacks

        if (target.TryGetComponent<UnitHealth>(out UnitHealth health))
        {
            health.TakeDamage(damage);
            Debug.Log($"🗡️ {name} hit {target.name} for {damage} damage");
        }
    }
}
