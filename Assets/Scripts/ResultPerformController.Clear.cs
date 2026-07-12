using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// リザルト演出のクリア(勝利)専用部分。
/// 流れ: ロケットが画面下端から弧を描いて月上空へ → 降下して着地(パーティクル+潰れ+影)
///       → Clear文字がふあっと拡大フェードイン → 項目が同時フェードイン → (共通)ボタン表示
/// </summary>
public partial class ResultPerformController
{
    // ===== クリア調整値 =====
    [Header("クリア - ロケット飛行")]
    [Tooltip("ロケットが画面下端から月上空(ホバー地点)まで上昇しきるまでの秒数")]
    public float ROCKET_FLIGHT_DURATION = 1.2f;
    [Tooltip("月に近づくにつれて減速するイージングの強さ。1で等速、大きいほど月の手前で急激に減速する")]
    public float ROCKET_FLIGHT_EASE_POWER = 4f;
    [Tooltip("月に到達した瞬間のロケットの縮小率(開始スケールに対する倍率)。小さいほど遠近感が強く出る")]
    public float ROCKET_MOON_ARRIVAL_SCALE = 0.35f;
    [Tooltip("発進位置(画面下端)を中心から左右にずらすオフセット。マイナスで左、プラスで右。月までの軌道を斜めにしたいときに使う")]
    public float ROCKET_LAUNCH_X_OFFSET = 0f;
    [Tooltip("発進地点から月上空(ホバー地点)までの軌道を弧状に膨らませる量(ワールド単位)。0で直線。符号でどちら側に膨らむかが変わる")]
    public float ROCKET_ARC_HEIGHT = 1f;
    [Tooltip("月へ向かう際、進行方向にロケットの向きを合わせる。ロケットの絵が正面(上向き)に描かれている前提で、そこからのズレ(度)を補正できる")]
    public float ROCKET_SPRITE_FORWARD_OFFSET = 0f;

    [Header("クリア - ロケット着地")]
    [Tooltip("着地前に月の上空でホバーする高さ(月からの上方向オフセット、ワールド単位)")]
    public float ROCKET_LANDING_HOVER_HEIGHT = 1f;
    [Tooltip("ホバー地点から月の表面まで降下しきるまでの秒数")]
    public float ROCKET_LANDING_DURATION = 0.6f;
    [Tooltip("着地の瞬間に潰れる度合い(1.0で潰れなし、小さいほど強く潰れて着地の重みが出る)")]
    public float ROCKET_LANDING_SQUASH = 0.7f;
    [Tooltip("着地で潰れてから元のスケールに戻りきるまでの秒数")]
    public float ROCKET_LANDING_SQUASH_DURATION = 0.2f;
    [Tooltip("着地が完全に終わってから、Clear文字などの表示が始まるまでの間(秒)。着地の余韻をとるための間")]
    public float ROCKET_LANDING_PAUSE_DURATION = 0.4f;

    // 発生量・色・寿命等はプレハブ側(Assets/Prefabs/ロケット軌跡.prefab)で作り込み済みのため、
    // ここでは生成間隔・止めるタイミング・描画順・後始末だけを調整する
    [Header("クリア - 軌跡パーティクル")]
    [Tooltip("移動中、軌跡パーティクルを生成する間隔(秒)")]
    public float ROCKET_TRAIL_INTERVAL = 0.15f;
    [Tooltip("月までの移動(接近+着地降下)全体のうち、この割合まで進んだら軌跡の生成をやめる(0〜1)。1にすると止めずに出し続ける")]
    public float ROCKET_TRAIL_STOP_PROGRESS = 0.7f;
    [Tooltip("軌跡パーティクルの描画順(sortingOrder)。ロケット・背景と同じCanvas/SortingLayerに対して確実に手前へ出すための値")]
    public int TRAIL_PARTICLE_SORTING_ORDER = 10;
    [Tooltip("軌跡パーティクルの寿命(フェードアウト等)が終わるのを待つ安全マージンの秒数")]
    public float TRAIL_PARTICLE_DESTROY_MARGIN = 0.5f;

    // 発生量・色・寿命等はプレハブ側(Assets/Prefabs/ロケット着.prefab)で作り込み済みのため、
    // ここでは描画順と後始末のタイミングだけを調整する
    [Header("クリア - 着地パーティクル")]
    [Tooltip("着地パーティクルの描画順(sortingOrder)。ロケット・背景と同じCanvas/SortingLayerに対して確実に手前へ出すための値")]
    public int LANDING_PARTICLE_SORTING_ORDER = 10;
    [Tooltip("着地パーティクルの寿命(フェードアウト等)が終わるのを待つ安全マージンの秒数")]
    public float LANDING_PARTICLE_DESTROY_MARGIN = 0.5f;
    [Tooltip("着地パーティクルの発生位置を、ロケットの足元(自動算出した下端)からさらに上下にずらす微調整量(ワールド単位)。マイナスでより上、プラスでより下")]
    public float LANDING_PARTICLE_OFFSET_Y = 0f;

    // 見た目(形・色・濃さ)はシーン上の「かげ」オブジェクト側で作り込み済みのため、
    // ここでは位置合わせとフェードインのタイミングだけを調整する
    [Header("クリア - 着地シャドウ")]
    [Tooltip("影の位置を、ロケットの足元(自動算出)からさらに上下にずらす微調整量(ローカル単位)。マイナスでより下")]
    public float SHADOW_OFFSET_Y = 0f;
    [Tooltip("着地時に影がフェードインしきるまでの秒数")]
    public float SHADOW_FADE_DURATION = 0.3f;

    [Header("クリア - 再発射")]
    [Tooltip("ボタン表示が終わってから、ロケットが震え始めるまでの待機秒数")]
    public float ROCKET_RELAUNCH_WAIT = 3f;
    [Tooltip("プルプル震えている秒数")]
    public float ROCKET_SHAKE_DURATION = 1.5f;
    [Tooltip("震えの振幅(ピクセル相当)。ロケットが左右にどれだけ揺れるか")]
    public float ROCKET_SHAKE_AMPLITUDE = 3f;
    [Tooltip("1秒あたりの震えの回数")]
    public float ROCKET_SHAKE_FREQUENCY = 30f;
    [Tooltip("ゴゴゴと加速しながら上に飛んでいく秒数")]
    public float ROCKET_RELAUNCH_DURATION = 2f;
    [Tooltip("再発射のイージングの強さ。大きいほど出だしがゆっくりで後半に急加速する")]
    public float ROCKET_RELAUNCH_EASE_POWER = 3f;
    [Tooltip("上に飛んで画面外に出てから、帰還フライトを始めるまでの間(秒)")]
    public float ROCKET_RETURN_WAIT = 0.5f;
    [Tooltip("画面上側中央から画面左下へ弧を描いて帰還するまでの秒数")]
    public float ROCKET_RETURN_DURATION = 2.5f;
    [Tooltip("帰還時のロケットのスケール倍率(着地時スケールに対する倍率)。遠景で小さく飛んでいく感じ")]
    public float ROCKET_RETURN_SCALE = 0.6f;
    [Tooltip("帰還軌道の弧の膨らみ(ワールド単位)。正で右に膨らむ、負で左に膨らむ")]
    public float ROCKET_RETURN_ARC_HEIGHT = -2f;
    [Tooltip("帰還のイージングの強さ。大きいほど出だしがゆっくりで後半に加速する")]
    public float ROCKET_RETURN_EASE_POWER = 2f;

    [Header("クリア - ラベル / 項目")]
    [Tooltip("Clear文字が下端中心を軸に拡大しながらフェードインする秒数")]
    public float LABEL_ZOOM_FADE_DURATION = 0.8f;
    [Tooltip("Clear文字の色")]
    public Color CLEAR_LABEL_COLOR = Color.white;
    [Tooltip("スコア/タイムが同時に静かにフェードインする秒数")]
    public float ITEM_FADE_DURATION = 0.7f;

    // ===== クリア専用参照 =====
    [Header("クリア - 参照")]
    [Tooltip("月のRectTransform。ロケットが向かう目的地")]
    [SerializeField] private RectTransform _moonRect;
    [Tooltip("月へ着地した瞬間に生成する着地パーティクルのプレハブ(Assets/Prefabs/ロケット着.prefab)")]
    [SerializeField] private ParticleSystem _landingParticlePrefab;
    [Tooltip("月への移動中に一定間隔で残す軌跡パーティクルのプレハブ(Assets/Prefabs/ロケット軌跡.prefab)")]
    [SerializeField] private ParticleSystem _trailParticlePrefab;
    [Tooltip("着地の足元に表示するシャドウのImage(シーン上の「かげ」オブジェクト)")]
    [SerializeField] private Image _shadowImage;

    // クリア時の発進位置(画面下端の外 + 左右オフセット)。敗北の落下終点とは独立させている
    private Vector2 _rocketLaunchPos;

    // 着地後の状態を保持(再発射演出の起点に使う)
    private Vector3 _rocketLandedScale;
    private Vector3 _rocketLandedPos;

    // シャドウの「見えている時」の目標アルファ値。シーン側で設定された値をInit()時に読み取って使う
    private float _shadowTargetAlpha;

    /// <summary>
    /// クリア演出用の初期化。共通のInit()から呼ばれる。
    /// </summary>
    private void InitClear()
    {
        _rocketLaunchPos = _rocketOffScreenBottomPos + new Vector2(ROCKET_LAUNCH_X_OFFSET, 0f);

        if (_shadowImage != null)
        {
            _shadowTargetAlpha = _shadowImage.color.a;
        }
    }

    /// <summary>
    /// クリア演出の初期配置。共通のSetInitialState()から呼ばれる。
    /// </summary>
    private void SetInitialStateClear()
    {
        // 画面下端の外からスタートし、月へ向かって上昇する
        _rocketRect.anchoredPosition = _rocketLaunchPos;

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

    /// <summary>
    /// ロケットが画面下端から月の上空(ホバー地点)まで上昇して近づくにつれて縮小し、
    /// そこから月の表面まで降下して着地する。着地の瞬間は少し潰れてから元に戻り(重みのある着地感)、
    /// 着地パーティクルと足元の影を出す。
    /// ロケットと月は親は同じだがアンカー/ピボットが異なるため、anchoredPositionではなく
    /// ワールド座標(position)同士で補間することで正しく月へ到達させる。
    /// Skip時はパーティクルを発生させず即座に最終状態へスナップする。
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

            // 着地後のロケットのスケール・位置を記録しておく(発射演出で使う)
        _rocketLandedScale = arrivalScale;
        _rocketLandedPos = _rocketRect.position;

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
    /// 着地後のロケットが、一定秒数待ったあとプルプル震え → ゴゴゴと加速上昇(土埃パーティクル付き)
    /// → 画面外に出た後、上側中央から再登場して弧を描きながら画面左下へ帰還(軌跡パーティクル付き)する。
    /// </summary>
    private IEnumerator RocketRelaunchRoutine()
    {
        // 待機
        yield return WaitOrSkip(ROCKET_RELAUNCH_WAIT);

        // プルプル震え: 着地位置を中心に高周波で左右に揺らす
        Vector3 shakeCenter = _rocketLandedPos;
        float shakeElapsed = 0f;
        while (shakeElapsed < ROCKET_SHAKE_DURATION)
        {
            shakeElapsed += Time.deltaTime;
            float offsetX = Mathf.Sin(shakeElapsed * ROCKET_SHAKE_FREQUENCY * Mathf.PI * 2f) * ROCKET_SHAKE_AMPLITUDE * _rocketRect.lossyScale.x;
            _rocketRect.position = shakeCenter + Vector3.right * offsetX;
            yield return null;
        }
        _rocketRect.position = shakeCenter;

        // 影をフェードアウト + 発射地点に土埃パーティクル(着地パーティクルを流用)
        if (_shadowImage != null)
        {
            yield return Tween(0.2f, t =>
            {
                SetImageAlpha(_shadowImage, Mathf.Lerp(_shadowTargetAlpha, 0f, t));
            });
        }
        SpawnLandingParticleInstance();

        // ゴゴゴ加速上昇: ease-inで最初はゆっくり、どんどん速くなる
        Vector3 launchStart = _rocketRect.position;
        RectTransform canvasRect = _rocketRect.parent as RectTransform;
        float canvasHalfHeight = canvasRect != null ? canvasRect.rect.height * 0.5f : 1000f;
        float canvasHalfWidth = canvasRect != null ? canvasRect.rect.width * 0.5f : 500f;
        float rocketHeight = _rocketRect.rect.height * _rocketLandedScale.y;
        Vector3 launchEnd = launchStart + Vector3.up * (canvasHalfHeight + rocketHeight + SCREEN_EDGE_MARGIN);

        yield return Tween(ROCKET_RELAUNCH_DURATION, t =>
        {
            float easeT = Mathf.Pow(t, ROCKET_RELAUNCH_EASE_POWER);
            _rocketRect.position = Vector3.Lerp(launchStart, launchEnd, easeT);
        });

        // --- 帰還フライト ---
        yield return WaitOrSkip(ROCKET_RETURN_WAIT);

        // 画面上側中央付近から再登場
        Vector3 returnStart = _rocketRect.parent.TransformPoint(new Vector3(0f, canvasHalfHeight + rocketHeight, 0f));
        // 画面左下の外へ向かう
        Vector3 returnEnd = _rocketRect.parent.TransformPoint(new Vector3(
            -(canvasHalfWidth + rocketHeight + SCREEN_EDGE_MARGIN),
            -(canvasHalfHeight * 0.3f),
            0f));

        Vector3 returnScale = _rocketLandedScale * ROCKET_RETURN_SCALE;
        _rocketRect.position = returnStart;
        _rocketRect.localScale = returnScale;

        // 弧の制御点
        Vector3 returnMid = Vector3.Lerp(returnStart, returnEnd, 0.5f);
        Vector3 returnArcNormal = new Vector3(-(returnEnd - returnStart).y, (returnEnd - returnStart).x, 0f).normalized;
        Vector3 returnControlPoint = returnMid + returnArcNormal * ROCKET_RETURN_ARC_HEIGHT;

        // 帰還フライト中の回転(進行方向を向く)と軌跡パーティクル
        float returnTrailTimer = 0f;
        float returnTotalDuration = ROCKET_RETURN_DURATION;
        yield return Tween(ROCKET_RETURN_DURATION, t =>
        {
            float easeT = Mathf.Pow(t, ROCKET_RETURN_EASE_POWER);
            Vector3 currentPos = QuadraticBezier(returnStart, returnControlPoint, returnEnd, easeT);
            _rocketRect.position = currentPos;

            // 進行方向に合わせて回転
            float nextT = Mathf.Clamp01(easeT + 0.01f);
            Vector3 nextPos = QuadraticBezier(returnStart, returnControlPoint, returnEnd, nextT);
            Vector3 dir = nextPos - currentPos;
            if (dir.sqrMagnitude > 0.0001f)
            {
                float angle = ComputeFacingAngle(dir);
                _rocketRect.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            // 軌跡パーティクル(帰還中ずっと出す)
            float moveProgress = t * ROCKET_RETURN_DURATION / returnTotalDuration;
            if (!_skipRequested && moveProgress < 0.9f)
            {
                returnTrailTimer += Time.deltaTime;
                if (returnTrailTimer >= ROCKET_TRAIL_INTERVAL)
                {
                    returnTrailTimer = 0f;
                    SpawnTrailParticleInstance();
                }
            }
        });

        _rocketRect.gameObject.SetActive(false);
    }

    /// <summary>
    /// Clear文字が下端中心を軸に拡大しながらフェードインする。
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
    /// スコア・タイムを同時に静かにフェードインさせる。
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
}
