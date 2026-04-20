using UnityEngine;
using Unity.MLAgents;

public class TankWeapon : MonoBehaviour
{
    public Transform turretPivot;
    public GameObject shellPrefab;
    public Transform firePoint;
    private Agent ownerAgent;

    public float fireCooldown = 1f;
    private float nextFireTime = 0f;
    public bool CanShoot => Time.time >= nextFireTime;

    public enum Direction
    {
        Up,
        Right,
        Down,
        Left
    }

    public void SetDirection(Direction dir)
    {
        float angle = 0f;

        switch (dir)
        {
            case Direction.Up: angle = 0f; break;
            case Direction.Right: angle = -90f; break;
            case Direction.Down: angle = 180f; break;
            case Direction.Left: angle = 90f; break;
        }

        turretPivot.localRotation = Quaternion.Euler(0, 0, angle);
    }

    public void SetOwner(Agent agent)
    {
        ownerAgent = agent;
    }

    public bool TryShoot()
    {
        if (Time.time < nextFireTime)
            return false;

        // Debug.Log("Tank fired!");

        nextFireTime = Time.time + fireCooldown;

        GameObject shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);

        shell.GetComponent<ShellProjectile>().Init(ownerAgent);

        return true;
    }
}