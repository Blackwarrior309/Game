// UpgradeData.cs
// ScriptableObject für Levelup-Angebote
// Ablegen in: Assets/Scripts/Core/UpgradeData.cs
// Erstellen: Assets > Create > OlympusSurvivors > UpgradeData

using UnityEngine;

[CreateAssetMenu(menuName = "OlympusSurvivors/UpgradeData")]
public class UpgradeData : ScriptableObject
{
    public string upgradeId;
    public string displayName;
    public string description;
    [TextArea] public string iconEmoji;  // Platzhalter bis Sprites vorhanden
    public UpgradeCategory category;

    public enum UpgradeCategory { Weapon, Passive, Building }
}
