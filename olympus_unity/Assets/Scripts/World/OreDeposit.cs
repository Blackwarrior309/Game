// OreDeposit.cs
// Ablegen in: Assets/Scripts/World/OreDeposit.cs
// Erz-Deposit in der Gefahrenzone (> 40m vom Pyros)
// Interaktion: Spieler hält E für 2 Sekunden
//
// Hierarchy:
//   OreDeposit (StaticBody3D)
//   ├── MeshInstance (Stein-Mesh, leuchtende Erz-Adern)
//   ├── SphereCollider (isTrigger, Radius 2.5 — Interaktions-Radius)
//   ├── InteractPrompt (Canvas — "E halten: Erz abbauen")
//   └── ParticleSystem "MineSparkFX"

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class OreDeposit : MonoBehaviour
{
    [Header("Deposit Settings")]
    [SerializeField] int   oreAmount        = 10;
    [SerializeField] float mineTime         = 2f;    // Sekunden Halten
    [SerializeField] float respawnTime      = 60f;
    [SerializeField] int   respawnAmount    = 8;     // Etwas weniger beim Respawn

    [Header("UI")]
    [SerializeField] GameObject      interactPromptGO;
    [SerializeField] TextMeshProUGUI promptText;
    [SerializeField] Image           progressBar;      // Radial oder linear fill

    [Header("FX")]
    [SerializeField] ParticleSystem  mineSparkFX;
    [SerializeField] Renderer        depositRenderer;
    [SerializeField] Color           activeColor   = new Color(0.8f, 0.6f, 0.1f);
    [SerializeField] Color           depletedColor = new Color(0.3f, 0.3f, 0.3f);

    // ── State ──────────────────────────────────────────────────────────────
    bool    playerInRange   = false;
    bool    isMining        = false;
    bool    isDepleted      = false;
    float   mineProgress    = 0f;
    int     currentOre;

    void Awake()
    {
        currentOre = oreAmount;
        if (interactPromptGO != null) interactPromptGO.SetActive(false);
        if (progressBar      != null) progressBar.fillAmount = 0f;
        UpdateVisual();
    }

    void Update()
    {
        if (isDepleted || !playerInRange) return;

        // E halten zum Abbauen
        if (Input.GetKey(KeyCode.E))
        {
            if (!isMining)
            {
                isMining = true;
                if (mineSparkFX != null) mineSparkFX.Play();
            }

            mineProgress += Time.deltaTime;
            float ratio   = mineProgress / mineTime;
            if (progressBar != null) progressBar.fillAmount = ratio;

            if (promptText  != null)
                promptText.text = $"⛏ Abbauen... {Mathf.CeilToInt(mineTime - mineProgress)}s";

            if (mineProgress >= mineTime)
                CompleteMining();
        }
        else
        {
            // Losgelassen → Fortschritt zurücksetzen
            if (isMining)
            {
                isMining     = false;
                mineProgress = 0f;
                if (progressBar != null) progressBar.fillAmount = 0f;
                if (promptText  != null) promptText.text = "[E halten] Erz abbauen";
                if (mineSparkFX != null) mineSparkFX.Stop();
            }
        }
    }

    // ── Abbau abgeschlossen ────────────────────────────────────────────────
    void CompleteMining()
    {
        isMining     = false;
        mineProgress = 0f;

        // Erz-Bonus aus Lavameer-Synergie
        int finalOre = currentOre;
        if (SynergySystem.Instance.IsActive("lava_sea"))
            finalOre = Mathf.RoundToInt(finalOre * 1.2f);

        PlayerState.Instance.AddOre(finalOre);

        // Deposit erschöpft
        isDepleted  = true;
        currentOre  = 0;

        if (interactPromptGO != null) interactPromptGO.SetActive(false);
        if (progressBar      != null) progressBar.fillAmount = 0f;
        if (mineSparkFX      != null) mineSparkFX.Stop();
        UpdateVisual();

        // Respawn nach 60s
        StartCoroutine(RespawnCoroutine());
    }

    IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnTime);
        isDepleted = false;
        currentOre = respawnAmount;
        UpdateVisual();

        // Wenn Spieler noch in Range ist, Prompt zeigen
        if (playerInRange && interactPromptGO != null)
            interactPromptGO.SetActive(true);
    }

    // ── Spieler-Nähe ──────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;

        if (!isDepleted && interactPromptGO != null)
        {
            interactPromptGO.SetActive(true);
            if (promptText != null) promptText.text = "[E halten] Erz abbauen";
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;

        if (interactPromptGO != null) interactPromptGO.SetActive(false);
        if (progressBar      != null) progressBar.fillAmount = 0f;

        if (isMining)
        {
            isMining     = false;
            mineProgress = 0f;
            if (mineSparkFX != null) mineSparkFX.Stop();
        }
    }

    // ── Visual-Update ─────────────────────────────────────────────────────
    void UpdateVisual()
    {
        if (depositRenderer == null) return;
        depositRenderer.material.color = isDepleted ? depletedColor : activeColor;

        // Leuchten-Effekt: Emission aus/an
        if (isDepleted)
            depositRenderer.material.DisableKeyword("_EMISSION");
        else
        {
            depositRenderer.material.EnableKeyword("_EMISSION");
            depositRenderer.material.SetColor("_EmissionColor", activeColor * 0.5f);
        }
    }
}
