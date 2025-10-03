using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/FX/FxRuleSet")]
public class FxRuleSet : ScriptableObject
{
    [Serializable]
    public class Rule
    {
        [Header("조건")]
        public FxEventType eventType = FxEventType.None;
        public string requiredSurface;                      //비우면 무시
        public string requiredTargetTag;                    //비우면 무시
        public bool requireCrit = false;                    //false면 무시, true면 isCrit = =true 필요
        public int minDamage = 0;                           //피해량 하한 (0 = 무시)
        public int priority = 0;                            //클수록 우선

        [Header("출력")]
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
