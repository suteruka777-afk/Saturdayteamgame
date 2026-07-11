using UnityEngine;

public class Meteor : MonoBehaviour
{
    public float speed = 5f;

    void Update()
    {
        transform.Translate(Vector2.down * speed * Time.deltaTime);

        //‰æ–ÊŠO‚Ü‚Ås‚Á‚½‚çíœ
        if (transform.position.y < -6)
        {
            Destroy(gameObject);
        }
    }
}