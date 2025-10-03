using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Game/FX/AnimMap", fileName = "AnimMap")]
public class AnimMap : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public FxEventType eventType;  //Fire / Hit / Death ...
        public string[] triggers;      //이 이벤트 때 쏠 트리거 이름들
    }

    [SerializeField] private Entry[] entries;

    public bool TryGet(FxEventType evt, out string[] names)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e != null && e.eventType == evt && e.triggers != null && e.triggers.Length > 0)
                { names = e.triggers; return true; }
            }
        }
        names = null; return false;
    }
}
