using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float deceleration = 0.0f;
    public Transform bulletSpawnPoint;


    //public Sprite sideSprite;
    //public Sprite backSprite;
    //public Sprite frontSprite;

    private SpriteRenderer sr;
    private Vector2 inputDir = Vector2.zero;
    private Vector2 velocity = Vector2.zero;
    private Rigidbody2D rb;
    public GameObject dangerObject;

    void Update()
    {
        if (Time.timeScale == 0f) return;

    }

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

    }


    public void OnMovePlayer(InputAction.CallbackContext context)
    {
        inputDir = context.ReadValue<Vector2>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name.Contains("Circle(Clone)"))
        {
            Debug.Log("やばい！");
        }
    }

    public GameObject playerBulletPrefab;   // PlayerBullet を入れる
    public float bulletSpeed = 10f;

    // Input System の Fire アクションから呼ばれる
    private void OnFire(InputAction.CallbackContext context)
    {
        GameObject bullet = Instantiate(
            playerBulletPrefab,
            bulletSpawnPoint.position,   // ← ここがポイント！
            Quaternion.identity
        );

        Rigidbody2D rbBullet = bullet.GetComponent<Rigidbody2D>();
        if (rbBullet != null)
            rbBullet.linearVelocity = Vector2.up * bulletSpeed;
    }




}