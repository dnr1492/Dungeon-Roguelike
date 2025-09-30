using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPlayerHealthBar : MonoBehaviour
{
    [SerializeField] Health health;
    [SerializeField] Slider slider;
    [SerializeField] TextMeshProUGUI textHp;

    private readonly float smooth = 0.15f;  //0 = 즉시, >0 = 부드럽게

    private int animVersion;  //UniTask 애니메이션 취소 토큰 대용

    private void Awake()
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = false;

        health.OnHealthChanged += OnHealthChanged;
    }

    private void OnDestroy()
    {
        health.OnHealthChanged -= OnHealthChanged;

        //진행 중 애니메이션 종료
        animVersion++;
    }

    private void OnHealthChanged(int curHp, int maxHp)
    {
        float target = (maxHp > 0) ? (float)curHp / maxHp : 1f;

        if (smooth <= 0f)
        {
            slider.value = target;
            return;
        }

        textHp.text = $"{curHp} / {maxHp}";

        AnimateToAsync(target, ++animVersion).Forget();
    }

    private async UniTaskVoid AnimateToAsync(float target, int ver)
    {
        var token = this.GetCancellationTokenOnDestroy();

        while (!token.IsCancellationRequested &&
               ver == animVersion &&
               Mathf.Abs(slider.value - target) > 0.001f)
        {
            float lerpAlpha = 1f - Mathf.Pow(1f - 0.5f, Time.unscaledDeltaTime / Mathf.Max(0.0001f, smooth));
            slider.value = Mathf.Lerp(slider.value, target, lerpAlpha);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        if (!token.IsCancellationRequested && ver == animVersion)
            slider.value = target;
    }
}