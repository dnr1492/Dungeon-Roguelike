using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    protected enum PlayerName { None, Knight }
    protected PlayerName curPlayerName;

    [Header("공통")]
    protected Controller selectedPlayer;

    [Header("플레이어")]
    protected PlayerController playerController;
    protected CharacterController characterController;
    protected WeaponController weaponController;
    protected KnightController knightController;
    protected GunController gunController;
    protected KnifeController knifeController;

    [Header("몬스터")]
    protected MonsterController monsterController;
    protected GoblinController goblinController;

    protected virtual void Awake()
    {
        //하위 1
        playerController = FindObjectOfType<PlayerController>();
        monsterController = FindObjectOfType<MonsterController>();

        //하위 2
        characterController = FindObjectOfType<CharacterController>();
        weaponController = FindObjectOfType<WeaponController>();
        goblinController = FindObjectOfType<GoblinController>();

        //하위 3
        knightController = FindObjectOfType<KnightController>();
        knifeController = FindObjectOfType<KnifeController>();
        gunController = FindObjectOfType<GunController>();

        if (knightController.gameObject.activeSelf) curPlayerName = PlayerName.Knight;
    }

    /// <summary>
    /// 현재 컨트롤 중인 플레이어를 설정
    /// </summary>
    protected void SetSelectedPlayer()
    {
        if (curPlayerName == PlayerName.Knight) selectedPlayer = knightController;
    }
}
