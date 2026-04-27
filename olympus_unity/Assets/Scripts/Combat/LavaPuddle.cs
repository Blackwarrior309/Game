// LavaPuddle.cs
// Ablegen in: Assets/Scripts/Combat/LavaPuddle.cs
// Boden-DoT, der nach Vulkan-Zorn-Aufprall liegen bleibt.
// Anhängen an: GameObject mit ParticleSystem (Lava-VFX) — kein Collider nötig,
// Schaden läuft über OverlapSphere.

using UnityEngine;
using System.Collections;

public class LavaPuddle : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] float radius     = 2.5f;
    [SerializeField] float dpsDamage  = 8f;     // Schaden pro Sekunde
    [SerializeField] float duration   = 5f;
    [SerializeField] float tickRate   = 0.25f;  // alle 0.25 s ein Tick

    void Start() => StartCoroutine(BurnLoop());

    IEnumerator BurnLoop()
    {
        float elapsed = 0f;
        var enemyMask = LayerMask.GetMask("Enemy");

        while (elapsed < duration)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyMask);
            foreach (var hit in hits)
                hit.GetComponent<EnemyBase>()?.TakeDamage(dpsDamage * tickRate);

            yield return new WaitForSeconds(tickRate);
            elapsed += tickRate;
        }

        Destroy(gameObject);
    }
}
