// GodCardUI.cs
// Ablegen in: Assets/Scripts/UI/MainMenu/GodCardUI.cs
// Auf dem GodCard Prefab
//
// GodCard Hierarchy:
//   GodCard (Button)
//   ├── CardBG (Image — Stein-Optik)
//   ├── SelectionGlow (Image — leuchtet bei Auswahl)
//   ├── GodIcon (Image — 80×80)
//   ├── GodNameText (TextMeshProUGUI — groß, oben)
//   ├── SubtitleText (TextMeshProUGUI — klein, kursiv)
//   └── ActiveIndicator (Image — kleiner Punkt unten)

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class GodCardUI : MonoBehaviour
{
    [SerializeField] Image           cardBG;
    [SerializeField] Image           selectionGlow;
    [SerializeField] Image           godIconImage;
    [SerializeField] TextMeshProUGUI godNameText;
    [SerializeField] TextMeshProUGUI subtitleText;
    [SerializeField] Button          button;

    public FavorManager.God GodId { get; private set; }

    GodSelectData   data;
    bool            isSelected = false;
    Coroutine       glowCoroutine;

    public void Setup(GodSelectData godData, Action<GodSelectData> onSelected)
    {
        data  = godData;
        GodId = godData.God;

        if (godNameText  != null) godNameText.text  = godData.Name;
        if (subtitleText != null) subtitleText.text  = godData.Subtitle;

        // Akzentfarbe als subtilen Border/Tint
        if (cardBG != null)
        {
            Color bg = godData.AccentColor;
            bg.a = 0.08f;
            cardBG.color = bg;
        }

        if (selectionGlow != null)
        {
            selectionGlow.color  = new Color(godData.AccentColor.r, godData.AccentColor.g, godData.AccentColor.b, 0f);
        }

        button?.onClick.AddListener(() => onSelected(godData));

        // Hover-Effekt
        AddHoverEvents();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (glowCoroutine != null) StopCoroutine(glowCoroutine);

        if (selected)
        {
            glowCoroutine = StartCoroutine(GlowPulse());
            transform.localScale = Vector3.one * 1.06f;
        }
        else
        {
            if (selectionGlow != null)
            {
                Color c = selectionGlow.color; c.a = 0f;
                selectionGlow.color = c;
            }
            transform.localScale = Vector3.one;
        }
    }

    IEnumerator GlowPulse()
    {
        if (selectionGlow == null) yield break;
        while (isSelected)
        {
            float t = 0f;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                float a = 0.3f + Mathf.Sin(t * Mathf.PI / 1.2f) * 0.25f;
                Color c = selectionGlow.color; c.a = a;
                selectionGlow.color = c;
                yield return null;
            }
        }
    }

    void AddHoverEvents()
    {
        var trigger = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        var enter = new UnityEngine.EventSystems.EventTrigger.Entry
        { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            if (!isSelected) StartCoroutine(ScaleTo(1.04f, 0.1f));
        });

        var exit = new UnityEngine.EventSystems.EventTrigger.Entry
        { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
        exit.callback.AddListener(_ =>
        {
            if (!isSelected) StartCoroutine(ScaleTo(1f, 0.1f));
        });

        trigger.triggers.Add(enter);
        trigger.triggers.Add(exit);
    }

    IEnumerator ScaleTo(float targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale   = Vector3.one * targetScale;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, t / duration);
            yield return null;
        }
        transform.localScale = endScale;
    }
}
