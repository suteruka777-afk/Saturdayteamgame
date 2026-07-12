using System.Collections;
using UnityEngine;

/// <summary>
/// リザルト演出のゲームオーバー(敗北)専用部分。
/// 流れ: ロケットが画面上端から燃えながら落下して消滅 → GameOver文字が落ちてきてバウンド
///       → 項目がバンッバンッと順に出現 → (共通)ボタン表示 → エイリアンが徘徊し続ける
/// </summary>
public partial class ResultPerformController
{
    // ===== 敗北調整値 =====
    [Header("敗北 - ロケット落下")]
    [Tooltip("ロケットが画面上端から下端まで落下しきるまでの秒数")]
    public float ROCKET_FALL_DURATION = 1.0f;
    [Tooltip("落下しきった瞬間のロケットの縮小率(開始スケールに対する倍率)")]
    public float ROCKET_BURN_SCALE = 0.4f;
    [Tooltip("落下開始から何割進んだ時点でフェードアウトを始めるか(0=最初から, 1=最後だけ)")]
    public float ROCKET_FADE_START = 0.3f;

    // 発生量・色・寿命等はプレハブ側(Assets/Prefabs/ロケット燃.prefab)で作り込み済みのため、
    // ここではプレハブをどのタイミングで生やすかだけを調整する
    [Header("敗北 - 燃焼パーティクル")]
    [Tooltip("落下演出(ROCKET_FALL_DURATION)が何割進んだ時点で燃え始めるか(0=落下開始と同時, 1=落下しきる直前)")]
    public float BURN_START_PROGRESS = 0.3f;
    [Tooltip("落下が終わってエミッタを止めたあと、プレハブの粒子寿命(フェードアウトが終わるタイミング)にさらに足す安全マージンの秒数")]
    public float BURN_STOP_DESTROY_DELAY = 0.5f;
    [Tooltip("燃焼パーティクルの描画順(sortingOrder)。ロケット・背景と同じCanvas/SortingLayerに対して確実に手前へ出すための値。大きいほど手前に描画される")]
    public int BURN_PARTICLE_SORTING_ORDER = 10;

    [Header("敗北 - ラベル / 項目")]
    [Tooltip("GameOver文字が落下してバウンドが収まるまでの秒数")]
    public float LABEL_FALL_DURATION = 0.9f;
    [Tooltip("GameOver文字の落下開始位置。定位置から上に何px離れた所から降ってくるか")]
    public float LABEL_FALL_START_OFFSET = 700f;
    [Tooltip("GameOver文字の色")]
    public Color GAME_OVER_LABEL_COLOR = Color.white;
    [Tooltip("スコア/タイムそれぞれが「バンッ」とスケールポップインする秒数")]
    public float ITEM_POP_DURATION = 0.25f;
    [Tooltip("ポップイン時に一瞬だけ超える最大スケール(1.0が等倍)。大きいほど勢いが出る")]
    public float ITEM_POP_OVERSHOOT = 1.2f;
    [Tooltip("スコアとタイムのポップインの間隔(秒)")]
    public float ITEM_POP_INTERVAL = 0.15f;

    [Header("敗北 - エイリアン")]
    [Tooltip("一連の演出(ボタン表示)が終わった後、エイリアンが画面内の位置まで降りてくるまでの秒数")]
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

    // ===== 敗北専用参照 =====
    [Header("敗北 - 参照")]
    [Tooltip("落下中のロケットへ追従させる燃焼パーティクルのプレハブ(Assets/Prefabs/ロケット燃.prefab)")]
    [SerializeField] private ParticleSystem _burnParticlePrefab;
    [Tooltip("画面上部から出現して徘徊するエイリアンのTransform(ワールド空間のSpriteRenderer)。未設定なら演出をスキップする")]
    [SerializeField] private Transform _alienTransform;

    private Vector3 _alienStartPos;
    private Vector3 _alienStartScale;

    /// <summary>
    /// 敗北演出用の初期化。共通のInit()から呼ばれる。
    /// </summary>
    private void InitLose()
    {
        if (_alienTransform != null)
        {
            _alienStartPos = _alienTransform.position;
            _alienStartScale = _alienTransform.localScale;
        }
    }

    /// <summary>
    /// 敗北演出の初期配置。共通のSetInitialState()から呼ばれる。
    /// </summary>
    private void SetInitialStateLose()
    {
        // 画面上端の外からスタートし、下端の外まで落下する
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

    /// <summary>
    /// ロケットが画面上端から下端まで落下しながらフェード+縮小(疑似ディゾルブ)する。
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
    /// GameOver文字が上から落ちてきて、床を跳ねるようにバウンドしながら定位置に収まる。
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
    /// スコア→タイムの順に「バンッ」とスケールポップインさせる。
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
    /// 一連の演出(ボタン表示)が終わった後、エイリアンが画面上部から画面内へ降りてきて、
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
}
