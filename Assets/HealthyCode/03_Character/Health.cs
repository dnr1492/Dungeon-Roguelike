using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    [SerializeField] CharacterStats stats;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] UIDamageFloatingText damageFloatingTextPrefab;
    [SerializeField] Transform damageFloatingTextPivot;

    public int CurHP { get; private set; }
    public int MaxHP => stats.maxHP;

    public event Action OnDeath;
    public event Action<int, int> OnHealthChanged;  //curHp, maxHp

    #region ��Ʈ �÷���
    private Color originColor;
    private Color hitColor = new Color(1f, 0.4f, 0.4f, 1f);
    private int flashVer;

    private readonly float hitFlashTime = 0.06f;
    #endregion

    private readonly Dictionary<int, float> lastHitBy = new();  //�ֱ� �ǰ� ��� (������ ID �� ������ �ð�)

    private void Awake()
    {
        CurHP = stats.maxHP;

        originColor = spriteRenderer.color;

        OnHealthChanged?.Invoke(CurHP, MaxHP);
    }

    /// <summary>
    /// �ܼ� ������ ���� (������ ID ����). �Ѿ�/Ʈ��/ȯ�濡 ���.
    /// '�����ں� i-frame' ������ Ÿ�� �ʴ´�.
    /// </summary>
    public void TakeDamage(int dmg) => TakeHit(dmg, 0);

    /// <summary>
    /// ������ ID�� ������ ������ ����. ����/ƽ�� �� '���� ������'�� ���� ��Ʈ��
    /// perSourceIFrame �������� �����Ѵ�. �ٸ� ������/�ٸ� �Ѿ��� �״�� ����.
    /// attackerId���� ���� attacker.GetInstanceID()�� �ִ´�. (0�� ������ i-frame ������)
    /// </summary>
    public void TakeHit(int dmg, int attackerId)
    {
        if (CurHP <= 0) return;

        if (stats.usePerSourceIFrame && attackerId != 0)
        {
            float now = Time.time;
            if (lastHitBy.TryGetValue(attackerId, out var last) && now - last < stats.perSourceIFrame)
                return;
            lastHitBy[attackerId] = now;
        }

        CurHP -= dmg;

        //��Ʈ �÷���
        if (spriteRenderer) HitFlashAsync().Forget();

        OnHealthChanged?.Invoke(CurHP, MaxHP);

        var dt = GameManager.Instance.GetDamageFloatingText(damageFloatingTextPrefab);
        if (dt) dt.Show(dmg, damageFloatingTextPivot.position, Color.red, GameManager.Instance.ReturnDamageFloatingText);

        if (CurHP <= 0)
        {
            OnDeath.Invoke();
            if (stats.destroyOnDeath) Destroy(gameObject);
        }
    }

    /// <summary>
    /// �ǰ� �� ���� ǥ��
    /// </summary>
    /// <returns></returns>
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