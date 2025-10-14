using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health = 10;

    // Get position of the enemy
    public Vector3 GetPosition()
    {
        return transform.position;
    }

    // Reduce health by damage amount
    public void Damage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
