using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/FX/FxRuleSet")]
public class FxRuleSet : ScriptableObject
{
    [Serializable]
    public class Rule
    {
        [Header("����")]
        public FxEventType eventType = FxEventType.None;
        public string requiredSurface;                      //���� ����
        public string requiredTargetTag;                    //���� ����
        public bool requireCrit = false;                    //false�� ����, true�� isCrit = =true �ʿ�
        public int minDamage = 0;                           //���ط� ���� (0 = ����)
        public int priority = 0;                            //Ŭ���� �켱

        [Header("���")]
        public AudioClip sfx;
        public GameObject vfxPrefab;
        public float vfxLife = 0.25f;
        public int hitStopMs = 0;
        public string cameraShakeKey;
    }

    [SerializeField] List<Rule> rules = new();

    public Rule Resolve(FxEventType evt, in FxContext ctx)
    {
        Rule best = null;
        int bestPrio = int.MinValue;

        for (int i = 0; i < rules.Count; i++)
        {
            var r = rules[i];
            if (r.eventType != evt) continue;

            if (!string.IsNullOrEmpty(r.requiredSurface) &&
                !string.Equals(r.requiredSurface, ctx.surface, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrEmpty(r.requiredTargetTag))
            {
                var tag = ctx.target ? ctx.target.tag : null;
                if (!string.Equals(tag, r.requiredTargetTag, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            if (r.requireCrit && !ctx.isCrit) continue;
            if (r.minDamage > 0 && ctx.damage < r.minDamage) continue;

            if (r.priority >= bestPrio) { best = r; bestPrio = r.priority; }
        }
        return best;
    }
}
