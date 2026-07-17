using UnityEngine;

public class MeteorSpawner : MonoBehaviour
{
    [Header("隕石のプレハブ")]
    public GameObject meteorPrefab;

    [Header("生成する間隔（秒）")]
    public float spawnInterval = 1.0f;

    [Header("生成するX座標の範囲")]
    public float minX = -8.0f;
    public float maxX = 8.0f;

    [Header("生成するY座標（高さ）")]
    public float spawnY = 6.0f;

    private float timer;

    void Update()
    {
        // 時間を計測
        timer += Time.deltaTime;

        // 設定した間隔を超えたら隕石を生成
        if (timer >= spawnInterval)
        {
            SpawnMeteor();
            timer = 0f; // タイマーをリセット
        }
    }

    void SpawnMeteor()
    {
        // X座標をランダムに決定
        float randomX = Random.Range(minX, maxX);

        // 生成する位置を設定
        Vector3 spawnPosition = new Vector3(randomX, spawnY, 0);

        // 隕石を生成（インスタンス化）
        Instantiate(meteorPrefab, spawnPosition, Quaternion.identity);
    }
}