using UnityEngine;

[CreateAssetMenu(fileName = "ScoreData", menuName = "Scriptable Objects/ScoreData")]
public class ScoreData : ScriptableObject
{
    [Header("スコア関係の数値")]
    public float score;//スコア
    public int KillCount;//撃破数
    public float stageProgress;//ステージの進捗率
    public bool isClear;

    public void SetUp()
    {
        score = 0f;
        stageProgress = 0f;
        isClear = false;
    }
}
