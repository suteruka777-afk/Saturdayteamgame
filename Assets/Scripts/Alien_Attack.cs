using UnityEngine;

public class Enemy3 : MonoBehaviour
{
    [Header("Bullet Prefab")]
    public GameObject bulletPrefab;
    [Header("Shot Interval (seconds)")]
    public float shotInterval = 0.5f;
    private float timer = 0.0f;
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= shotInterval)
        {
            Shot();
            timer = 0.0f;
        }
    }

    void Shot()
    {
        if (bulletPrefab != null)
        {
            Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y, 0f);

            //1.
            Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

            // 2.
            Quaternion leftRotation = Quaternion.Euler(0, 0, 50f);
            Instantiate(bulletPrefab, spawnPosition, leftRotation);

            // 3.
            Quaternion rightRotation = Quaternion.Euler(0, 0, -50f);
            Instantiate(bulletPrefab, spawnPosition, rightRotation);
        }
    }
}
