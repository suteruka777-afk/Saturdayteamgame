using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _killText;
    [SerializeField] private TextMeshProUGUI _progText;
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
        _scoreText.text = "Score : " + _scoreData.score.ToString("N0");
        _killText.text = "Kill : " + _scoreData.KillCount.ToString("N0");
        if(_scoreData.stageProgress > 100f) _scoreData.stageProgress = 100f;
        _progText.text = "Stage : " + _scoreData.stageProgress.ToString("N0") +"%";
    }

    public void ToTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }
    public void ReTry()
    {
        SceneManager.LoadScene("MainGameScene");
    }
}
