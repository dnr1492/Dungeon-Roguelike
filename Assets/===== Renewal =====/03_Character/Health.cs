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

    #region ��Ʈ �÷���
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

        //��Ʈ �÷���
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

        //�ı� �� �ڵ����� ��ҵǴ� ��ū ���
        var token = this.GetCancellationTokenOnDestroy();
        await UniTask.Delay(TimeSpan.FromSeconds(hitFlashTime), cancellationToken: token);

        //�� �ֱ� ��Ʈ�� ������ �� ������ ������ ��
        if (myVer == flashVer && spriteRenderer) spriteRenderer.color = originColor;
    }
}