using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

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
        Init();
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

    private void Init()
    {
        _state = GameState.SetUp;
        _scoreData.SetUp();
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
    /// <summary>
    /// リザルトへの遷移
    /// 引数 : クリアか否か
    /// </summary>
    /// <param name="isClear">クリアしたかどうか</param>
    public void ToResult(bool a_isClear = false)
    {
        _scoreData.isClear = a_isClear;
        SceneManager.LoadScene("ResultScene");
    }
}
