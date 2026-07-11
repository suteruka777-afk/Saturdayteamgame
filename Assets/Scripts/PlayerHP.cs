using UnityEngine;

public class PlayerHP : MonoBehaviour
{
    [SerializeField] private int hp = 5;  // Inspector Ç©ÇÁïœçXÇ≈Ç´ÇÈ

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null || collision.gameObject == null)
            return;

        if (collision.gameObject.name.Contains("Circle(Clone)"))
        {
            hp--;
            Debug.Log("HP: " + hp);

            if (hp <= 0)
            {
                Debug.Log("Ç‹Ç∂Ç‚ÇŒÇ¢ÅI");
            }
        }
    }
}