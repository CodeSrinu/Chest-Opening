using UnityEngine;
using UnityEngine.UI;
public class PlayerHealth: MonoBehaviour
{   
    public Slider Healthbar;
    public GameObject GameoverPanel;
    public ChestMovement player;
    public int damagePerAttack = 30;
    int health = 100;
    void Start()
    {
        Healthbar.maxValue = 100;
        Healthbar.value = health;
        GameoverPanel.SetActive(false);
    }
    public void TakeDamage()
    {
        health -= damagePerAttack;
        Healthbar.value = health;
        if (health <= 0)
        {
            player.enabled = false;
            GameoverPanel.SetActive(true);

        }
    }
}

