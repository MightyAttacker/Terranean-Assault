using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public int damage = 5;           // Damage amount
    public float attackRange = 1f;   // Distance to hit enemy
    public LayerMask enemyLayer;     // Assign Enemy layer in Inspector

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))  // Attack key
        {
            Attack();
        }
    }

    void Attack()
    {
        // Detect enemies in range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            Enemy enemy = enemyCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Damage(damage);
                Debug.Log("Hit enemy: " + enemy.name);
            }
        }
    }
}
