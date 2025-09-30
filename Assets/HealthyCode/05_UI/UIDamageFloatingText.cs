using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System;

public class UIDamageFloatingText : MonoBehaviour
{
    private readonly float duration = 0.65f;         //전체 재생시간
    private readonly float riseDistance = 42f;       //직선 기준 이동 거리(px)
    private readonly float spawnJitterX = 20f;       //시작점 좌우 지터(px)
    private readonly float angleMaxDeg = 22f;        //상승 각도 범위 — 직선 상승만 아니게
    private readonly float upThenFallChance = 0.7f;  //상승 후 낙하 확률

    //Sway(흔들림)
    private readonly float swayAmp = 6f;  //좌우 흔들림 진폭(px)
    private readonly float swayFreq = 3f;  //초당 진동수

    //Fall (상승 → 낙하 모드 전용)
    private readonly Vector2 upTimeRange = new Vector2(0.18f, 0.32f);  //상승 유지 시간
    private readonly float fallGravity = 260f;  //낙하 중 가속(px/s)
    private readonly float upSpeedScale = 1.0f;  //초기 상승 속도 스케일
    private readonly float fallSideMinSpeed = 60f;  //낙하 전환 시 최소 수평 속도(px/s)
    private readonly float fallSideJitter = 30f;  //낙하 전환 시 일회성 수평 가감속(px/s)
    private readonly float fallExtraTime = 0.7f;  //낙하 연출 확보용 추가 시간

    private Camera cam;
    private RectTransform uiParent;
    private TextMeshProUGUI txt;
    private RectTransform rect;
    private int animVersion;

    private void Awake()
    {
        cam = GameManager.Instance.PlayerCam;
        uiParent = GameManager.Instance.UIDamageFloatingTextParent;
        txt = GetComponent<TextMeshProUGUI>();
        rect = GetComponent<RectTransform>();

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 풀에서 꺼낸 후 호출: 표시 + 애니메이션 후 onComplete(this)
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="worldPos"></param>
    /// <param name="color"></param>
    /// <param name="onComplete"></param>
    public void Show(int amount, Vector3 worldPos, Color color, Action<UIDamageFloatingText> onComplete)
    {
        transform.SetParent(uiParent, false);

        //World → Screen → Canvas local (Overlay는 ScreenPointToLocalPointInRectangle의 camera = null)
        Vector2 screen = cam.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(uiParent, screen, null, out var anchored);

        //시작 좌표 + 스폰 지터
        rect.anchoredPosition = anchored + new Vector2(
            (spawnJitterX > 0f) ? UnityEngine.Random.Range(-spawnJitterX, spawnJitterX) : 0f,
            0f
        );

        txt.text = amount.ToString();
        color.a = 1f;
        txt.color = color;

        gameObject.SetActive(true);
        AnimateAsync(++animVersion, onComplete).Forget();
    }

    private async UniTaskVoid AnimateAsync(int version, Action<UIDamageFloatingText> onComplete)
    {
        var token = this.GetCancellationTokenOnDestroy();
        float t = 0f;

        Vector2 start = rect.anchoredPosition;
        float angleDeg = (angleMaxDeg > 0f) ? UnityEngine.Random.Range(-angleMaxDeg, angleMaxDeg) : 0f;
        Vector2 dirUp = Quaternion.Euler(0f, 0f, angleDeg) * Vector2.up;

        bool useUpThenFall = UnityEngine.Random.value < upThenFallChance;
        //모드 A: 다양한 방향 + 스웨이(좌우 흔들림)
        if (!useUpThenFall)
        {
            Vector2 end = start + dirUp * riseDistance;
            while (!token.IsCancellationRequested && version == animVersion && t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);

                Vector2 pos = Vector2.LerpUnclamped(start, end, EaseOutQuad(u));
                if (swayAmp > 0f && swayFreq > 0f)
                    pos.x += Mathf.Sin(u * Mathf.PI * 2f * swayFreq) * swayAmp * (1f - u);

                rect.anchoredPosition = pos;

                var c = txt.color; c.a = 1f - u; txt.color = c;

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        //모드 B: 상승 → 낙하(랜덤)
        else
        {
            float upTime = Mathf.Clamp(UnityEngine.Random.Range(upTimeRange.x, upTimeRange.y), 0.05f, duration * 0.8f);
            float upSpeed = (riseDistance / Mathf.Max(0.1f, duration)) * upSpeedScale;

            Vector2 vel = dirUp * upSpeed;
            Vector2 pos = start;

            bool enteredFall = false;

            float sideSign = (Mathf.Abs(dirUp.x) > 0.1f) ? Mathf.Sign(dirUp.x)
                                             : (UnityEngine.Random.value < 0.5f ? -1f : 1f);

            float dur = duration + fallExtraTime;

            while (!token.IsCancellationRequested && version == animVersion && t < dur)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                float u = Mathf.Clamp01(t / duration);

                if (t > upTime)
                {
                    vel += Vector2.down * (fallGravity * dt);

                    if (!enteredFall)
                    {
                        if (vel.y > 0f) vel.y = 0f;

                        float targetX = sideSign * fallSideMinSpeed
                                        + UnityEngine.Random.Range(-fallSideJitter, fallSideJitter);

                        if (Mathf.Abs(vel.x) < Mathf.Abs(targetX))
                            vel.x = targetX;

                        enteredFall = true;
                    }

                    if (Mathf.Abs(vel.x) < fallSideMinSpeed * 0.9f)
                        vel.x = sideSign * fallSideMinSpeed;

                    vel.x += sideSign * 10f * dt;
                }

                float sway = (swayAmp > 0f && swayFreq > 0f) ? Mathf.Sin(t * Mathf.PI * 2f * swayFreq) * swayAmp : 0f;

                pos += vel * dt;
                rect.anchoredPosition = new Vector2(pos.x + sway, pos.y);

                var c = txt.color; c.a = 1f - u; txt.color = c;

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        if (!token.IsCancellationRequested && version == animVersion)
            onComplete?.Invoke(this);
    }

    private static float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
}
