using UnityEngine;

public class BulletController : MonoBehaviour
{
    void Update()
    {
        // ‰æ–ÊŠO‚Éo‚½‚çÁ‚·
        if (transform.position.y > 6f || transform.position.y < -6f ||
            transform.position.x > 10f || transform.position.x < -10f)
        {
            Destroy(gameObject);
        }
    }
}
