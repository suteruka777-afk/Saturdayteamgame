using UnityEngine;

public class PlayerHPManager : MonoBehaviour
{
    [SerializeField] private int _maxHP = 3;
    private int _currentHP;

    private void Start()
    {
        _currentHP = _maxHP;
    }

    private void OnTriggerEnter2D(Collider2D collision) 
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            _TakeDamage(1);
        }
    }

    private void _TakeDamage(int damage)
    {
        _currentHP -= damage;
        _currentHP = Mathf.Clamp(_currentHP, 0, _maxHP);

        Debug.Log("現在のHP: " + _currentHP);

        if (_currentHP <= 0)
        {
            _GameOver();
        }
    }

    private void _GameOver()
    {
        Debug.Log("ゲームオーバー判定");

        // シーン内からMainManagerを探してスクリプトを取得
        MainGameManager mainManager = FindAnyObjectByType<MainGameManager>();

        if (mainManager != null)
        {
            // 引数なしで呼び出す
            mainManager.ToResult();

        }
    }
}