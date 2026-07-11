using UnityEngine;
using UnityEngine.SceneManagement;
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
    [SerializeField] private ResutlPerformController _performController;

    void Start()
    {
        UpdateText();

        _performController.Init();
        StartCoroutine(_performController.Play(_scoreData.isClear));
    }

    private void UpdateText()
    {
        _clearLb.text = _scoreData.isClear ? "Clear" : "GameOver";
        _scoreText.text = "Score : " + _scoreData.score.ToString("N0");

        int minutes = Mathf.FloorToInt(_scoreData.timer / 60f);
        int seconds = Mathf.FloorToInt(_scoreData.timer % 60f);
        int millis = Mathf.FloorToInt(_scoreData.timer % 1000f);

        _timeText.text = $"ClearTime : {minutes:00}:{seconds:00}.{millis:000}";
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
