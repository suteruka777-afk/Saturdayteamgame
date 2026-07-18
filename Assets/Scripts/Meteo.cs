using UnityEngine;


public class Meteo : BulletController

{
    [SerializeField] private int HP = 3;
    // トリガー（Is Trigger）に何かが触れた瞬間に、Unityが自動でこの関数を呼び出す
    // 「Collider other」には、ぶつかった相手（今回は隕石）の情報が自動で入る
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // もし、ぶつかった相手（other）のタグ（Tag）の名前が「Meteor」だったら…
        if (collision.gameObject.CompareTag("PlayerBullet"))
        {
            HP--;
            if (HP < 0)
            {
                // other（隕石）のゲームオブジェクトをこの世（ゲーム内）から消滅させる
                Destroy(collision.gameObject);

                // gameObject（このスクリプトがついている自分自身＝弾）を消滅させる
                Destroy(gameObject);
            }
        }
    }
}
