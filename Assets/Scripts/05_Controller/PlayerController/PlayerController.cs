using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : Controller
{
    [Header("����")]
    protected Rigidbody2D curRigidbody2D;
    protected SpriteRenderer curSpriteRenderer;
    protected Animator playerAni;

    [Header("�÷��̾� ����")]
    protected Joystick joystick;

    protected virtual void Init()
    {
    }

    protected override void Awake()
    {
        base.Awake();

        curRigidbody2D = gameObject.GetComponent<Rigidbody2D>();
        curSpriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        playerAni = gameObject.GetComponent<Animator>();

        joystick = FindObjectOfType<Joystick>();

        SetSelectedPlayer();

        Init();
    }
}
