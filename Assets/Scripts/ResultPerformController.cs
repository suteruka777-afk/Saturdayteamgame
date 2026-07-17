using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// リザルト画面の演出担当(共通コア)。
/// MonoBehaviourは継承しない。ResultManager (MonoBehaviour) が
/// Init() → StartCoroutine(Play(isClear)) の順で呼び出し、コルーチンを動かす。
///
/// partial class で3ファイルに分割している:
/// - ResultPerformController.cs       … このファイル。共通の参照・状態・進行制御・汎用ヘルパー
/// - ResultPerformController.Clear.cs … クリア(勝利)専用の演出と調整値
/// - ResultPerformController.Lose.cs  … ゲームオーバー(敗北)専用の演出と調整値
/// </summary>
[System.Serializable]
public partial class ResultPerformController
{
    // ===== 共通調整値 =====
    [Header("共通 - ボタン演出 (Retry / Title)")]
    [Tooltip("ボタンがぽわっと広がりながらフェードインする秒数")]
    public float BUTTON_FADE_DURATION = 0.4f;
    [Tooltip("ボタンの出現前のスケール(1.0が等倍)。小さいほど「ぽわっ」と広がる感じが強まる")]
    public float BUTTON_START_SCALE = 0.7f;
    [Tooltip("ボタン出現時に一瞬だけ超える最大スケール(1.0が等倍)")]
    public float BUTTON_POP_OVERSHOOT = 1.08f;

    [Header("共通 - スキップ")]
    [Tooltip("演出開始からこの秒数が経つまでスキップ入力を無視する")]
    public float SKIP_COOLDOWN = 0.5f;

    [Header("共通 - 画面レイアウト")]
    [Tooltip("ロケットを画面外に配置するときの追加余白(px相当)。0だと画面端ぎりぎりで見切れる")]
    public float SCREEN_EDGE_MARGIN = 100f;

    // ===== 共通参照 (勝敗どちらの演出でも使うもの) =====
    [Header("共通 - 参照")]
    [Tooltip("ロケットのRectTransform。移動・拡縮のアニメーション対象")]
    [SerializeField] private RectTransform _rocketRect;
    [Tooltip("ロケットのImage。フェードアウトのアルファ制御に使う")]
    [SerializeField] private Image _rocketImage;
    [Tooltip("GameOver / Clear の見出しテキスト")]
    [SerializeField] private TextMeshProUGUI _labelText;
    [Tooltip("スコア表示テキスト")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [Tooltip("タイム表示テキスト")]
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private Button _btnRetry;
    [SerializeField] private Button _btnTitle;

    // Init() でシーン上の初期配置から算出する値。演出中はこれらを基準にアニメーションする
    private Vector2 _rocketStartPos;
    private Vector3 _rocketStartScale;
    private Quaternion _rocketStartRotation;
    private Vector2 _rocketOffScreenTopPos;
    private Vector2 _rocketOffScreenBottomPos;
    private Vector2 _labelRestPos;

    private CanvasGroup _retryGroup;
    private CanvasGroup _titleGroup;

    private bool _skipRequested;
    private float _playStartTime;

    /// <summary>
    /// 演出開始前の初期化。シーン上の初期配置・画面サイズから各種基準位置を算出する。
    /// Play() より前に必ず1回呼ぶこと。
    /// </summary>
    public void Init()
    {
        _rocketStartPos = _rocketRect.anchoredPosition;
        _rocketStartScale = _rocketRect.localScale;
        _rocketStartRotation = _rocketRect.rotation;
        _rocketOffScreenTopPos = CalculateOffScreenPosition(true);
        _rocketOffScreenBottomPos = CalculateOffScreenPosition(false);

        _labelRestPos = _labelText.rectTransform.anchoredPosition;

        _retryGroup = GetOrAddCanvasGroup(_btnRetry);
        _titleGroup = GetOrAddCanvasGroup(_btnTitle);

        InitClear();
        InitLose();
    }

    /// <summary>
    /// 勝敗に応じたリザルト演出を再生する。ResultManager から StartCoroutine で呼ぶ。
    /// </summary>
    public IEnumerator Play(bool a_isClear)
    {
        _playStartTime = Time.time;
        SetInitialState(a_isClear);

        if (a_isClear)
        {
            yield return RocketFlightRoutine();
            yield return WaitOrSkip(ROCKET_LANDING_PAUSE_DURATION);
            yield return LabelZoomFadeRoutine();
            yield return ItemsFadeRoutine();
        }
        else
        {
            yield return RocketBurnRoutine();
            yield return LabelFallBounceRoutine();
            yield return ItemsPopStaggerRoutine();
        }

        yield return ButtonsFadeInRoutine();

        // スキップの目的は「ボタンが出るまで飛ばす」ことなので、ここでリセットする。
        // 以降の演出(再発射・エイリアン徘徊)は通常再生させる
        _skipRequested = false;

        if (a_isClear)
        {
            // ボタン表示後、一定秒数待ってからロケットが震えて再発射する演出
            yield return RocketRelaunchRoutine();
        }
        else
        {
            // ボタン表示まで終わった後に始まる、終わりのない徘徊演出。リザルト画面を離れる(シーン遷移する)まで動き続ける
            yield return AlienIntroAndWanderRoutine();
        }
    }

    /// <summary>
    /// 演出をすべて飛ばし、完成形(ボタン表示まで)に即座に進める。
    /// 実際のフラグ処理のみで、入力との紐付けは別途行う。
    /// </summary>
    public void OnSkip()
    {
        if (Time.time - _playStartTime < SKIP_COOLDOWN)
        {
            return;
        }

        _skipRequested = true;
    }

    /// <summary>
    /// 勝敗共通の初期状態を整えたうえで、勝敗それぞれの初期配置を各partialファイル側に任せる。
    /// </summary>
    private void SetInitialState(bool a_isClear)
    {
        _rocketRect.gameObject.SetActive(true);
        _rocketRect.localScale = _rocketStartScale;
        _rocketRect.rotation = _rocketStartRotation;
        SetImageAlpha(_rocketImage, 1f);

        // エイリアンと影は敗北/勝利それぞれ専用の要素だが、
        // どちらの演出でも「最初は見えていない」状態から始める必要があるためここで隠す
        if (_alienTransform != null)
        {
            _alienTransform.position = _alienStartPos;
            _alienTransform.localScale = _alienStartScale;
            _alienTransform.gameObject.SetActive(false);
        }

        if (_shadowImage != null)
        {
            SetImageAlpha(_shadowImage, 0f);
        }

        _retryGroup.alpha = 0f;
        _titleGroup.alpha = 0f;
        _retryGroup.interactable = false;
        _titleGroup.interactable = false;
        _retryGroup.blocksRaycasts = false;
        _titleGroup.blocksRaycasts = false;
        _btnRetry.transform.localScale = Vector3.one * BUTTON_START_SCALE;
        _btnTitle.transform.localScale = Vector3.one * BUTTON_START_SCALE;

        if (a_isClear)
        {
            SetInitialStateClear();
        }
        else
        {
            SetInitialStateLose();
        }
    }

    /// <summary>
    /// 画面の上端 / 下端の少し外側にあたる、ロケットのローカル座標(anchoredPosition)を求める。
    /// ロケットの親(Canvas直下)のRectTransform.rectはCanvasScalerの参照解像度基準の
    /// サイズを返すため、解像度によらずこの計算で画面外の位置を算出できる。
    /// </summary>
    private Vector2 CalculateOffScreenPosition(bool a_above)
    {
        RectTransform canvasRect = _rocketRect.parent as RectTransform;
        float halfCanvasHeight = canvasRect != null
            ? canvasRect.rect.height * 0.5f
            : Mathf.Abs(_rocketStartPos.y) + SCREEN_EDGE_MARGIN;
        float rocketHalfHeight = _rocketRect.rect.height * 0.5f * _rocketStartScale.y;

        float y = halfCanvasHeight + rocketHalfHeight + SCREEN_EDGE_MARGIN;
        return new Vector2(_rocketStartPos.x, a_above ? y : -y);
    }

    /// <summary>
    /// 勝敗共通: Retry/Titleボタンがぽわっと広がりながらフェードインする。
    /// </summary>
    private IEnumerator ButtonsFadeInRoutine()
    {
        yield return Tween(BUTTON_FADE_DURATION, t =>
        {
            float scale = t < 0.7f
                ? Mathf.Lerp(BUTTON_START_SCALE, BUTTON_POP_OVERSHOOT, t / 0.7f)
                : Mathf.Lerp(BUTTON_POP_OVERSHOOT, 1f, (t - 0.7f) / 0.3f);
            _retryGroup.alpha = t;
            _titleGroup.alpha = t;
            _btnRetry.transform.localScale = Vector3.one * scale;
            _btnTitle.transform.localScale = Vector3.one * scale;
        });

        _retryGroup.interactable = true;
        _retryGroup.blocksRaycasts = true;
        _titleGroup.interactable = true;
        _titleGroup.blocksRaycasts = true;
    }

    /// <summary>
    /// 0→1に正規化した進行度tを渡しながらa_onUpdateを毎フレーム呼ぶ汎用アニメーションループ。
    /// Skip要求時はその場でt=1相当の状態を1回呼んで即座に抜ける。
    /// </summary>
    private IEnumerator Tween(float a_duration, System.Action<float> a_onUpdate)
    {
        float t = 0f;
        while (t < 1f)
        {
            if (_skipRequested)
            {
                a_onUpdate(1f);
                yield break;
            }

            t += Time.deltaTime / a_duration;
            a_onUpdate(Mathf.Clamp01(t));
            yield return null;
        }

        a_onUpdate(1f);
    }

    /// <summary>
    /// Skip要求が来ない限り指定秒数だけ待つ。演出の合間の「間」に使う。
    /// </summary>
    private IEnumerator WaitOrSkip(float a_seconds)
    {
        float t = 0f;
        while (t < a_seconds)
        {
            if (_skipRequested)
            {
                yield break;
            }

            t += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// UI要素(RectTransform)の画面上の位置を、ParticleSystem等の通常のRendererを
    /// 正しく重ねられるワールド座標に変換する。
    /// Canvasが Screen Space - Overlay の場合、UI階層の中に直接ParticleSystemを置いても
    /// 描画されない(オーバーレイCanvasはカメラを介さず描画され、RectTransform.positionも
    /// 実カメラのワールド座標とは無関係な専用の座標系になるため)。その場合のみ、画面上の
    /// 位置をいったんスクリーン座標に変換してからMain Cameraのワールド座標へ変換し直す。
    /// 一方 Screen Space - Camera / World Space のCanvasでは、RectTransform.positionは
    /// 最初から実カメラ基準の正しいワールド座標になっているため、そのまま使えば良い。
    /// </summary>
    private static Vector3 ConvertUiPositionToWorld(RectTransform a_uiRect)
    {
        Canvas canvas = a_uiRect.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            return a_uiRect.position;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return a_uiRect.position;
        }

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, a_uiRect.position);
        return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
    }

    /// <summary>
    /// パーティクルの生存時間(startLifetime)の最大値を取得する。
    /// エミッタを止めた後、既に出た粒子がプレハブ側の設定通り(フェードアウト等)消え切るまで
    /// 十分待ってからオブジェクトを破棄するための時間算出に使う。
    /// </summary>
    private static float GetMaxStartLifetime(ParticleSystem a_particleSystem)
    {
        ParticleSystem.MinMaxCurve startLifetime = a_particleSystem.main.startLifetime;
        switch (startLifetime.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return startLifetime.constant;
            case ParticleSystemCurveMode.TwoConstants:
                return startLifetime.constantMax;
            default:
                // Curve / TwoCurves の場合、curveMultiplierが曲線の最大到達値の目安になる
                return startLifetime.curveMultiplier;
        }
    }

    private static void SetImageAlpha(Image a_image, float a_alpha)
    {
        Color color = a_image.color;
        color.a = a_alpha;
        a_image.color = color;
    }

    /// <summary>
    /// RectTransformのpivotを変更しつつ、見た目の位置がズレないようanchoredPositionを補正する。
    /// (Unityの既知の挙動: pivotだけ変えるとその場で図形が伸縮した位置に飛ぶため)
    /// </summary>
    private static void SetPivotPreservePosition(RectTransform a_rect, Vector2 a_newPivot)
    {
        Vector2 size = a_rect.rect.size;
        Vector2 deltaPivot = a_newPivot - a_rect.pivot;
        Vector2 offset = new Vector2(
            deltaPivot.x * size.x * a_rect.localScale.x,
            deltaPivot.y * size.y * a_rect.localScale.y);

        a_rect.pivot = a_newPivot;
        a_rect.anchoredPosition += offset;
    }

    private static CanvasGroup GetOrAddCanvasGroup(Component a_target)
    {
        CanvasGroup group = a_target.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = a_target.gameObject.AddComponent<CanvasGroup>();
        }

        return group;
    }

    /// <summary>
    /// ease-in-out。前半は加速、後半は減速するS字カーブ。a_powerが大きいほど中間の加速が急になり、
    /// 出だしと止まり際だけがゆっくりな「ぐいーん」とした緩急のついた動きになる(1で等速の直線移動)。
    /// </summary>
    private static float EaseInOut(float a_t, float a_power)
    {
        return a_t < 0.5f
            ? 0.5f * Mathf.Pow(2f * a_t, a_power)
            : 1f - 0.5f * Mathf.Pow(2f * (1f - a_t), a_power);
    }

    /// <summary>
    /// Robert Penner の ease-out-bounce。ボールが跳ねながら着地するような減衰カーブを返す。
    /// </summary>
    private static float EaseOutBounce(float a_t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (a_t < 1f / d1)
        {
            return n1 * a_t * a_t;
        }
        if (a_t < 2f / d1)
        {
            a_t -= 1.5f / d1;
            return n1 * a_t * a_t + 0.75f;
        }
        if (a_t < 2.5f / d1)
        {
            a_t -= 2.25f / d1;
            return n1 * a_t * a_t + 0.9375f;
        }

        a_t -= 2.625f / d1;
        return n1 * a_t * a_t + 0.984375f;
    }
}
