using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MoveAgent : Agent
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotateSpeed = 180f;
    public Transform goal;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;   // turret / barrel tip direction source
    public float shootCooldown = 0.5f;
    public float hitPlayerReward = 2.0f;
    public float missPenalty = -0.01f;

    [Header("Vision")]
    public float rayDistance = 10f;
    public int rayCount = 9;
    public float visionAngle = 90f;
    public LayerMask rayMask;

    [Header("Rewards")]
    public float stepPenalty = -0.001f;
    public float wallPenalty = -0.05f;
    public float seePlayerReward = 0.05f;
    public float facePlayerReward = 0.01f;
    public float loseSightPenalty = -0.01f;

    private Rigidbody2D rb;
    private float lastShootTime;

    private bool canSeePlayer;
    private Vector2 lastSeenPlayerDirection;
    private bool sawPlayerLastStep;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        transform.position = new Vector2(
            Random.Range(-4f, 4f),
            Random.Range(-4f, 4f)
        );

        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        if (goal != null)
        {
            goal.position = new Vector3(
                Random.Range(-4f, 4f),
                Random.Range(-4f, 4f),
                0f
            );
        }

        lastShootTime = -shootCooldown;
        canSeePlayer = false;
        lastSeenPlayerDirection = Vector2.zero;
        sawPlayerLastStep = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector2 velocity = rb.linearVelocity;
        Vector2 forward = firePoint != null ? (Vector2)firePoint.up : (Vector2)transform.up;

        sensor.AddObservation(transform.position.x / 5f);
        sensor.AddObservation(transform.position.y / 5f);
        sensor.AddObservation(velocity.x / moveSpeed);
        sensor.AddObservation(velocity.y / moveSpeed);
        sensor.AddObservation(forward);

        CastRays(sensor);

        sensor.AddObservation(canSeePlayer ? 1f : 0f);
        sensor.AddObservation(lastSeenPlayerDirection);

        float facingDot = 0f;
        if (canSeePlayer)
        {
            facingDot = Vector2.Dot(forward.normalized, lastSeenPlayerDirection.normalized);
        }
        sensor.AddObservation(facingDot);
    }

    private void CastRays(VectorSensor sensor)
    {
        if (rayCount <= 0)
            return;

        canSeePlayer = false;
        lastSeenPlayerDirection = Vector2.zero;

        Vector2 forward = firePoint != null ? (Vector2)firePoint.up : (Vector2)transform.up;
        float baseAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

        float startAngle = baseAngle - (visionAngle * 0.5f);
        float angleStep = rayCount > 1 ? visionAngle / (rayCount - 1) : 0f;

        float closestPlayerDistance = float.MaxValue;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = startAngle + (i * angleStep);
            Vector2 direction = DegreeToVector2(angle);

            Vector2 origin = rb.position;
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, rayDistance, rayMask);

            if (hit.collider != null)
            {
                float normalizedDistance = 1f - (hit.distance / rayDistance);
                GameObject hitObject = hit.collider.gameObject;

                bool hitPlayer = hitObject.CompareTag("Player");
                bool hitWall = hitObject.CompareTag("Wall") ||
                               hitObject.layer == LayerMask.NameToLayer("Wall");

                if (hitPlayer)
                {
                    if (hit.distance < closestPlayerDistance)
                    {
                        closestPlayerDistance = hit.distance;
                        canSeePlayer = true;
                        lastSeenPlayerDirection = direction.normalized;
                    }

                    sensor.AddObservation(0f); // wall
                    sensor.AddObservation(1f); // player
                    sensor.AddObservation(0f); // other
                    sensor.AddObservation(0f); // nothing
                }
                else if (hitWall)
                {
                    sensor.AddObservation(1f); // wall
                    sensor.AddObservation(0f); // player
                    sensor.AddObservation(0f); // other
                    sensor.AddObservation(0f); // nothing
                }
                else
                {
                    sensor.AddObservation(0f); // wall
                    sensor.AddObservation(0f); // player
                    sensor.AddObservation(1f); // other
                    sensor.AddObservation(0f); // nothing
                }

                sensor.AddObservation(normalizedDistance);
            }
            else
            {
                sensor.AddObservation(0f); // wall
                sensor.AddObservation(0f); // player
                sensor.AddObservation(0f); // other
                sensor.AddObservation(1f); // nothing
                sensor.AddObservation(0f); // distance
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveAction = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float rotateAction = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        Vector2 forward = firePoint != null ? (Vector2)firePoint.up : (Vector2)transform.up;

        rb.linearVelocity = forward * moveAction * moveSpeed;
        rb.angularVelocity = -rotateAction * rotateSpeed;

        AddReward(stepPenalty);

        if (canSeePlayer && !sawPlayerLastStep)
        {
            AddReward(seePlayerReward);
        }

        if (!canSeePlayer && sawPlayerLastStep)
        {
            AddReward(loseSightPenalty);
        }

        if (canSeePlayer)
        {
            float facingDot = Vector2.Dot(forward.normalized, lastSeenPlayerDirection.normalized);

            if (facingDot > 0.95f)
            {
                AddReward(facePlayerReward);
            }
            else if (facingDot < 0.5f)
            {
                AddReward(-0.002f);
            }
        }

        sawPlayerLastStep = canSeePlayer;

        int shootAction = actions.DiscreteActions[0];
        if (shootAction == 1)
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        if (Time.time < lastShootTime + shootCooldown)
            return;

        if (bulletPrefab == null || firePoint == null)
            return;

        if (!canSeePlayer)
            return;

        Vector2 forward = firePoint.up.normalized;
        float facingDot = Vector2.Dot(forward, lastSeenPlayerDirection.normalized);

        // Must be facing the player closely before shooting
        if (facingDot < 0.95f)
            return;

        lastShootTime = Time.time;

        GameObject bulletObject = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        AgentBullet bullet = bulletObject.GetComponent<AgentBullet>();

        if (bullet != null)
        {
            bullet.SetDirection(forward);
            bullet.SetOwner(this);
        }

        AddReward(missPenalty);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(wallPenalty);
        }
    }

    public void RewardShotHitPlayer()
    {
        AddReward(hitPlayerReward);
        EndEpisode();
    }

    private Vector2 DegreeToVector2(float degree)
    {
        float radians = degree * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
    }

    private void OnDrawGizmosSelected()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body == null || rayCount <= 0)
            return;

        Vector2 forward = firePoint != null ? (Vector2)firePoint.up : (Vector2)transform.up;
        float baseAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

        float startAngle = baseAngle - (visionAngle * 0.5f);
        float angleStep = rayCount > 1 ? visionAngle / (rayCount - 1) : 0f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = startAngle + (i * angleStep);
            Vector2 direction = DegreeToVector2(angle);
            Vector2 origin = body.position;

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, rayDistance, rayMask);

            if (hit.collider != null)
            {
                GameObject hitObject = hit.collider.gameObject;
                bool hitPlayer = hitObject.CompareTag("Player");
                bool hitWall = hitObject.CompareTag("Wall") ||
                               hitObject.layer == LayerMask.NameToLayer("Wall");

                if (hitPlayer)
                    Gizmos.color = Color.green;
                else if (hitWall)
                    Gizmos.color = Color.red;
                else
                    Gizmos.color = Color.yellow;

                Gizmos.DrawLine(origin, hit.point);
            }
            else
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(origin, origin + direction * rayDistance);
            }
        }

        if (firePoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(firePoint.position, firePoint.position + firePoint.up * 2f);
        }
    }

    public void MoveToRandomPosition()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        transform.position = new Vector2(
            Random.Range(-4f, 4f),
            Random.Range(-4f, 4f)
        );

        transform.rotation = Quaternion.Euler(
            0f,
            0f,
            Random.Range(0f, 360f)
        );
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        var discreteActions = actionsOut.DiscreteActions;

        continuousActions[0] = Input.GetAxis("Vertical");
        continuousActions[1] = Input.GetAxis("Horizontal");
        discreteActions[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }
}