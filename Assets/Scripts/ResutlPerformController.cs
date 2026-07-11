using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// リザルト画面の演出担当。ResultManager から Init() → Play() を呼んで開始する
/// MonoBehaviourは継承せず、参照は ResultManager の Inspector 配下で設定する
/// </summary>
[System.Serializable]
public class ResutlPerformController
{
    private const float ROCKET_FALL_DURATION = 1.0f;
    private const float ROCKET_FALL_DISTANCE = 600f;
    private const float ROCKET_BURN_SCALE = 0.4f;
    private const float ROCKET_FADE_START = 0.3f;
    private const float ROCKET_FLIGHT_DURATION = 1.2f;

    private const float LABEL_FALL_DURATION = 0.9f;
    private const float LABEL_FALL_START_OFFSET = 700f;
    private const float LABEL_ZOOM_FADE_DURATION = 0.8f;

    private const float ITEM_POP_DURATION = 0.25f;
    private const float ITEM_POP_OVERSHOOT = 1.2f;
    private const float ITEM_POP_INTERVAL = 0.15f;
    private const float ITEM_FADE_DURATION = 0.7f;

    private const float BUTTON_FADE_DURATION = 0.4f;
    private const float BUTTON_START_SCALE = 0.7f;
    private const float BUTTON_POP_OVERSHOOT = 1.08f;

    private const float BURN_PARTICLE_LIFETIME = 1.0f;
    private const int BURN_PARTICLE_BURST_COUNT = 30;
    private const float BURN_PARTICLE_START_SIZE = 0.4f;
    private const float BURN_PARTICLE_START_SPEED = 3f;
    private const float BURN_PARTICLE_SHAPE_RADIUS = 0.15f;
    private const float BURN_PARTICLE_GRAVITY = 0.6f;
    private const int BURN_PARTICLE_TEX_SIZE = 64;

    [Header("演出対象")]
    [SerializeField] private RectTransform _rocketRect;
    [SerializeField] private Image _rocketImage;
    [SerializeField] private RectTransform _moonRect;
    [SerializeField] private TextMeshProUGUI _labelText;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private Button _btnRetry;
    [SerializeField] private Button _btnTitle;

    private Vector2 _rocketStartPos;
    private Vector3 _rocketStartScale;
    private Vector2 _labelRestPos;

    private CanvasGroup _retryGroup;
    private CanvasGroup _titleGroup;

    private bool _skipRequested;

    // 燃焼パーティクル用のマテリアルは全インスタンスで共有する
    private static Material _burnMaterial;

    /// <summary>
    /// 演出開始前の初期化。シーン上の初期配置をここで記憶する
    /// </summary>
    public void Init()
    {
        _rocketStartPos = _rocketRect.anchoredPosition;
        _rocketStartScale = _rocketRect.localScale;
        _labelRestPos = _labelText.rectTransform.anchoredPosition;

        _retryGroup = GetOrAddCanvasGroup(_btnRetry);
        _titleGroup = GetOrAddCanvasGroup(_btnTitle);
    }

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

    public void OnSkip()
    {
        _skipRequested = true;
    }

    private void SetInitialState(bool a_isClear)
    {
        _rocketRect.gameObject.SetActive(true);
        _rocketRect.anchoredPosition = _rocketStartPos;
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
            _labelText.rectTransform.anchoredPosition = _labelRestPos + Vector2.up * LABEL_FALL_START_OFFSET;
            _labelText.rectTransform.localScale = Vector3.one;
            _labelText.alpha = 1f;

            _scoreText.rectTransform.localScale = Vector3.zero;
            _timeText.rectTransform.localScale = Vector3.zero;
            _scoreText.alpha = 1f;
            _timeText.alpha = 1f;
        }
    }

    private IEnumerator RocketBurnRoutine()
    {
        Vector2 endPos = _rocketStartPos + Vector2.down * ROCKET_FALL_DISTANCE;

        yield return Tween(ROCKET_FALL_DURATION, t =>
        {
            float easeT = t * t;
            _rocketRect.anchoredPosition = Vector2.Lerp(_rocketStartPos, endPos, easeT);
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

    private IEnumerator RocketFlightRoutine()
    {
        // ロケットと月でアンカー設定が異なっても正しく到達するよう、ワールド座標同士で補間する
        Vector3 startPos = _rocketRect.position;
        Vector3 endPos = _moonRect.position;

        yield return Tween(ROCKET_FLIGHT_DURATION, t =>
        {
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            _rocketRect.position = Vector3.Lerp(startPos, endPos, easeT);
        });
    }

    private IEnumerator LabelFallBounceRoutine()
    {
        Vector2 startPos = _labelText.rectTransform.anchoredPosition;

        yield return Tween(LABEL_FALL_DURATION, t =>
        {
            float easeT = EaseOutBounce(t);
            _labelText.rectTransform.anchoredPosition = Vector2.Lerp(startPos, _labelRestPos, easeT);
        });
    }

    private IEnumerator LabelZoomFadeRoutine()
    {
        SetPivotPreservePosition(_labelText.rectTransform, new Vector2(0.5f, 0f));
        _labelText.rectTransform.localScale = Vector3.zero;

        yield return Tween(LABEL_ZOOM_FADE_DURATION, t =>
        {
            float easeT = 1f - Mathf.Pow(1f - t, 2f);
            _labelText.rectTransform.localScale = Vector3.one * easeT;
            _labelText.alpha = easeT;
        });
    }

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
            float scale = t < 0.6f
                ? Mathf.Lerp(0f, ITEM_POP_OVERSHOOT, t / 0.6f)
                : Mathf.Lerp(ITEM_POP_OVERSHOOT, 1f, (t - 0.6f) / 0.4f);
            a_rect.localScale = Vector3.one * scale;
        });
    }

    private IEnumerator ItemsFadeRoutine()
    {
        yield return Tween(ITEM_FADE_DURATION, t =>
        {
            _scoreText.alpha = t;
            _timeText.alpha = t;
        });
    }

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

        // コントローラー操作を見越し、専用カーソル画像の代わりにButtonのHighlighted色で選択状態を示す
        EventSystem.current.SetSelectedGameObject(_btnRetry.gameObject);
    }

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

    private void SpawnBurnParticles(RectTransform a_uiRect)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        // Canvas が Screen Space - Overlay のため、UI配下に直接ParticleSystemを置くと描画されない。
        // 画面上の位置をワールド座標に変換し、Canvas外にパーティクルを生成する。
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, a_uiRect.position);
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));

        GameObject burstObj = new GameObject("RocketBurnParticles");
        burstObj.transform.position = worldPos;

        ParticleSystem ps = burstObj.AddComponent<ParticleSystem>();
        // AddComponent直後は既定設定で自動再生されるため、設定が終わるまで止めておく
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

        // ランタイム生成のParticleSystemはマテリアル未設定で何も描画されないため、明示的に割り当てる
        ParticleSystemRenderer psRenderer = burstObj.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = GetBurnMaterial();

        ps.Play();
        Object.Destroy(burstObj, BURN_PARTICLE_LIFETIME + 0.5f);
    }

    private static Material GetBurnMaterial()
    {
        if (_burnMaterial == null)
        {
            // ビルドでSprites/Defaultが含まれない場合に備え、uGUIが必ず含めるUI/Defaultへフォールバック
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

    private static Texture2D CreateSoftCircleTexture(int a_size)
    {
        // 専用のパーティクル画像素材が無いため、中心から縁へ透明になる円をコードで生成する
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
