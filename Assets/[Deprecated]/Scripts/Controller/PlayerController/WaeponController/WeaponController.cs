using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : PlayerController
{
    public enum WeaponType { None, Gun, Knife }
    private WeaponType curWeaponType;

    public int Damage { get { return damage; } }

    [Header("���� ����")]
    protected float attackSpeed;  //����
    protected int damage;  //����

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
    /// ���̽�ƽ ���⿡ �°� ���� ȸ��
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

        if (curWeaponType == WeaponType.Knife) transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle - 90));  //���� ȸ���� "20 Evolving Weapons"�� �⺻ �̹����� �°� ����
        else transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }
    
    /// <summary>
    /// Ÿ���� ���⿡ �°� ���� ȸ��
    /// </summary>
    protected void RotateWeaponToTarget()
    {
        if (!characterController.GetTargetingBool()) return;

        if (curWeaponType == WeaponType.Knife) transform.rotation = Quaternion.Euler(new Vector3(0, 0, characterController.GetTargetingAngle() - 90));  //���� ȸ���� "20 Evolving Weapons"�� �⺻ �̹����� �°� ����
        else transform.rotation = Quaternion.Euler(new Vector3(0, 0, characterController.GetTargetingAngle()));
    }

    /// <summary>
    /// ���� ��� ���� ���� Ÿ���� ����
    /// </summary>
    /// <param name="weaponType"></param>
    protected void SetCurWeaponType(WeaponType weaponType = WeaponType.None)
    {
        curWeaponType = weaponType;
    }

    /// <summary>
    /// ���� ��� ���� ���� Ÿ���� ��������
    /// </summary>
    /// <returns></returns>
    public WeaponType GetCurWeaponType()
    {
        return curWeaponType;
    }

    /// <summary>
    /// ������ �������ͽ� �⺻ ����
    /// </summary>
    protected virtual void SetWeaponStadardStatus()
    {
    }
}
