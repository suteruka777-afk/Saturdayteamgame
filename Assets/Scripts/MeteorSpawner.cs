using UnityEngine;

public class MeteorSpawner : MonoBehaviour
{
    public GameObject meteorPrefab;

    public float spawnTime = 1.5f;

    void Start()
    {
        InvokeRepeating("SpawnMeteor", 1f, spawnTime);
    }

    void SpawnMeteor()
    {
        float x = Random.Range(-7f, 7f);

        Vector2 pos = new Vector2(x, 6);

        Instantiate(meteorPrefab, pos, Quaternion.identity);
    }
}