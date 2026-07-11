using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float deceleration = 8f;

    //public Sprite sideSprite;
    //public Sprite backSprite;
    //public Sprite frontSprite;

    private SpriteRenderer sr;
    private Vector2 inputDir = Vector2.zero;
    private Vector2 velocity = Vector2.zero;
    private Rigidbody2D rb;
    public GameObject dangerObject;



    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        if (inputDir.magnitude > 0.1f)
        {
            velocity = inputDir * maxSpeed;
        }
        else
        {
            velocity = Vector2.MoveTowards(
                velocity,
                Vector2.zero,
                deceleration * Time.fixedDeltaTime
            );
        }

        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);

      //  UpdateDirectionSprite();
    }

    //  void UpdateDirectionSprite()
    //  {
    //      float x = inputDir.x;
    //      float y = inputDir.y;

    //      if (x > 0)
    //    {
    //      sr.sprite = sideSprite;
    //         sr.flipX = true;   // ‰EŒü‚«
    //     }
    //     else if (x < 0)
    //     {
    //         sr.sprite = sideSprite;
    //         sr.flipX = false;  // ¶Œü‚«
    //     }
    //     else if (y > 0)
    //     {
    //         sr.sprite = backSprite;
    //     }
    //     else if (y < 0)
    //     {
    //         sr.sprite = frontSprite;
    //     }
    // }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == dangerObject)
        {
            Debug.Log("‚â‚Î‚¢I");
        }
    }



    public void OnMovePlayer(InputAction.CallbackContext context)
    {
        inputDir = context.ReadValue<Vector2>();
    }
}
