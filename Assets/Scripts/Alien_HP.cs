using UnityEngine;
using UnityEngine.SceneManagement;

public class Alien_HP : MonoBehaviour
{
    [Header("HP")]
    public int maxHp = 4;
    private int currentHp;

    void Start()
    {
        currentHp = maxHp;
    }

    public void HPOverrite()
    {
        currentHp = 1;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerBullet"))
        {
            currentHp -= 1;

            Destroy(collision.gameObject);

            if (currentHp <= 0)
            {
                EnemyDie();
            }
        }
    }

    void EnemyDie()
    {
        Destroy(gameObject);
    }
}