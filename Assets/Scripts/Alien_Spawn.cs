using UnityEngine;


public class SingleEnemySpawner : MonoBehaviour
{
    [Header("生成する敵のプレハブ")]
    public GameObject Alien;

    [Header("起動してから生成されるまでの時間（秒）")]
    public float spawnDelay = 3.0f;

    void Start()
    {
        Invoke("SpawnEnemy", spawnDelay);
    }

    void SpawnEnemy()
    {
        if (Alien != null)
        {
            Instantiate(Alien, transform.position, transform.rotation);
        }
    }
}