using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : PlayerController
{
    public enum WeaponType { None, Gun, Knife }
    private WeaponType curWeaponType;

    public int Damage { get { return damage; } }

    [Header("무기 스탯")]
    protected float attackSpeed;  //스탯
    protected int damage;  //스탯

    protected override void Init()
    {
    }

    protected override void Awake()
    {
        base.Awake();
    }

    protected virtual void OnEnable()
    {
    }

    /// <summary>
    /// 조이스틱 방향에 맞게 무기 회전
    /// </summary>
    protected void RotateWeapon()
    {
        if (joystick.Horizontal == 0 || joystick.Vertical == 0) return;
        if (characterController.GetTargetingBool()) return;

        Vector3 rightMovement = Vector3.right * joystick.Horizontal;
        Vector3 upMovement = Vector3.up * joystick.Vertical;

        float x = rightMovement.x + upMovement.x;
        float y = rightMovement.y + upMovement.y;
        float angle = Mathf.Atan2(y, x) * Mathf.Rad2Deg;

        if (curWeaponType == WeaponType.Knife) transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle - 90));  //무기 회전을 "20 Evolving Weapons"의 기본 이미지에 맞게 설정
        else transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }
    
    /// <summary>
    /// 타겟팅 방향에 맞게 무기 회전
    /// </summary>
    protected void RotateWeaponToTarget()
    {
        if (!characterController.GetTargetingBool()) return;

        if (curWeaponType == WeaponType.Knife) transform.rotation = Quaternion.Euler(new Vector3(0, 0, characterController.GetTargetingAngle() - 90));  //무기 회전을 "20 Evolving Weapons"의 기본 이미지에 맞게 설정
        else transform.rotation = Quaternion.Euler(new Vector3(0, 0, characterController.GetTargetingAngle()));
    }

    /// <summary>
    /// 현재 사용 중인 무기 타입을 설정
    /// </summary>
    /// <param name="weaponType"></param>
    protected void SetCurWeaponType(WeaponType weaponType = WeaponType.None)
    {
        curWeaponType = weaponType;
    }

    /// <summary>
    /// 현재 사용 중인 무기 타입을 가져오기
    /// </summary>
    /// <returns></returns>
    public WeaponType GetCurWeaponType()
    {
        return curWeaponType;
    }

    /// <summary>
    /// 무기의 스테이터스 기본 설정
    /// </summary>
    protected virtual void SetWeaponStadardStatus()
    {
    }
}
