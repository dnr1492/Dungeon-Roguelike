using UnityEngine;
using System;

[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    private readonly int maxHP = 3;
    private readonly bool destroyOnDeath = true;

    public int Current { get; private set; }
    public event Action OnDeath;

    private void Awake()
    {
        Current = Mathf.Max(1, maxHP);
    }

    public void TakeDamage(int dmg)
    {
        if (Current <= 0) return;
        Current -= Mathf.Max(1, dmg);

        if (Current <= 0)
        {
            OnDeath?.Invoke();
            if (destroyOnDeath) Destroy(gameObject);
        }
    }
}