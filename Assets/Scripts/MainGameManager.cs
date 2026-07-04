using UnityEngine;

public class MainGameManager : MonoBehaviour
{
    public static MainGameManager Instance;

    public enum GameState
    {
        SetUp,
        Phase1,
        Phase2,
        GameEnd
    }
    public GameState _state;

    public ScoreData _scoreData;
    
    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        
    }

    void Update()
    {
        switch (_state)
        {
            case GameState.SetUp:
                break;
            case GameState.Phase1:
                break;
            case GameState.Phase2:
                break;
            case GameState.GameEnd:
                break;
            default:
                break;
        }
    }
    /// <summary>
    /// スコアの加算を行う。
    /// 引数 : 加算するスコア
    /// </summary>
    /// <param name="a_amount">加算するスコア</param>
    public void AddScore(float a_amount)
    {
        _scoreData.score += a_amount;
    }
}
