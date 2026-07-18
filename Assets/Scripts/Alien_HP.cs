using UnityEngine;
using UnityEngine.SceneManagement;

public class Alien_HP : MonoBehaviour
{
    [Header("HP")]
    public int maxHp = 4;
    private int currentHp;

    [Header("References")]
    public MainGameManager gameManager;
    
    [SerializeField] private ScoreData _data;
    [SerializeField] private float _addscore = 300f;

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

            _data.score += _addscore;

            if (currentHp <= 0)
            {
                EnemyDie();
            }
        }
    }

    void EnemyDie()
    {
        _data.score += _addscore * 10f;
        if (gameManager != null)
        {
            gameManager.ToResult(true);
            Debug.Log("a");
        }
        else
        {
            MainGameManager manager = Object.FindAnyObjectByType<MainGameManager>();
            if (manager != null)
            {
                manager.ToResult(true);
                Debug.Log("b");
            }
        }

        Destroy(gameObject);
    }
}