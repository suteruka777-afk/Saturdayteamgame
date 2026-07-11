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
        GameObject managerObj = GameObject.Find("MainGameManager");

        if (managerObj != null)
        {
            MainGameManager manager = managerObj.GetComponent<MainGameManager>();

            if (manager != null)
            {
                manager.ToResult();
            }
            else
            {
                Debug.LogError("MainGameManager オブジェクトに MainGameManager スクリプトが見つかりません。");
            }
        }
        else
        {
            Debug.LogError("ヒエラルキー上に 'MainGameManager' という名前のオブジェクトが見つかりません。");
        }

        Destroy(gameObject);
    }
}