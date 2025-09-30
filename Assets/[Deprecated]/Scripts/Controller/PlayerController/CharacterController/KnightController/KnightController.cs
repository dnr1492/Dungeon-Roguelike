using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnightController : CharacterController
{
    private void OnDrawGizmos()
    {
        //타겟팅 범위 디버그
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawSphere(transform.position, outoTargetingRange);
    }

    protected override void Init()
    {
        SetPlayerStadardStatus();

        hp = maxHp;
        mp = maxMp;
    }

    protected override void Awake()
    {
        base.Awake();
    }

    private void Update()
    {
        if (hp <= 0)
        {
            StartCoroutine(DiePlayerRoutine());
            return;
        }

        MovePlayer();

        FindTargeting();
    }

    protected override void SetPlayerStadardStatus()
    {
        moveSpeed = 3.0f;
        maxHp = 5;
        maxMp = 1;
    }
}
