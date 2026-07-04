using UnityEngine;

[CreateAssetMenu(fileName = "ScoreData", menuName = "Scriptable Objects/ScoreData")]
public class ScoreData : ScriptableObject
{
    public float score;//スコア
    public int KillCount;//撃破数
    public float stageProgress;//ステージの進捗率

    public void SetUp()
    {
        score = 0f;
        stageProgress = 0f;
    }
}
