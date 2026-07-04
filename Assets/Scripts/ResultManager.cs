using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _clearLb;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private ScoreData _scoreData;

    void Start()
    {
        UpdateText();
    }

    void Update()
    {
        
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
        SceneManager.LoadScene("MainScene");
    }
}
