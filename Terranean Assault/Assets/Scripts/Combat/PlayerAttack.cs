using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public LayerMask enemyLayer;
    public float CloseRange = 1f;
    public float LongRange = 3f;
    public AttackRangeVisual attackVisual;

    private int selectedAttackType = 0; // 0 = none, 1 = melee, 2 = long
    private Testing testing;

    public float attackRange => selectedAttackType switch
    {
        1 => CloseRange,
        2 => LongRange,
        _ => 0f
    };

    private void Start()
    {
        attackVisual?.SetGrid(Pathfinding.Instance.GetGrid());
        testing = FindObjectOfType<Testing>();
    }

    private void Update()
    {
        if (testing == null) return;

        // Only show range for currently selected unit
        bool isSelected = testing.selectedCharacter?.gameObject == gameObject;

        if (!isSelected)
        {
            // Clear this unit's highlights only if it’s deselected
            attackVisual?.ClearHighlights();
            return;
        }

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

            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero, Mathf.Infinity, enemyLayer);
            if (hit.collider != null && hit.collider.TryGetComponent<UnitHealth>(out _))
            {
                Attack(hit.collider.gameObject);
                attackVisual?.ClearHighlights();
                selectedAttackType = 0;
            }
        }
    }

    private void Attack(GameObject target)
    {
        if (!TryGetComponent<CharacterPathfindingMovementHandler>(out var movementHandler)) return;

        int damage = Mathf.RoundToInt(movementHandler.MeleeDamage);

        if (target.TryGetComponent<UnitHealth>(out UnitHealth health))
        {
            health.TakeDamage(damage);
            Debug.Log($"🗡️ {name} hit {target.name} for {damage} damage");
        }
    }
}
