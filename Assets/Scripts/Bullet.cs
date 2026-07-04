using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 5f;

    private Vector2 moveDirection;

    public void SetDirection(Vector2 dir)
    {
        moveDirection = dir.normalized;
    }

    void Update()
    {
        transform.position +=
            (Vector3)moveDirection * speed * Time.deltaTime;
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
