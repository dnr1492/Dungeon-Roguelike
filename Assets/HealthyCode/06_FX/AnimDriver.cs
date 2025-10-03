using UnityEngine;

[DisallowMultipleComponent]
public class AnimDriver : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] AnimMap animMap;

    public void Play(FxEventType evt)
    {
        if (!enabled || !animator || !animMap) return;

        if (!animMap.TryGet(evt, out var triggers)) return;
        for (int i = 0; i < triggers.Length; i++)
        {
            var t = triggers[i];
            if (!string.IsNullOrEmpty(t)) animator.SetTrigger(t);
        }
    }

    public void SetMap(AnimMap map) { animMap = map; }
}
