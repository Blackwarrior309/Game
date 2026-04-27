// AttackRangeDetector.cs
// Anhängen an ein Child-GameObject des Spielers mit SphereCollider (isTrigger)
// Ablegen in: Assets/Scripts/Player/AttackRangeDetector.cs

using UnityEngine;

public class AttackRangeDetector : MonoBehaviour
{
    PlayerController playerController;

    void Awake()
    {
        // PlayerController sitzt am Parent
        playerController = GetComponentInParent<PlayerController>();
    }

    void OnTriggerEnter(Collider other)
    {
        var enemy = other.GetComponent<EnemyBase>();
        if (enemy != null && !playerController.NearbyEnemies.Contains(enemy))
            playerController.NearbyEnemies.Add(enemy);
    }

    void OnTriggerExit(Collider other)
    {
        var enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
            playerController.NearbyEnemies.Remove(enemy);
    }
}
