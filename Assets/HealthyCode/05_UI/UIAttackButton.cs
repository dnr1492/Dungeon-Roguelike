using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIAttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] Player player;
    [SerializeField] Image ring;

    private int activePointerId = -1;
    private bool pressed;

    private int animVersion;  //UniTask 애니메이션 취소 토큰 대용

    private void Awake()
    {
        ring.type = Image.Type.Filled;
        ring.fillMethod = Image.FillMethod.Radial360;
        ring.fillOrigin = (int)Image.Origin360.Top;
        ring.fillClockwise = true;
        ring.fillAmount = player.CooldownRatio;

        player.OnFired += OnPlayerFired;

        //씬 진입 시 쿨다운이 남아 있으면 감소 애니 시작
        if (player.CooldownRemaining > 0f)
            AnimateCooldown(player.CooldownRemaining, ++animVersion).Forget();
    }

    private void OnDestroy()
    {
        if (player) player.OnFired -= OnPlayerFired;
        animVersion++;  //진행 중 애니 중단
    }

    private void OnDisable()
    {
        //버튼이 비활성화되거나 화면 전환 시 눌림 상태가 남지 않도록 복구
        if (!pressed) return;

        pressed = false;
        activePointerId = -1;
        if (player != null) player.FireButtonUp();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (player != null && player.CooldownRemaining > 0f) return;

        //이미 누르고 있는 중이면 무시 (다중터치 방지)
        if (pressed) return;  

        pressed = true;
        activePointerId = eventData.pointerId;
        if (player != null) player.FireButtonDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!pressed || eventData.pointerId != activePointerId) return;

        pressed = false;
        activePointerId = -1;
        if (player != null) player.FireButtonUp();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //손가락이 버튼 영역을 벗어나면 Up과 동일하게 해제
        if (!pressed || eventData.pointerId != activePointerId) return;

        pressed = false;
        activePointerId = -1;
        if (player != null) player.FireButtonUp();
    }

    private void OnPlayerFired(float interval)
    {
        ring.fillAmount = 0f;
        AnimateCooldown(interval, ++animVersion).Forget();
    }

    private async UniTaskVoid AnimateCooldown(float duration, int ver)
    {
        var token = this.GetCancellationTokenOnDestroy();
        float t = 0f;
        while (!token.IsCancellationRequested && ver == animVersion && t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / duration);
            ring.fillAmount = progress;
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        //완료
        if (!token.IsCancellationRequested && ver == animVersion)
            ring.fillAmount = 1f;
    }
}
