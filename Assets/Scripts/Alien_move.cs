using UnityEngine;
using UnityEngine.SceneManagement;

public class Alien_move : MonoBehaviour
{
    [Header("Speed & Limits")]
    public float moveSpeed = 4f;
    public float leftLimit = -6f;
    public float rightLimit = 6f;

    private bool movingLeft = true;

    [Header("Attack")]
    public GameObject bulletPrefab;
    public float shotIntervalInMove = 0.8f;
    private float moveShotTimer = 0f;

    void Update()
    {
        float step = moveSpeed * Time.deltaTime;

        moveShotTimer += Time.deltaTime;
        if (moveShotTimer >= shotIntervalInMove)
        {
            SpawnStraightBullet();
            moveShotTimer = 0f;
        }

        if (movingLeft)
        {
            transform.Translate(Vector3.left * step);
            if (transform.position.x <= leftLimit)
            {
                movingLeft = false; 
            }
        }
        else
        {
            transform.Translate(Vector3.right * step);
            if (transform.position.x >= rightLimit)
            {
                movingLeft = true;
            }
        }
    }

    void SpawnStraightBullet()
    {
        if (bulletPrefab == null) return;

        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.down * 5f;
        }
    }
}