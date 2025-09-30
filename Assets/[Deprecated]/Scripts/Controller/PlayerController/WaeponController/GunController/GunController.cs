using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : WeaponController
{
    [Header("����")]
    [SerializeField] GunCoroutineManager gunCoroutineManager;  //��Ȱ��ȭ ���¿��� �ڷ�ƾ�� ������ �ϱ� ���ؼ� ���
    [SerializeField] private GameObject bulletGo;
    [SerializeField] private Transform fireTr;  //�ѱ� (�Ѿ��� ������ ��ġ)

    [Header("�ѱ�� ����")]
    private float bulletSpeed;  //����

    protected override void Init()
    {
        SetWeaponStadardStatus();
        SetCurWeaponType(WeaponType.Gun);
    }

    protected override void Awake()
    {
        base.Awake();
    }

    private void Update()
    {
        RotateWeapon();
        RotateWeaponToTarget();
    }

    protected override void OnEnable()
    {
        SetCurWeaponType(WeaponType.Gun);
    }

    /// <summary>
    /// �Ѿ� ����
    /// </summary>
    public void CreateBullet()
    {
        GameObject bullet = Instantiate<GameObject>(bulletGo);
        bullet.transform.position = fireTr.transform.position;
        bullet.transform.rotation = transform.rotation;

        gunCoroutineManager.StartCoroutine(bullet, bulletSpeed);
    }

    /// <summary>
    /// On Hit �Ѿ� ����
    /// </summary>
    /// <param name="bullet"></param>
    public void DestroyOnHitBullet(GameObject bullet)
    {
        Destroy(bullet);
    }

    protected override void SetWeaponStadardStatus()
    {
        //attackSpeed = ?;
        damage = 1;
        bulletSpeed = 5f;
    }
}
