using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;

    private readonly int maxHP = 3;
    private readonly bool destroyOnDeath = true;

    public int Current { get; private set; }
    public event Action OnDeath;

    #region 히트 플래시
    private Color originColor;
    private Color hitColor = new Color(1f, 0.4f, 0.4f, 1f);
    private int flashVer;

    private readonly float hitFlashTime = 0.06f;
    #endregion

    private void Awake()
    {
        Current = Mathf.Max(1, maxHP);

        originColor = spriteRenderer.color;
    }

    public void TakeDamage(int dmg)
    {
        if (Current <= 0) return;
        Current -= Mathf.Max(1, dmg);

        //히트 플래시
        if (spriteRenderer) HitFlashAsync().Forget();

        if (Current <= 0)
        {
            OnDeath?.Invoke();
            if (destroyOnDeath) Destroy(gameObject);
        }
    }

    private async UniTaskVoid HitFlashAsync()
    {
        flashVer++;
        int myVer = flashVer; 

        spriteRenderer.color = hitColor;

        //파괴 시 자동으로 취소되는 토큰 사용
        var token = this.GetCancellationTokenOnDestroy();
        await UniTask.Delay(TimeSpan.FromSeconds(hitFlashTime), cancellationToken: token);

        //더 최근 히트가 있으면 색 복구를 내버려 둠
        if (myVer == flashVer && spriteRenderer) spriteRenderer.color = originColor;
    }
}