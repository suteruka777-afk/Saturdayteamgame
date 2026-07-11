using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float deceleration = 0.0f;

    public Transform bulletSpawnPoint;   // 弾の発射位置
    public GameObject playerBulletPrefab;
    public float bulletSpeed = 10f;

    // ★ インスペクターには出さず、コード内で完結させる
    private InputAction moveAction;
    private InputAction fireAction;

    private Vector2 inputDir = Vector2.zero;
    private Vector2 velocity = Vector2.zero;

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // ==========================================
        // ★ コード側で入力をゼロから構築する
        // ==========================================

        // 1. 移動アクションの設定（Vector2型）
        moveAction = new InputAction(
            name: "Move",
            type: InputActionType.Value,
            expectedControlType: "Vector2"
        );
        // キーボード（WASD）のバインディング（コンポジット）
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        // コントローラー（左スティック）のバインディング
        moveAction.AddBinding("<Gamepad>/leftStick");

        // 2. 攻撃アクションの設定（Button型）
        fireAction = new InputAction(
            name: "Fire",
            type: InputActionType.Button
        );
        // キーボード（スペースキー）のバインディング
        fireAction.AddBinding("<Keyboard>/space");
        // コントローラー（右側の決定ボタン系：PSでいう✕、XboxでいうAなど）のバインディング
        fireAction.AddBinding("<Gamepad>/buttonSouth");
    }

    private void OnEnable()
    {
        // アクションの有効化
        moveAction.Enable();
        fireAction.Enable();

        // イベントの登録
        fireAction.started += OnFire;
    }

    private void OnDisable()
    {
        // イベントの解除と無効化
        fireAction.started -= OnFire;

        moveAction.Disable();
        fireAction.Disable();
    }

    void FixedUpdate()
    {
        // 移動入力の読み取り
        inputDir = moveAction.ReadValue<Vector2>();

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

    private void OnFire(InputAction.CallbackContext context)
    {
        GameObject bullet = Instantiate(
            playerBulletPrefab,
            bulletSpawnPoint.position,
            Quaternion.identity
        );

        Rigidbody2D rbBullet = bullet.GetComponent<Rigidbody2D>();
        if (rbBullet != null)
            rbBullet.linearVelocity = Vector2.up * bulletSpeed;
    }
}