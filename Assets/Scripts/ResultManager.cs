using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class ResultManager : MonoBehaviour
{
    [Header("テキスト")]
    [SerializeField] private TextMeshProUGUI _clearLb;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _timeText;

    [Header("データ")]
    [SerializeField] private ScoreData _scoreData;

    [Header("演出")]
    [SerializeField] private ResultPerformController _performController;

    [Header("入力")]
    [Tooltip("演出をスキップするための入力アクション(InputSystem_Actions の Player/Skip)")]
    [SerializeField] private InputActionReference _skipAction;

    void Start()
    {
        UpdateText();

        _performController.Init();
        StartCoroutine(_performController.Play(_scoreData.isClear));
    }

    void OnEnable()
    {
        if (_skipAction == null)
        {
            return;
        }

        _skipAction.action.Enable();
        _skipAction.action.performed += OnSkipPerformed;
    }

    void OnDisable()
    {
        if (_skipAction == null)
        {
            return;
        }

        _skipAction.action.performed -= OnSkipPerformed;
        _skipAction.action.Disable();
    }

    private void OnSkipPerformed(InputAction.CallbackContext a_context)
    {
        _performController.OnSkip();
    }

    private void UpdateText()
    {
        _clearLb.text = _scoreData.isClear ? "Clear" : "GameOver";
        _scoreText.text = "Score : " + _scoreData.score.ToString("N0");

        int minutes = Mathf.FloorToInt(_scoreData.timer / 60f);
        int seconds = Mathf.FloorToInt(_scoreData.timer % 60f);
        int millis = Mathf.FloorToInt((_scoreData.timer % 1f) * 1000f);

        string timeLabel = _scoreData.isClear ? "ClearTime" : "SurvTime";
        _timeText.text = $"{timeLabel} : {minutes:00}:{seconds:00}.{millis:000}";
    }

    public void ToTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }
    public void ReTry()
    {
        SceneManager.LoadScene("tanakayuta");
    }
}
