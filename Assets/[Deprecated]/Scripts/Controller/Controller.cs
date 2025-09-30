using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    protected enum PlayerName { None, Knight }
    protected PlayerName curPlayerName;

    [Header("����")]
    protected Controller selectedPlayer;

    [Header("�÷��̾�")]
    protected PlayerController playerController;
    protected CharacterController characterController;
    protected WeaponController weaponController;
    protected KnightController knightController;
    protected GunController gunController;
    protected KnifeController knifeController;

    [Header("����")]
    protected MonsterController monsterController;
    protected GoblinController goblinController;

    protected virtual void Awake()
    {
        //���� 1
        playerController = FindObjectOfType<PlayerController>();
        monsterController = FindObjectOfType<MonsterController>();

        //���� 2
        characterController = FindObjectOfType<CharacterController>();
        weaponController = FindObjectOfType<WeaponController>();
        goblinController = FindObjectOfType<GoblinController>();

        //���� 3
        knightController = FindObjectOfType<KnightController>();
        knifeController = FindObjectOfType<KnifeController>();
        gunController = FindObjectOfType<GunController>();

        if (knightController.gameObject.activeSelf) curPlayerName = PlayerName.Knight;
    }

    /// <summary>
    /// ���� ��Ʈ�� ���� �÷��̾ ����
    /// </summary>
    protected void SetSelectedPlayer()
    {
        if (curPlayerName == PlayerName.Knight) selectedPlayer = knightController;
    }
}
