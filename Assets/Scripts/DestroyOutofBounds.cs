using UnityEngine;

public class DestroyOutofBounds : MonoBehaviour
{
    // Á‹‚·‚é‰ºŒÀ‚ÌYÀ•W
    private float lowerBound = -6.0f;

    void Update()
    {
        // ‚à‚µ©•ª‚ÌYÀ•W‚ª‰ºŒÀ‚ğ‰º‰ñ‚Á‚½‚ç
        if (transform.position.y < lowerBound)
        {
            // ©•ª©g‚ğÁ‹
            Destroy(gameObject);
        }
    }
}