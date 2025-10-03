using UnityEngine;
using System;

public enum FxEventType
{
    None,
    Fire,       //발사
    Hit,        //피격 (데미지 발생)
    Death,      //사망
    CritHit,    //치명타
}

[Serializable]
public struct FxContext
{
    public Transform origin;    //Play 기준 위치 (무기/맞은곳 등)
    public Transform target;    //대상 (피격자 등)
    public Vector3 hitPos;      //충돌 지점 (없으면 origin.position 사용)
    public string surface;      //Flesh/Metal/Stone...
    public bool isCrit;         //치명타 여부
    public int damage;          //피해량
}

public interface IFxEventSource
{
    event Action<FxEventType, FxContext> OnFxEvent;
}