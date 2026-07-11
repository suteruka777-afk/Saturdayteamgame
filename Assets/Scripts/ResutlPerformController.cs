using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// リザルト画面の演出担当。
/// MonoBehaviourは継承しない。ResultManager (MonoBehaviour) が
/// Init() → StartCoroutine(Play(isClear)) の順で呼び出し、コルーチンを動かす。
/// </summary>
[System.Serializable]
public class ResutlPerformController
{
    // ===== ロケット演出 =====
    // 敗北: 画面上端から下端まで落下してフェードアウト
    // 勝利: 画面下端から月の位置まで上昇し、近づくほど縮小
    [Header("ロケット演出")]
    [Tooltip("敗北時: ロケットが画面上端から下端まで落下しきるまでの秒数")]
    public float ROCKET_FALL_DURATION = 1.0f;
    [Tooltip("敗北時: 落下しきった瞬間のロケットの縮小率(開始スケールに対する倍率)")]
    public float ROCKET_BURN_SCALE = 0.4f;
    [Tooltip("敗北時: 落下開始から何割進んだ時点でフェードアウトを始めるか(0=最初から, 1=最後だけ)")]
    public float ROCKET_FADE_START = 0.3f;
    [Tooltip("勝利時: ロケットが画面下端から月まで上昇しきるまでの秒数")]
    public float ROCKET_FLIGHT_DURATION = 1.2f;
    [Tooltip("勝利時: 月に近づくにつれて減速するイージングの強さ。1で等速、大きいほど月の手前で急激に減速する")]
    public float ROCKET_FLIGHT_EASE_POWER = 4f;
    [Tooltip("勝利時: 月に到達した瞬間のロケットの縮小率(開始スケールに対する倍率)。小さいほど遠近感が強く出る")]
    public float ROCKET_MOON_ARRIVAL_SCALE = 0.35f;
    [Tooltip("ロケットを画面外に配置するときの追加余白(px相当)。0だと画面端ぎりぎりで見切れる")]
    public float SCREEN_EDGE_MARGIN = 100f;
    [Tooltip("勝利時: 発進位置(画面下端)を中心から左右にずらすオフセット。マイナスで左、プラスで右。月までの軌道を斜めにしたいときに使う")]
    public float ROCKET_LAUNCH_X_OFFSET = 0f;
    [Tooltip("勝利時: 発進地点から月上空(ホバー地点)までの軌道を弧状に膨らませる量(ワールド単位)。0で直線。符号でどちら側に膨らむかが変わる")]
    public float ROCKET_ARC_HEIGHT = 1f;
    [Tooltip("勝利時: 着地前に月の上空でホバーする高さ(月からの上方向オフセット、ワールド単位)")]
    public float ROCKET_LANDING_HOVER_HEIGHT = 1f;
    [Tooltip("勝利時: ホバー地点から月の表面まで降下しきるまでの秒数")]
    public float ROCKET_LANDING_DURATION = 0.6f;
    [Tooltip("勝利時: 着地の瞬間に潰れる度合い(1.0で潰れなし、小さいほど強く潰れて着地の重みが出る)")]
    public float ROCKET_LANDING_SQUASH = 0.7f;
    [Tooltip("勝利時: 着地で潰れてから元のスケールに戻りきるまでの秒数")]
    public float ROCKET_LANDING_SQUASH_DURATION = 0.2f;
    [Tooltip("勝利時: 着地が完全に終わってから、Clear文字などの表示が始まるまでの間(秒)。着地の余韻をとるための間")]
    public float ROCKET_LANDING_PAUSE_DURATION = 0.4f;
    [Tooltip("勝利時: 月へ向かう際、進行方向にロケットの向きを合わせる。ロケットの絵が正面(上向き)に描かれている前提で、そこからのズレ(度)を補正できる")]
    public float ROCKET_SPRITE_FORWARD_OFFSET = 0f;

    // ===== 軌跡パーティクル (勝利時、月へ向かう移動中に一定間隔で残す単発の演出) =====
    // 発生量・色・寿命等はプレハブ側(Assets/Prefabs/ロケット軌跡.prefab)で作り込み済みのため、
    // ここでは生成間隔・止めるタイミング・描画順・後始末だけを調整する
    [Header("軌跡パーティクル")]
    [Tooltip("移動中、軌跡パーティクルを生成する間隔(秒)")]
    public float ROCKET_TRAIL_INTERVAL = 0.15f;
    [Tooltip("月までの移動(接近+着地降下)全体のうち、この割合まで進んだら軌跡の生成をやめる(0〜1)。1にすると止めずに出し続ける")]
    public float ROCKET_TRAIL_STOP_PROGRESS = 0.7f;
    [Tooltip("軌跡パーティクルの描画順(sortingOrder)。ロケット・背景と同じCanvas/SortingLayerに対して確実に手前へ出すための値")]
    public int TRAIL_PARTICLE_SORTING_ORDER = 10;
    [Tooltip("軌跡パーティクルの寿命(フェードアウト等)が終わるのを待つ安全マージンの秒数")]
    public float TRAIL_PARTICLE_DESTROY_MARGIN = 0.5f;

    // ===== 着地パーティクル (勝利時、月に着地した瞬間に発生する単発の演出) =====
    // 発生量・色・寿命等はプレハブ側(Assets/Prefabs/ロケット着.prefab)で作り込み済みのため、
    // ここでは描画順と後始末のタイミングだけを調整する
    [Header("着地パーティクル")]
    [Tooltip("着地パーティクルの描画順(sortingOrder)。ロケット・背景と同じCanvas/SortingLayerに対して確実に手前へ出すための値")]
    public int LANDING_PARTICLE_SORTING_ORDER = 10;
    [Tooltip("着地パーティクルの寿命(フェードアウト等)が終わるのを待つ安全マージンの秒数")]
    public float LANDING_PARTICLE_DESTROY_MARGIN = 0.5f;
    [Tooltip("着地パーティクルの発生位置を、ロケットの足元(自動算出した下端)からさらに上下にずらす微調整量(ワールド単位)。マイナスでより上、プラスでより下")]
    public float LANDING_PARTICLE_OFFSET_Y = 0f;

    // ===== 着地シャドウ (勝利時、着地した足元に表示する影) =====
    // 見た目(形・色・濃さ)はシーン上の「かげ」オブジェクト側で作り込み済みのため、
    // ここでは位置合わせとフェードインのタイミングだけを調整する
    [Header("着地シャドウ")]
    [Tooltip("影の位置を、ロケットの足元(自動算出)からさらに上下にずらす微調整量(ローカル単位)。マイナスでより下")]
    public float SHADOW_OFFSET_Y = 0f;
    [Tooltip("着地時に影がフェードインしきるまでの秒数")]
    public float SHADOW_FADE_DURATION = 0.3f;

    // ===== ラベル演出 (GameOver / Clear の見出し文字) =====
    [Header("ラベル演出")]
    [Tooltip("敗北時: GameOver文字が落下してバウンドが収まるまでの秒数")]
    public float LABEL_FALL_DURATION = 0.9f;
    [Tooltip("敗北時: GameOver文字の落下開始位置。定位置から上に何px離れた所から降ってくるか")]
    public float LABEL_FALL_START_OFFSET = 700f;
    [Tooltip("勝利時: Clear文字が下端中心を軸に拡大しながらフェードインする秒数")]
    public float LABEL_ZOOM_FADE_DURATION = 0.8f;
    [Tooltip("勝利時: Clear文字の色")]
    public Color CLEAR_LABEL_COLOR = Color.white;
    [Tooltip("敗北時: GameOver文字の色")]
    public Color GAME_OVER_LABEL_COLOR = Color.white;

    // ===== 項目演出 (スコア・タイム) =====
    [Header("項目演出")]
    [Tooltip("敗北時: スコア/タイムそれぞれが「バンッ」とスケールポップインする秒数")]
    public float ITEM_POP_DURATION = 0.25f;
    [Tooltip("敗北時: ポップイン時に一瞬だけ超える最大スケール(1.0が等倍)。大きいほど勢いが出る")]
    public float ITEM_POP_OVERSHOOT = 1.2f;
    [Tooltip("敗北時: スコアとタイムのポップインの間隔(秒)")]
    public float ITEM_POP_INTERVAL = 0.15f;
    [Tooltip("勝利時: スコア/タイムが同時に静かにフェードインする秒数")]
    public float ITEM_FADE_DURATION = 0.7f;

    // ===== ボタン演出 (Retry / Title 共通) =====
    [Header("ボタン演出")]
    [Tooltip("ボタンがぽわっと広がりながらフェードインする秒数")]
    public float BUTTON_FADE_DURATION = 0.4f;
    [Tooltip("ボタンの出現前のスケール(1.0が等倍)。小さいほど「ぽわっ」と広がる感じが強まる")]
    public float BUTTON_START_SCALE = 0.7f;
    [Tooltip("ボタン出現時に一瞬だけ超える最大スケール(1.0が等倍)")]
    public float BUTTON_POP_OVERSHOOT = 1.08f;

    // ===== 燃焼パーティクル (敗北時、落下中のロケットの位置に追従して発生し続ける) =====
    // 発生量・色・寿命等はプレハブ側(Assets/Prefabs/ロケット燃.prefab)で作り込み済みのため、
    // ここではプレハブをどのタイミングで生やすかだけを調整する
    [Header("燃焼パーティクル")]
    [Tooltip("落下演出(ROCKET_FALL_DURATION)が何割進んだ時点で燃え始めるか(0=落下開始と同時, 1=落下しきる直前)")]
    public float BURN_START_PROGRESS = 0.3f;
    [Tooltip("落下が終わってエミッタを止めたあと、プレハブの粒子寿命(フェードアウトが終わるタイミング)にさらに足す安全マージンの秒数")]
    public float BURN_STOP_DESTROY_DELAY = 0.5f;
    [Tooltip("燃焼パーティクルの描画順(sortingOrder)。ロケット・背景と同じCanvas/SortingLayerに対して確実に手前へ出すための値。大きいほど手前に描画される")]
    public int BURN_PARTICLE_SORTING_ORDER = 10;

    // ===== エイリアン演出 (敗北時のみ。ボタン表示まで終わった後に画面上部から出現して徘徊し続ける) =====
    [Header("エイリアン演出 (敗北時のみ)")]
    [Tooltip("敗北時: 一連の演出(ボタン表示)が終わった後、エイリアンが画面内の位置まで降りてくるまでの秒数")]
    public float ALIEN_ENTRY_DURATION = 1.5f;
    [Tooltip("エイリアンが画面内を徘徊するときの移動速度(ワールド単位/秒)")]
    public float ALIEN_WANDER_SPEED = 2f;
    [Tooltip("エイリアンが画面端に寄りすぎないようにする余白(ワールド単位)")]
    public float ALIEN_WANDER_MARGIN = 1.5f;
    [Tooltip("徘徊移動のイージングの強さ。1で等速、大きいほど出だしと止まり際がゆっくりで中間が速い『ぐいーん』とした動きになる")]
    public float ALIEN_WANDER_EASE_POWER = 3f;
    [Tooltip("徘徊で近づいたように見えるときの最大スケール倍率(1.0が基準スケール)")]
    public float ALIEN_WANDER_SCALE_MAX = 1.4f;
    [Tooltip("徘徊で遠ざかったように見えるときの最小スケール倍率(1.0が基準スケール)")]
    public float ALIEN_WANDER_SCALE_MIN = 0.7f;

    [Header("参照 - ロケット / 月")]
    [Tooltip("ロケットのRectTransform。移動・拡縮のアニメーション対象")]
    [SerializeField] private RectTransform _rocketRect;
    [Tooltip("ロケットのImage。フェードアウトのアルファ制御に使う")]
    [SerializeField] private Image _rocketImage;
    [Tooltip("月のRectTransform。勝利時にロケットが向かう目的地")]
    [SerializeField] private RectTransform _moonRect;
    [Tooltip("敗北時にロケットへ追従させる燃焼パーティクルのプレハブ(Assets/Prefabs/ロケット燃.prefab)")]
    [SerializeField] private ParticleSystem _burnParticlePrefab;
    [Tooltip("勝利時、月へ着地した瞬間に生成する着地パーティクルのプレハブ(Assets/Prefabs/ロケット着.prefab)")]
    [SerializeField] private ParticleSystem _landingParticlePrefab;
    [Tooltip("勝利時、月への移動中に一定間隔で残す軌跡パーティクルのプレハブ(Assets/Prefabs/ロケット軌跡.prefab)")]
    [SerializeField] private ParticleSystem _trailParticlePrefab;
    [Tooltip("勝利時、着地の足元に表示するシャドウのImage(シーン上の「かげ」オブジェクト)")]
    [SerializeField] private Image _shadowImage;

    [Header("参照 - テキスト")]
    [Tooltip("GameOver / Clear の見出しテキスト")]
    [SerializeField] private TextMeshProUGUI _labelText;
    [Tooltip("スコア表示テキスト")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [Tooltip("タイム表示テキスト")]
    [SerializeField] private TextMeshProUGUI _timeText;

    [Header("参照 - ボタン")]
    [SerializeField] private Button _btnRetry;
    [SerializeField] private Button _btnTitle;

    [Header("参照 - エイリアン")]
    [Tooltip("敗北時に画面上部から出現して徘徊するエイリアンのTransform(ワールド空間のSpriteRenderer)。未設定なら演出をスキップする")]
    [SerializeField] private Transform _alienTransform;

    // Init() でシーン上の初期配置から算出する値。演出中はこれらを基準にアニメーションする
    private Vector2 _rocketStartPos;
    private Vector3 _rocketStartScale;
    private Quaternion _rocketStartRotation;
    private Vector2 _rocketOffScreenTopPos;
    private Vector2 _rocketOffScreenBottomPos;
    private Vector2 _labelRestPos;
    private Vector3 _alienStartPos;
    private Vector3 _alienStartScale;

    private CanvasGroup _retryGroup;
    private CanvasGroup _titleGroup;

    // シャドウの「見えている時」の目標アルファ値。シーン側で設定された値をInit()時に読み取って使う
    private float _shadowTargetAlpha;

    private bool _skipRequested;

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
        _rocketOffScreenBottomPos = CalculateOffScreenPosition(false) + new Vector2(ROCKET_LAUNCH_X_OFFSET, 0f);

        _labelRestPos = _labelText.rectTransform.anchoredPosition;

        _retryGroup = GetOrAddCanvasGroup(_btnRetry);
        _titleGroup = GetOrAddCanvasGroup(_btnTitle);

        if (_alienTransform != null)
        {
            _alienStartPos = _alienTransform.position;
            _alienStartScale = _alienTransform.localScale;
        }

        if (_shadowImage != null)
        {
            _shadowTargetAlpha = _shadowImage.color.a;
        }
    }

    /// <summary>
    /// 勝敗に応じたリザルト演出を再生する。ResultManager から StartCoroutine で呼ぶ。
    /// </summary>
    public IEnumerator Play(bool a_isClear)
    {
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

        if (!a_isClear)
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
        _skipRequested = true;
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

    private void SetInitialState(bool a_isClear)
    {
        _rocketRect.gameObject.SetActive(true);
        _rocketRect.localScale = _rocketStartScale;
        _rocketRect.rotation = _rocketStartRotation;
        SetImageAlpha(_rocketImage, 1f);

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
            // 勝利: 画面下端の外からスタートし、月へ向かって上昇する
            _rocketRect.anchoredPosition = _rocketOffScreenBottomPos;

            _labelText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _labelText.rectTransform.anchoredPosition = _labelRestPos;
            _labelText.rectTransform.localScale = Vector3.one;
            _labelText.color = CLEAR_LABEL_COLOR;
            _labelText.alpha = 0f;

            _scoreText.rectTransform.localScale = Vector3.one;
            _timeText.rectTransform.localScale = Vector3.one;
            _scoreText.alpha = 0f;
            _timeText.alpha = 0f;
        }
        else
        {
            // 敗北: 画面上端の外からスタートし、下端の外まで落下する
            _rocketRect.anchoredPosition = _rocketOffScreenTopPos;

            _labelText.rectTransform.anchoredPosition = _labelRestPos + Vector2.up * LABEL_FALL_START_OFFSET;
            _labelText.rectTransform.localScale = Vector3.one;
            _labelText.color = GAME_OVER_LABEL_COLOR;
            _labelText.alpha = 1f;

            _scoreText.rectTransform.localScale = Vector3.zero;
            _timeText.rectTransform.localScale = Vector3.zero;
            _scoreText.alpha = 1f;
            _timeText.alpha = 1f;
        }
    }

    /// <summary>
    /// 敗北時: ロケットが画面上端から下端まで落下しながらフェード+縮小(疑似ディゾルブ)する。
    /// 落下がBURN_START_PROGRESS割まで進んだ時点でロケット位置に燃焼パーティクルを発生させ、
    /// 以後は毎フレームその位置に追従させることで「燃えながら落ちる」見た目にする。
    /// Skip時は燃焼を一切発生させず即座に最終状態へスナップする。
    /// </summary>
    private IEnumerator RocketBurnRoutine()
    {
        ParticleSystem burnPs = null;

        yield return Tween(ROCKET_FALL_DURATION, t =>
        {
            float easeT = t * t; // ease-in: 落下が徐々に加速していく感覚
            _rocketRect.anchoredPosition = Vector2.Lerp(_rocketOffScreenTopPos, _rocketOffScreenBottomPos, easeT);
            _rocketRect.localScale = Vector3.Lerp(_rocketStartScale, _rocketStartScale * ROCKET_BURN_SCALE, easeT);

            float fadeT = Mathf.Clamp01((t - ROCKET_FADE_START) / (1f - ROCKET_FADE_START));
            SetImageAlpha(_rocketImage, 1f - fadeT);

            if (_skipRequested)
            {
                return;
            }

            if (burnPs == null && t >= BURN_START_PROGRESS)
            {
                burnPs = SpawnBurnParticleInstance();
            }

            if (burnPs != null)
            {
                burnPs.transform.position = ConvertUiPositionToWorld(_rocketRect);
            }
        });

        if (burnPs != null)
        {
            // 追従用のエミッタは停止するだけ。既に出た粒子は寿命が尽きるまで自然に消えていく。
            // 破棄はプレハブ側で設定された寿命(フェードアウトが終わるタイミング)に安全マージンを
            // 足した秒数だけ待ってから行う。固定秒数で先に破棄すると、フェードし切る前に
            // オブジェクトごと消えてプレハブの「だんだん透明になる」演出が反映されなくなるため。
            burnPs.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            float destroyDelay = GetMaxStartLifetime(burnPs) + BURN_STOP_DESTROY_DELAY;
            Object.Destroy(burnPs.gameObject, destroyDelay);
        }

        _rocketRect.gameObject.SetActive(false);
    }

    /// <summary>
    /// 勝利時: ロケットが画面下端から月の上空(ホバー地点)まで上昇して近づくにつれて縮小し、
    /// そこから月の表面まで降下して着地する。着地の瞬間は少し潰れてから元に戻り(重みのある着地感)、
    /// 着地パーティクルを発生させる。区間ごとに直線移動なので、区間の開始時に進行方向へ
    /// ロケットの向きを合わせる(区間の途中で方向は変わらないため毎フレーム計算する必要はない)。
    /// ロケットと月は親は同じだがアンカー/ピボットが異なるため、anchoredPositionではなく
    /// ワールド座標(position)同士で補間することで正しく月へ到達させる。
    /// Skip時は着地パーティクルを発生させず即座に最終状態へスナップする。
    /// </summary>
    private IEnumerator RocketFlightRoutine()
    {
        Vector3 startPos = _rocketRect.position;
        Vector3 hoverPos = _moonRect.position + Vector3.up * ROCKET_LANDING_HOVER_HEIGHT;
        Vector3 arrivalScale = _rocketStartScale * ROCKET_MOON_ARRIVAL_SCALE;

        // 接近・着地降下を通した移動時間全体に対する進行度で、軌跡パーティクルを止めるタイミングを判定する
        float totalMoveDuration = ROCKET_FLIGHT_DURATION + ROCKET_LANDING_DURATION;
        float trailTimer = 0f;

        // 接近: 画面下端からホバー地点まで、近づくにつれて減速・縮小しながら飛行する。
        // 直線ではなく、進行方向に垂直な向きへ膨らませた2次ベジェ曲線で弧を描かせる。
        // 向きは発進直後の(弧の接線方向の)進行方向から、降下開始時にちょうど真上を向くよう、
        // 位置と同じイージングで滑らかに回転させる(降下がまっすぐ上向きの姿勢で始められるようにするため)
        Vector3 midPoint = Vector3.Lerp(startPos, hoverPos, 0.5f);
        Vector3 arcNormal = new Vector3(-(hoverPos - startPos).y, (hoverPos - startPos).x, 0f).normalized;
        Vector3 arcControlPoint = midPoint + arcNormal * ROCKET_ARC_HEIGHT;

        float launchAngle = ComputeFacingAngle(arcControlPoint - startPos);
        float uprightAngle = ROCKET_SPRITE_FORWARD_OFFSET;
        yield return Tween(ROCKET_FLIGHT_DURATION, t =>
        {
            // ease-out: 指数(ROCKET_FLIGHT_EASE_POWER)が大きいほどホバー地点の手前で急激に減速する
            float easeT = 1f - Mathf.Pow(1f - t, ROCKET_FLIGHT_EASE_POWER);
            _rocketRect.position = QuadraticBezier(startPos, arcControlPoint, hoverPos, easeT);
            _rocketRect.localScale = Vector3.Lerp(_rocketStartScale, arrivalScale, easeT);
            _rocketRect.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(launchAngle, uprightAngle, easeT));

            float moveProgress = t * ROCKET_FLIGHT_DURATION / totalMoveDuration;
            trailTimer = UpdateTrailSpawn(trailTimer, moveProgress);
        });

        // 着地: ホバー地点から月の表面まで、減速しながら降りる(ソフトランディング)。
        // ここで進行方向(下向き)に合わせて回転させると墜落しているように見えるため、
        // 向きは変えず、接近フェーズの終わりに揃えた真上向きのまま降下させる
        yield return Tween(ROCKET_LANDING_DURATION, t =>
        {
            float easeT = 1f - Mathf.Pow(1f - t, 2f);
            _rocketRect.position = Vector3.Lerp(hoverPos, _moonRect.position, easeT);

            float moveProgress = (ROCKET_FLIGHT_DURATION + t * ROCKET_LANDING_DURATION) / totalMoveDuration;
            trailTimer = UpdateTrailSpawn(trailTimer, moveProgress);
        });

        if (!_skipRequested)
        {
            SpawnLandingParticleInstance();
        }

        // 着地の瞬間、横に潰れてすぐ戻る(バウンドではなく重みで沈み込むような着地感)
        Vector3 squashScale = new Vector3(arrivalScale.x / ROCKET_LANDING_SQUASH, arrivalScale.y * ROCKET_LANDING_SQUASH, arrivalScale.z);
        yield return Tween(ROCKET_LANDING_SQUASH_DURATION, t =>
        {
            float squashT = t < 0.4f
                ? Mathf.Lerp(0f, 1f, t / 0.4f)
                : Mathf.Lerp(1f, 0f, (t - 0.4f) / 0.6f);
            _rocketRect.localScale = Vector3.Lerp(arrivalScale, squashScale, squashT);
        });
        _rocketRect.localScale = arrivalScale;

        // 着地シャドウをロケットの足元へ移動させてからフェードインする
        if (_shadowImage != null)
        {
            float rocketHalfHeight = _rocketRect.rect.height * 0.5f * _rocketRect.localScale.y;
            _shadowImage.rectTransform.anchoredPosition = _rocketRect.anchoredPosition + Vector2.down * (rocketHalfHeight + SHADOW_OFFSET_Y);

            yield return Tween(SHADOW_FADE_DURATION, t =>
            {
                SetImageAlpha(_shadowImage, t * _shadowTargetAlpha);
            });
        }
    }

    /// <summary>
    /// 敗北時: 一連の演出(ボタン表示)が終わった後、エイリアンが画面上部から画面内へ降りてきて、
    /// 以後は画面内のランダムな地点へ移動し続ける(徘徊)。リザルト画面を離れる(シーン遷移)まで終わらない。
    /// _alienTransform が未設定の場合は何もしない。
    /// </summary>
    private IEnumerator AlienIntroAndWanderRoutine()
    {
        if (_alienTransform == null)
        {
            yield break;
        }

        _alienTransform.gameObject.SetActive(true);

        Vector3 entryStartPos = _alienTransform.position;
        Vector3 entryTargetPos = GetRandomPointOnScreen(ALIEN_WANDER_MARGIN);

        yield return Tween(ALIEN_ENTRY_DURATION, t =>
        {
            float easeT = 1f - Mathf.Pow(1f - t, 2f); // ease-out: 画面内でふわりと止まる
            _alienTransform.position = Vector3.Lerp(entryStartPos, entryTargetPos, easeT);
        });

        while (true)
        {
            Vector3 from = _alienTransform.position;
            Vector3 to = GetRandomPointOnScreen(ALIEN_WANDER_MARGIN);
            float duration = Mathf.Max(0.1f, Vector3.Distance(from, to) / ALIEN_WANDER_SPEED);

            // 移動のたびにランダムなスケールを目的地として選び、位置と同じイージングで一緒に変化させる。
            // これにより移動と連動して「近づいて大きく見える/遠ざかって小さく見える」感じになる
            Vector3 fromScale = _alienTransform.localScale;
            Vector3 toScale = _alienStartScale * Random.Range(ALIEN_WANDER_SCALE_MIN, ALIEN_WANDER_SCALE_MAX);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                // ease-in-out: 出だしと止まり際はゆっくり、中間だけ速く動く「ぐいーん」とした緩急
                float easeT = EaseInOut(Mathf.Clamp01(t), ALIEN_WANDER_EASE_POWER);
                _alienTransform.position = Vector3.Lerp(from, to, easeT);
                _alienTransform.localScale = Vector3.Lerp(fromScale, toScale, easeT);
                yield return null;
            }
        }
    }

    /// <summary>
    /// Main Cameraの表示範囲内(端からALIEN_WANDER_MARGIN分内側)のランダムなワールド座標を返す。
    /// エイリアンの徘徊・出現先の目的地決めに使う。
    /// </summary>
    private static Vector3 GetRandomPointOnScreen(float a_margin)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return Vector3.zero;
        }

        float halfHeight = Mathf.Max(0.1f, cam.orthographicSize - a_margin);
        float halfWidth = Mathf.Max(0.1f, cam.orthographicSize * cam.aspect - a_margin);

        float x = Random.Range(-halfWidth, halfWidth);
        float y = Random.Range(-halfHeight, halfHeight);
        return new Vector3(cam.transform.position.x + x, cam.transform.position.y + y, 0f);
    }

    /// <summary>
    /// 敗北時: GameOver文字が上から落ちてきて、床を跳ねるようにバウンドしながら定位置に収まる。
    /// </summary>
    private IEnumerator LabelFallBounceRoutine()
    {
        Vector2 startPos = _labelText.rectTransform.anchoredPosition;

        yield return Tween(LABEL_FALL_DURATION, t =>
        {
            float easeT = EaseOutBounce(t);
            _labelText.rectTransform.anchoredPosition = Vector2.Lerp(startPos, _labelRestPos, easeT);
        });
    }

    /// <summary>
    /// 勝利時: Clear文字が下端中心を軸に拡大しながらフェードインする。
    /// ピボットを一時的に下端中心へ変更し、見た目の位置がズレないよう座標を補正する。
    /// </summary>
    private IEnumerator LabelZoomFadeRoutine()
    {
        SetPivotPreservePosition(_labelText.rectTransform, new Vector2(0.5f, 0f));
        _labelText.rectTransform.localScale = Vector3.zero;

        yield return Tween(LABEL_ZOOM_FADE_DURATION, t =>
        {
            float easeT = 1f - Mathf.Pow(1f - t, 2f); // ease-out: 「ふあっと」した柔らかい伸び
            _labelText.rectTransform.localScale = Vector3.one * easeT;
            _labelText.alpha = easeT;
        });
    }

    /// <summary>
    /// 敗北時: スコア→タイムの順に「バンッ」とスケールポップインさせる。
    /// </summary>
    private IEnumerator ItemsPopStaggerRoutine()
    {
        yield return PopIn(_scoreText.rectTransform);
        yield return WaitOrSkip(ITEM_POP_INTERVAL);
        yield return PopIn(_timeText.rectTransform);
    }

    private IEnumerator PopIn(RectTransform a_rect)
    {
        yield return Tween(ITEM_POP_DURATION, t =>
        {
            // 前半でオーバーシュートまで伸ばし、後半で等倍に収める = 「バンッ」という勢い
            float scale = t < 0.6f
                ? Mathf.Lerp(0f, ITEM_POP_OVERSHOOT, t / 0.6f)
                : Mathf.Lerp(ITEM_POP_OVERSHOOT, 1f, (t - 0.6f) / 0.4f);
            a_rect.localScale = Vector3.one * scale;
        });
    }

    /// <summary>
    /// 勝利時: スコア・タイムを同時に静かにフェードインさせる。
    /// </summary>
    private IEnumerator ItemsFadeRoutine()
    {
        yield return Tween(ITEM_FADE_DURATION, t =>
        {
            _scoreText.alpha = t;
            _timeText.alpha = t;
        });
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
    /// Skip要求が来ない限り指定秒数だけ待つ。項目演出の合間の「間」に使う。
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
    /// 燃焼パーティクルのプレハブ(_burnParticlePrefab)をワールド空間にインスタンス化する。
    /// プレハブ側で発生量・色・寿命・再生設定(loop / playOnAwake)まで作り込み済みのため、
    /// 生成すればそのまま燃え始める。停止・破棄は呼び出し側の責任。
    /// </summary>
    private ParticleSystem SpawnBurnParticleInstance()
    {
        if (_burnParticlePrefab == null)
        {
            return null;
        }

        Vector3 spawnPos = ConvertUiPositionToWorld(_rocketRect);
        ParticleSystem instance = Object.Instantiate(_burnParticlePrefab, spawnPos, _burnParticlePrefab.transform.rotation);

        // ロケット・背景が同じCanvas上のUIとして描画されており、通常のRendererであるパーティクルは
        // SortingLayer/Order勝負で背後に回り込んでしまうことがあるため、明示的に手前へ固定する
        ParticleSystemRenderer psRenderer = instance.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            psRenderer.sortingOrder = BURN_PARTICLE_SORTING_ORDER;
        }

        return instance;
    }

    /// <summary>
    /// 着地パーティクルのプレハブ(_landingParticlePrefab)を、ロケットの足元(自動算出した下端に
    /// LANDING_PARTICLE_OFFSET_Yの微調整を加えた位置)にワールド空間で生成する。
    /// プレハブ側が単発(non-looping / playOnAwake)のため、生成すればそのまま再生され、
    /// 寿命が尽きたタイミングでオブジェクトを自動で破棄する。
    /// </summary>
    private void SpawnLandingParticleInstance()
    {
        if (_landingParticlePrefab == null)
        {
            return;
        }

        // ロケットの見た目上の下端(足元)を、RectTransformの高さと現在のワールドスケールから概算する
        float rocketHalfHeight = _rocketRect.rect.height * 0.5f * _rocketRect.lossyScale.y;
        Vector3 spawnPos = _rocketRect.position + Vector3.down * (rocketHalfHeight + LANDING_PARTICLE_OFFSET_Y);

        ParticleSystem instance = Object.Instantiate(_landingParticlePrefab, spawnPos, _landingParticlePrefab.transform.rotation);

        ParticleSystemRenderer psRenderer = instance.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            psRenderer.sortingOrder = LANDING_PARTICLE_SORTING_ORDER;
        }

        float destroyDelay = GetMaxStartLifetime(instance) + LANDING_PARTICLE_DESTROY_MARGIN;
        Object.Destroy(instance.gameObject, destroyDelay);
    }

    /// <summary>
    /// 軌跡パーティクルの生成タイミングを判定する。Skip時、または移動全体の進行度が
    /// ROCKET_TRAIL_STOP_PROGRESSを超えた後は生成しない。ROCKET_TRAIL_INTERVALごとに
    /// _trailParticlePrefabを1個生成する。更新後のタイマー値を返すので、呼び出し側で
    /// 次フレームの計算に使うローカル変数へ代入し直すこと。
    /// </summary>
    private float UpdateTrailSpawn(float a_trailTimer, float a_moveProgress)
    {
        if (_skipRequested || a_moveProgress >= ROCKET_TRAIL_STOP_PROGRESS)
        {
            return a_trailTimer;
        }

        a_trailTimer += Time.deltaTime;
        if (a_trailTimer >= ROCKET_TRAIL_INTERVAL)
        {
            a_trailTimer = 0f;
            SpawnTrailParticleInstance();
        }

        return a_trailTimer;
    }

    /// <summary>
    /// 軌跡パーティクルのプレハブ(_trailParticlePrefab)を、ロケットの現在位置にワールド空間で生成する。
    /// プレハブ側が単発(non-looping / playOnAwake)のため、生成すればそのまま再生され、
    /// 寿命が尽きたタイミングでオブジェクトを自動で破棄する。
    /// </summary>
    private void SpawnTrailParticleInstance()
    {
        if (_trailParticlePrefab == null)
        {
            return;
        }

        ParticleSystem instance = Object.Instantiate(_trailParticlePrefab, _rocketRect.position, _trailParticlePrefab.transform.rotation);

        ParticleSystemRenderer psRenderer = instance.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            psRenderer.sortingOrder = TRAIL_PARTICLE_SORTING_ORDER;
        }

        float destroyDelay = GetMaxStartLifetime(instance) + TRAIL_PARTICLE_DESTROY_MARGIN;
        Object.Destroy(instance.gameObject, destroyDelay);
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

    /// <summary>
    /// 指定した方向ベクトルを向くためのZ軸回転角度(度)を計算する(2D)。
    /// ロケットの絵が正面(ノーズ)を上向きに描かれている前提で角度を計算しており、
    /// 実際の絵の向きとズレる場合はROCKET_SPRITE_FORWARD_OFFSETで補正できる。
    /// </summary>
    private float ComputeFacingAngle(Vector3 a_direction)
    {
        if (a_direction.sqrMagnitude < 0.0001f)
        {
            return ROCKET_SPRITE_FORWARD_OFFSET;
        }

        float angle = Mathf.Atan2(a_direction.y, a_direction.x) * Mathf.Rad2Deg - 90f;
        return angle + ROCKET_SPRITE_FORWARD_OFFSET;
    }

    /// <summary>
    /// a_p0からa_p2まで、a_p1を制御点とする2次ベジェ曲線上のa_t(0〜1)地点の座標を返す。
    /// </summary>
    private static Vector3 QuadraticBezier(Vector3 a_p0, Vector3 a_p1, Vector3 a_p2, float a_t)
    {
        float u = 1f - a_t;
        return u * u * a_p0 + 2f * u * a_t * a_p1 + a_t * a_t * a_p2;
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
