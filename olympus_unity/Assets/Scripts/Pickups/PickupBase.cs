// PickupBase.cs
// Ablegen in: Assets/Scripts/Pickups/PickupBase.cs
// Layer: "Pickup" | Tag: "Pickup"

using UnityEngine;

public class PickupBase : MonoBehaviour
{
    public enum PickupType { Ash, Ore, XP }

    [SerializeField] PickupType pickupType = PickupType.Ash;
    [SerializeField] int amount = 1;

    public void SetAmount(int value) => amount = value;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Collect();
    }

    void Collect()
    {
        switch (pickupType)
        {
            case PickupType.Ash:
                PlayerState.Instance.AddAsh(amount);
                break;
            case PickupType.Ore:
                int bonus = SynergySystem.Instance.IsActive("lava_sea")
                    ? Mathf.RoundToInt(amount * 1.2f) : amount;
                PlayerState.Instance.AddOre(bonus);
                break;
            case PickupType.XP:
                PlayerState.Instance.AddXP(amount);
                break;
        }
        Destroy(gameObject);
    }
}
