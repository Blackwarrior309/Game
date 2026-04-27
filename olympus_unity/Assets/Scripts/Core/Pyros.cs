// Pyros.cs
// Ablegen in: Assets/Scripts/Core/Pyros.cs
// Tag: "Pyros" setzen im Inspector

using UnityEngine;
using System;

public class Pyros : MonoBehaviour
{
    [SerializeField] float maxHp = 500f;
    float hp;

    public static event Action<float, float> OnHpChanged;   // current, max
    public static event Action               OnDestroyed;

    void Awake()
    {
        tag = "Pyros";
        hp  = maxHp;
    }

    void Start() => OnHpChanged?.Invoke(hp, maxHp);

    public void TakeDamage(float amount)
    {
        hp = Mathf.Max(0f, hp - amount);
        OnHpChanged?.Invoke(hp, maxHp);
        FavorManager.Instance.OnPyrosDamaged();
        if (hp <= 0f) { OnDestroyed?.Invoke(); GameEvents.RaiseGameOver("pyros_destroyed"); }
    }

    public void Heal(float amount)
    {
        hp = Mathf.Min(maxHp, hp + amount);
        OnHpChanged?.Invoke(hp, maxHp);
    }
}
