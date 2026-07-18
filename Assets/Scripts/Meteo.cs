using UnityEngine;

public class Meteo : BulletController
{
    [SerializeField] private int HP = 3;
    [SerializeField] private float scoreValue = 100f; // 倒したときにもらえるポイント(floatに合わせる)

    // 【追加】ここにUnityのエディタ上でScoreDataのファイルをドラッグ＆ドロップします
    [SerializeField] private ScoreData scoreData;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("PlayerBullet"))
        {
            HP--;

            // 当たった弾は消す
            Destroy(collision.gameObject);

            if (HP <= 0)
            {
                // 【追加】scoreDataがセットされていれば、その中のscoreにポイントを加算
                if (scoreData != null)
                {
                    scoreData.score += scoreValue;
                    Debug.Log("現在のスコア: " + scoreData.score);
                }

                // 自分自身（隕石）を消滅させる
                Destroy(gameObject);
            }
        }
    }
}