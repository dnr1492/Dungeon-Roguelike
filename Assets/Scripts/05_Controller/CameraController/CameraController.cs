using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : Controller
{
    [Header("카메라")]
    private Camera mainCam;  //메인 카메라
    [SerializeField] private Vector3 SetMainCamOffset;  //메인 카메라의 각도를 설정

    [Header("플레이어")]
    private Transform playerTr;  //플레이어의 위치

    protected override void Awake()
    {
        base.Awake();

        SetSelectedPlayer();

        mainCam = FindObjectOfType<Camera>();
        playerTr = selectedPlayer.GetComponent<Transform>();
    }

    private void LateUpdate()
    {
        if (playerTr == null) return;

        MoveCameraToPlayer();
    }

    /// <summary>
    /// 플레이어를 따라 카메라 이동
    /// </summary>
    private void MoveCameraToPlayer()
    {
        mainCam.transform.position = playerTr.position + SetMainCamOffset;
        mainCam.transform.LookAt(playerTr.transform);
    }
}
