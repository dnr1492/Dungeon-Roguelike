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

    private int animVersion;  //UniTask �ִϸ��̼� ��� ��ū ���

    private void Awake()
    {
        ring.type = Image.Type.Filled;
        ring.fillMethod = Image.FillMethod.Radial360;
        ring.fillOrigin = (int)Image.Origin360.Top;
        ring.fillClockwise = true;
        ring.fillAmount = player.CooldownRatio;

        player.OnFired += OnPlayerFired;

        //�� ���� �� ��ٿ��� ���� ������ ���� �ִ� ����
        if (player.CooldownRemaining > 0f)
            AnimateCooldown(player.CooldownRemaining, ++animVersion).Forget();
    }

    private void OnDestroy()
    {
        if (player) player.OnFired -= OnPlayerFired;
        animVersion++;  //���� �� �ִ� �ߴ�
    }

    private void OnDisable()
    {
        //��ư�� ��Ȱ��ȭ�ǰų� ȭ�� ��ȯ �� ���� ���°� ���� �ʵ��� ����
        if (!pressed) return;

        pressed = false;
        activePointerId = -1;
        if (player != null) player.FireButtonUp();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (player != null && player.CooldownRemaining > 0f) return;

        //�̹� ������ �ִ� ���̸� ���� (������ġ ����)
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
        //�հ����� ��ư ������ ����� Up�� �����ϰ� ����
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

        //�Ϸ�
        if (!token.IsCancellationRequested && ver == animVersion)
            ring.fillAmount = 1f;
    }
}
