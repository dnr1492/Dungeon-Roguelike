using UnityEngine;
using System;

public enum FxEventType
{
    None,
    Fire,       //�߻�
    Hit,        //�ǰ� (������ �߻�)
    Death,      //���
    CritHit,    //ġ��Ÿ
}

[Serializable]
public struct FxContext
{
    public Transform origin;    //Play ���� ��ġ (����/������ ��)
    public Transform target;    //��� (�ǰ��� ��)
    public Vector3 hitPos;      //�浹 ���� (������ origin.position ���)
    public string surface;      //Flesh/Metal/Stone...
    public bool isCrit;         //ġ��Ÿ ����
    public int damage;          //���ط�
}

public interface IFxEventSource
{
    event Action<FxEventType, FxContext> OnFxEvent;
}