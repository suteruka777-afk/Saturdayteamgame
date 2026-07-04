using UnityEngine;

[CreateAssetMenu(fileName = "ScoreData", menuName = "Scriptable Objects/ScoreData")]
public class ScoreData : ScriptableObject
{
    [Header("スコア関係の数値")]
    public float score;//スコア
    public float timer;//クリアタイム計測
    public bool isClear;

    public void SetUp()
    {
        score = 0f;
        timer = 0f;
        isClear = false;
    }
}
