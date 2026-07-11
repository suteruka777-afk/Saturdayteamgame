using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Alien_move : MonoBehaviour
{
    [Header("Speed & Limits")]
    public float moveSpeed = 4f;
    public float leftLimit = -6f;
    public float rightLimit = 6f;

    [Header("★Settings")]
    public float entryTargetY = 4.4f;
    public float entrySpeed = 3f;

    [Header("★StoppingTime")]
    public float freezeDuration = 0.5f;

    private bool isEntering = true;
    private bool isFreezing = false; 
    private bool movingLeft = true;

    [Header("Attack")]
    public GameObject bulletPrefab;
    public float shotIntervalInMove = 0.8f;
    private float moveShotTimer = 0f;


    IEnumerator FreezeGameTime()
    {
        isFreezing = true;   
        Time.timeScale = 0f; 

        yield return new WaitForSecondsRealtime(freezeDuration);

        Time.timeScale = 1f;
        isFreezing = false;  
    }

    void Update()
    {
        if (isFreezing) return;

        if (isEntering)
        {
            HandleEntry();
            return;
        }

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

    void HandleEntry()
    {
        Vector3 targetPosition = new Vector3(transform.position.x, entryTargetY, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, entrySpeed * Time.deltaTime);

        if (Mathf.Abs(transform.position.y - entryTargetY) < 0.05f)
        {
            transform.position = targetPosition;
            isEntering = false;

            StartCoroutine(FreezeGameTime());
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