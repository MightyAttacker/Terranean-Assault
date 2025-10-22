using UnityEngine;

public class UnitHealth : MonoBehaviour  //Author - Karl Martinez-Benham
{
    public int maxHealth = 10;
    public int currentHealth;

    public Transform healthBar; // drag green bar here
    private bool isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
    
        currentHealth -= amount;
    
        if (healthBar != null)
        {
            float scaleX = Mathf.Max(0, (float)currentHealth / maxHealth);
            healthBar.localScale = new Vector3(scaleX * 0.5f, 0.05f, 0.05f);
            healthBar.localPosition = new Vector3(healthBar.localPosition.x, -0.45f, healthBar.localPosition.z);
        }
    
        if (currentHealth <= 0)
        {
            isDead = true;
            Destroy(gameObject);
        }
    }
}
