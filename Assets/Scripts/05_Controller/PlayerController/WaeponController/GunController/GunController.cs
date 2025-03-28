using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : WeaponController
{
    [Header("공격")]
    [SerializeField] GunCoroutineManager gunCoroutineManager;  //비활성화 상태에서 코루틴을 관리를 하기 위해서 사용
    [SerializeField] private GameObject bulletGo;
    [SerializeField] private Transform fireTr;  //총구 (총알이 나가는 위치)

    [Header("총기류 스탯")]
    private float bulletSpeed;  //스탯

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
    /// 총알 생성
    /// </summary>
    public void CreateBullet()
    {
        GameObject bullet = Instantiate<GameObject>(bulletGo);
        bullet.transform.position = fireTr.transform.position;
        bullet.transform.rotation = transform.rotation;

        gunCoroutineManager.StartCoroutine(bullet, bulletSpeed);
    }

    /// <summary>
    /// On Hit 총알 제거
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
