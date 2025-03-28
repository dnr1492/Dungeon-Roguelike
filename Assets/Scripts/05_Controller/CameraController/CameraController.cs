using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : Controller
{
    [Header("ī�޶�")]
    private Camera mainCam;  //���� ī�޶�
    [SerializeField] private Vector3 SetMainCamOffset;  //���� ī�޶��� ������ ����

    [Header("�÷��̾�")]
    private Transform playerTr;  //�÷��̾��� ��ġ

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
    /// �÷��̾ ���� ī�޶� �̵�
    /// </summary>
    private void MoveCameraToPlayer()
    {
        mainCam.transform.position = playerTr.position + SetMainCamOffset;
        mainCam.transform.LookAt(playerTr.transform);
    }
}
