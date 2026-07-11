using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [Tooltip("勝利時: 月に到達した瞬間のロケットの縮小率(開始スケールに対する倍率)。小さいほど遠近感が強く出る")]
    public float ROCKET_MOON_ARRIVAL_SCALE = 0.35f;
    [Tooltip("ロケットを画面外に配置するときの追加余白(px相当)。0だと画面端ぎりぎりで見切れる")]
    public float SCREEN_EDGE_MARGIN = 100f;

    // ===== ラベル演出 (GameOver / Clear の見出し文字) =====
    [Header("ラベル演出")]
    [Tooltip("敗北時: GameOver文字が落下してバウンドが収まるまでの秒数")]
    public float LABEL_FALL_DURATION = 0.9f;
    [Tooltip("敗北時: GameOver文字の落下開始位置。定位置から上に何px離れた所から降ってくるか")]
    public float LABEL_FALL_START_OFFSET = 700f;
    [Tooltip("勝利時: Clear文字が下端中心を軸に拡大しながらフェードインする秒数")]
    public float LABEL_ZOOM_FADE_DURATION = 0.8f;

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

    // ===== 燃焼パーティクル (敗北時、ロケットが落下しきった位置に発生) =====
    [Header("燃焼パーティクル")]
    [Tooltip("パーティクルが再生されてから消えるまでの秒数")]
    public float BURN_PARTICLE_LIFETIME = 1.0f;
    [Tooltip("一度に発生させる粒子の数")]
    public int BURN_PARTICLE_BURST_COUNT = 30;
    [Tooltip("粒子1個の初期サイズ(ワールド単位)")]
    public float BURN_PARTICLE_START_SIZE = 0.4f;
    [Tooltip("粒子が飛び散る初速")]
    public float BURN_PARTICLE_START_SPEED = 3f;
    [Tooltip("粒子が発生する範囲の半径")]
    public float BURN_PARTICLE_SHAPE_RADIUS = 0.15f;
    [Tooltip("粒子にかかる重力の強さ。大きいほど早く落ちる")]
    public float BURN_PARTICLE_GRAVITY = 0.6f;

    private const int BURN_PARTICLE_TEX_SIZE = 64;

    [Header("参照 - ロケット / 月")]
    [Tooltip("ロケットのRectTransform。移動・拡縮のアニメーション対象")]
    [SerializeField] private RectTransform _rocketRect;
    [Tooltip("ロケットのImage。フェードアウトのアルファ制御に使う")]
    [SerializeField] private Image _rocketImage;
    [Tooltip("月のRectTransform。勝利時にロケットが向かう目的地")]
    [SerializeField] private RectTransform _moonRect;

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

    // Init() でシーン上の初期配置から算出する値。演出中はこれらを基準にアニメーションする
    private Vector2 _rocketStartPos;
    private Vector3 _rocketStartScale;
    private Vector2 _rocketOffScreenTopPos;
    private Vector2 _rocketOffScreenBottomPos;
    private Vector2 _labelRestPos;

    private CanvasGroup _retryGroup;
    private CanvasGroup _titleGroup;

    private bool _skipRequested;

    // 燃焼パーティクル用のマテリアルは専用画像素材が無いためコード生成し、全インスタンスで共有する
    private static Material _burnMaterial;

    /// <summary>
    /// 演出開始前の初期化。シーン上の初期配置・画面サイズから各種基準位置を算出する。
    /// Play() より前に必ず1回呼ぶこと。
    /// </summary>
    public void Init()
    {
        _rocketStartPos = _rocketRect.anchoredPosition;
        _rocketStartScale = _rocketRect.localScale;
        _rocketOffScreenTopPos = CalculateOffScreenPosition(true);
        _rocketOffScreenBottomPos = CalculateOffScreenPosition(false);

        _labelRestPos = _labelText.rectTransform.anchoredPosition;

        _retryGroup = GetOrAddCanvasGroup(_btnRetry);
        _titleGroup = GetOrAddCanvasGroup(_btnTitle);
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
        SetImageAlpha(_rocketImage, 1f);

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
            _labelText.alpha = 1f;

            _scoreText.rectTransform.localScale = Vector3.zero;
            _timeText.rectTransform.localScale = Vector3.zero;
            _scoreText.alpha = 1f;
            _timeText.alpha = 1f;
        }
    }

    /// <summary>
    /// 敗北時: ロケットが画面上端から下端まで落下しながらフェード+縮小(疑似ディゾルブ)し、
    /// 最後に燃焼パーティクルを発生させて非表示になる。
    /// </summary>
    private IEnumerator RocketBurnRoutine()
    {
        yield return Tween(ROCKET_FALL_DURATION, t =>
        {
            float easeT = t * t; // ease-in: 落下が徐々に加速していく感覚
            _rocketRect.anchoredPosition = Vector2.Lerp(_rocketOffScreenTopPos, _rocketOffScreenBottomPos, easeT);
            _rocketRect.localScale = Vector3.Lerp(_rocketStartScale, _rocketStartScale * ROCKET_BURN_SCALE, easeT);

            float fadeT = Mathf.Clamp01((t - ROCKET_FADE_START) / (1f - ROCKET_FADE_START));
            SetImageAlpha(_rocketImage, 1f - fadeT);
        });

        if (!_skipRequested)
        {
            SpawnBurnParticles(_rocketRect);
        }

        _rocketRect.gameObject.SetActive(false);
    }

    /// <summary>
    /// 勝利時: ロケットが画面下端から月の位置まで上昇し、近づくにつれて縮小していく。
    /// ロケットと月は親は同じだがアンカー/ピボットが異なるため、anchoredPositionではなく
    /// ワールド座標(position)同士で補間することで正しく月へ到達させる。
    /// </summary>
    private IEnumerator RocketFlightRoutine()
    {
        Vector3 startPos = _rocketRect.position;
        Vector3 endPos = _moonRect.position;

        yield return Tween(ROCKET_FLIGHT_DURATION, t =>
        {
            float easeT = 1f - Mathf.Pow(1f - t, 3f); // ease-out: 月の手前でゆっくり収束
            _rocketRect.position = Vector3.Lerp(startPos, endPos, easeT);
            _rocketRect.localScale = Vector3.Lerp(_rocketStartScale, _rocketStartScale * ROCKET_MOON_ARRIVAL_SCALE, easeT);
        });
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
    /// 勝敗共通: Retry/Titleボタンがぽわっと広がりながらフェードインし、
    /// 最後にRetryを初期選択状態にする(専用カーソル画像の代わりにButtonのHighlighted色で表現)。
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

        EventSystem.current.SetSelectedGameObject(_btnRetry.gameObject);
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
    /// ロケットの燃焼パーティクルをワールド空間に生成する。
    /// Canvasが Screen Space - Overlay のため、UI階層の中に直接ParticleSystemを置いても
    /// 描画されない(オーバーレイCanvasはカメラを介さず描画されるため)。
    /// そこでUI上の画面位置をワールド座標に変換し、Canvas外の独立したGameObjectとして生成する。
    /// </summary>
    private void SpawnBurnParticles(RectTransform a_uiRect)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, a_uiRect.position);
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));

        GameObject burstObj = new GameObject("RocketBurnParticles");
        burstObj.transform.position = worldPos;

        ParticleSystem ps = burstObj.AddComponent<ParticleSystem>();
        // AddComponent直後は既定パラメータで自動再生されてしまうため、設定完了まで一旦止める
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = BURN_PARTICLE_LIFETIME;
        main.loop = false;
        main.startLifetime = BURN_PARTICLE_LIFETIME * 0.8f;
        main.startSpeed = BURN_PARTICLE_START_SPEED;
        main.startSize = BURN_PARTICLE_START_SIZE;
        main.startColor = new Color(1f, 0.55f, 0.15f);
        main.gravityModifier = BURN_PARTICLE_GRAVITY;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, BURN_PARTICLE_BURST_COUNT) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = BURN_PARTICLE_SHAPE_RADIUS;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.55f, 0.15f), 0f),
                new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        // ランタイム生成のParticleSystemはマテリアル未設定のままだと何も描画されないため明示的に割り当てる
        ParticleSystemRenderer psRenderer = burstObj.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = GetBurnMaterial();

        ps.Play();
        Object.Destroy(burstObj, BURN_PARTICLE_LIFETIME + 0.5f);
    }

    /// <summary>
    /// 燃焼パーティクル用マテリアルを取得する(初回のみ生成し、以降は使い回す)。
    /// </summary>
    private static Material GetBurnMaterial()
    {
        if (_burnMaterial == null)
        {
            // ビルド設定次第でSprites/Defaultが含まれないケースに備え、uGUIが必ず含むUI/Defaultへフォールバック
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("UI/Default");
            }

            _burnMaterial = new Material(shader)
            {
                mainTexture = CreateSoftCircleTexture(BURN_PARTICLE_TEX_SIZE)
            };
        }

        return _burnMaterial;
    }

    /// <summary>
    /// 専用のパーティクル画像素材が無いため、中心から縁へ透明になる円のテクスチャをコード生成する。
    /// </summary>
    private static Texture2D CreateSoftCircleTexture(int a_size)
    {
        Texture2D tex = new Texture2D(a_size, a_size, TextureFormat.RGBA32, false);
        float half = a_size * 0.5f;
        Color32[] pixels = new Color32[a_size * a_size];

        for (int y = 0; y < a_size; y++)
        {
            for (int x = 0; x < a_size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                float alpha = Mathf.Clamp01(1f - dist);
                byte alphaByte = (byte)(alpha * alpha * 255f);
                pixels[y * a_size + x] = new Color32(255, 255, 255, alphaByte);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
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
