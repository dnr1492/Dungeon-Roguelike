using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : Controller
{
    [Header("공통")]
    protected Rigidbody2D curRigidbody2D;
    protected SpriteRenderer curSpriteRenderer;
    protected Animator playerAni;

    [Header("플레이어 조작")]
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
