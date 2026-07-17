using UnityEngine;

public class enemy_Bullet : MonoBehaviour
{
    [Header("Speed")]
    public float bulletSpeed = 4.5f;

    void Update()
    {
        // 毎フレーム、下方向（Vector3.down）へ移動する
        transform.Translate(Vector3.down * bulletSpeed * Time.deltaTime);

        // 画面外（画面下部）に出たら自分自身を削除する
        if (transform.position.y < -6.0f)
        {
            Destroy(gameObject);
        }
    }
}