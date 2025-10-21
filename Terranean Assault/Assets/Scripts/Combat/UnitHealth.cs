using UnityEngine;

public class UnitHealth : MonoBehaviour
{
    public int maxHealth = 10;
    public int currentHealth;

    public Transform healthBar; // drag green bar here
    private bool isDead = false;
    private float fullHealthBarWidth;

    void Awake()
    {
        currentHealth = maxHealth;

        if (healthBar != null)
            fullHealthBarWidth = healthBar.localScale.x; // starting width
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        if (healthBar != null)
        {
            float scaleX = Mathf.Max(0, (float)currentHealth / maxHealth) * fullHealthBarWidth;
            healthBar.localScale = new Vector3(scaleX, healthBar.localScale.y, healthBar.localScale.z);
            healthBar.localPosition = new Vector3(healthBar.localPosition.x, -0.45f, healthBar.localPosition.z);
        }

        if (currentHealth <= 0)
        {
            isDead = true;
            Destroy(gameObject);
        }
    }
}
