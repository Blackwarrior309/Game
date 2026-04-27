// BuildingCardUI.cs
// Ablegen in: Assets/Scripts/UI/BuildMenu/BuildingCardUI.cs
//
// Hierarchy:
//   BuildingCard (Button)
//   ├── IconText (TextMeshProUGUI)
//   ├── NameText (TextMeshProUGUI)
//   ├── DescText (TextMeshProUGUI)
//   ├── AshCostText (TextMeshProUGUI)
//   ├── OreCostRow (GameObject — nur aktiv wenn OreCost > 0)
//   │   └── OreCostText (TextMeshProUGUI)
//   └── LockOverlay (Image — aktiv wenn nicht baubar)

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class BuildingCardUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI iconText;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI descText;
    [SerializeField] TextMeshProUGUI ashCostText;
    [SerializeField] TextMeshProUGUI oreCostText;
    [SerializeField] GameObject      oreCostRow;
    [SerializeField] GameObject      lockOverlay;
    [SerializeField] Image           cardBG;
    [SerializeField] Button          button;

    public void Setup(BuildMenuController.BuildingData bld,
                      Action<BuildMenuController.BuildingData> onClicked)
    {
        if (iconText    != null) iconText.text    = bld.IconEmoji;
        if (nameText    != null) nameText.text    = bld.DisplayName.ToUpper();
        if (descText    != null) descText.text    = bld.Description;
        if (ashCostText != null) ashCostText.text = $"{bld.AshCost} 🌫";

        bool hasOre = bld.OreCost > 0;
        if (oreCostRow  != null) oreCostRow.SetActive(hasOre);
        if (hasOre && oreCostText != null) oreCostText.text = $"{bld.OreCost} 🪨";

        bool canBuild = bld.CanBuild()
            && PlayerState.Instance.ash >= bld.AshCost
            && PlayerState.Instance.ore >= bld.OreCost;

        if (lockOverlay != null) lockOverlay.SetActive(!canBuild);
        if (button      != null) button.interactable = canBuild;

        // Tempel-Karten goldene Tönung
        if (cardBG != null && bld.Category == BuildMenuController.BuildCategory.Temple)
            cardBG.color = new Color(0.18f, 0.14f, 0.06f);
        // Schmiede leicht orange
        if (cardBG != null && bld.Id == "forge")
            cardBG.color = new Color(0.18f, 0.10f, 0.04f);

        button?.onClick.AddListener(() => onClicked(bld));
    }
}
