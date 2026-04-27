// AthenaAvatar.cs
// Ablegen in: Assets/Scripts/Gods/Avatars/AthenaAvatar.cs
// Athena-Avatar: strategische Beschützerin. Pyros-Barriere = Pyros wird
// pro Tick geheilt UND Feinde im Pyros-Nahbereich werden weggebrannt.
// Zusätzlich gibt der Avatar dem Spieler temporäre Rüstung.

using UnityEngine;
using System.Collections;

public class AthenaAvatar : AvatarBase
{
    [Header("Pyros-Barriere")]
    [SerializeField] float pyrosHealAmount      = 25f;
    [SerializeField] float barrierRadius        = 8f;     // um Pyros
    [SerializeField] float barrierDamage        = 75f;

    [Header("Wisdom-Burst (am Ziel)")]
    [SerializeField] float burstRadius          = 5f;
    [SerializeField] float burstDamage          = 40f;

    [Header("Spieler-Rüstung")]
    [SerializeField] float armorBonus           = 20f;
    // Buff-Dauer = specialCooldown, damit Buffs nicht überlappen.

    bool armorBuffActive = false;

    protected override void Awake()
    {
        GodId           = FavorManager.God.Athena;
        moveSpeed       = 5f;
        damage          = 40f;
        attackCooldown  = 0.9f;
        specialCooldown = 6f;
        base.Awake();
    }

    protected override void DoSpecialAttack()
    {
        // 1) Pyros heilen + Feinde im Pyros-Umkreis ausbrennen ───────────
        var pyrosGO = GameObject.FindGameObjectWithTag("Pyros");
        if (pyrosGO != null)
        {
            pyrosGO.GetComponent<Pyros>()?.Heal(pyrosHealAmount);

            Collider[] near = Physics.OverlapSphere(pyrosGO.transform.position,
                barrierRadius, LayerMask.GetMask("Enemy"));
            foreach (var hit in near)
                hit.GetComponent<EnemyBase>()?.TakeDamage(barrierDamage);
        }

        // 2) Wisdom-Burst am aktuellen Ziel ───────────────────────────────
        if (currentTarget != null)
        {
            Collider[] hits = Physics.OverlapSphere(currentTarget.transform.position,
                burstRadius, LayerMask.GetMask("Enemy"));
            foreach (var hit in hits)
                hit.GetComponent<EnemyBase>()?.TakeDamage(burstDamage);
        }

        // 3) Spieler-Rüstung temporär anheben ─────────────────────────────
        if (!armorBuffActive)
            StartCoroutine(ArmorBuff(specialCooldown));
    }

    IEnumerator ArmorBuff(float duration)
    {
        armorBuffActive = true;
        var ps = PlayerState.Instance;
        ps.armor += armorBonus;
        yield return new WaitForSeconds(duration);
        ps.armor -= armorBonus;
        armorBuffActive = false;
    }
}
