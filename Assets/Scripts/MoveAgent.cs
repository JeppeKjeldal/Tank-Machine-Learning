using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MoveAgent : Agent
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public Transform goal;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shootCooldown = 0.5f;
    public float shootReward = 0.2f;
    public float hitPlayerReward = 1.0f;
    public float missPenalty = -0.01f;

    [Header("Ray Settings")]
    public float rayDistance = 10f;
    public int rayCount = 32;
    public LayerMask rayMask;

    [Header("Rewards")]
    public float stepPenalty = -0.001f;
    public float wallPenalty = -0.2f;
    public float reachGoalReward = 1.0f;
    public float closerRewardMultiplier = 0.05f;

    private Rigidbody2D rb;
    private float previousDistanceToGoal;
    private float lastShootTime;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        rb.position = new Vector2(
            Random.Range(-4f, 4f),
            Random.Range(-4f, 4f)
        );

        goal.localPosition = new Vector3(
            Random.Range(-4f, 4f),
            Random.Range(-4f, 4f),
            0f
        );

        previousDistanceToGoal = Vector2.Distance(rb.position, goal.localPosition);
        lastShootTime = -shootCooldown;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector2 agentPos = rb.position;
        Vector2 goalPos = goal.localPosition;
        Vector2 toGoal = goalPos - agentPos;
        Vector2 velocity = rb.linearVelocity;

        sensor.AddObservation(agentPos / 4f);
        sensor.AddObservation(goalPos / 4f);
        sensor.AddObservation(toGoal.normalized);
        sensor.AddObservation(toGoal.magnitude / 8f);
        sensor.AddObservation(velocity / moveSpeed);

        CastRays(sensor);
    }

    private void CastRays(VectorSensor sensor)
    {
        if (rayCount <= 0)
            return;

        float angleStep = 360f / rayCount;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * angleStep;
            Vector2 direction = DegreeToVector2(angle);
            Vector2 origin = rb.position + direction * 0.1f;

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, rayDistance, rayMask);

            if (hit.collider != null)
            {
                float normalizedDistance = 1f - (hit.distance / rayDistance);
                GameObject hitObject = hit.collider.gameObject;

                bool hitPlayer = hitObject.CompareTag("Player");
                bool hitWall = hitObject.CompareTag("Wall") || hitObject.layer == LayerMask.NameToLayer("Wall");

                if (hitWall)
                {
                    sensor.AddObservation(1f);
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                }
                else if (hitPlayer)
                {
                    sensor.AddObservation(0f);
                    sensor.AddObservation(1f);
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                }
                else
                {
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                    sensor.AddObservation(1f);
                    sensor.AddObservation(0f);
                }

                sensor.AddObservation(normalizedDistance);
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(1f);
                sensor.AddObservation(0f);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float moveY = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        Vector2 moveDirection = new Vector2(moveX, moveY).normalized;
        rb.linearVelocity = moveDirection * moveSpeed;

        AddReward(stepPenalty);

        float currentDistanceToGoal = Vector2.Distance(rb.position, goal.localPosition);
        float distanceDelta = previousDistanceToGoal - currentDistanceToGoal;
        AddReward(distanceDelta * closerRewardMultiplier);

        if (currentDistanceToGoal < 3f)
            AddReward(0.002f);

        if (currentDistanceToGoal < 1.5f)
            AddReward(0.005f);

        if (currentDistanceToGoal < 0.75f)
            AddReward(0.01f);

        previousDistanceToGoal = currentDistanceToGoal;

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

        if (bulletPrefab == null || firePoint == null || goal == null)
            return;

        lastShootTime = Time.time;

        Vector2 shootDirection = ((Vector2)goal.position - (Vector2)firePoint.position).normalized;

        GameObject bulletObject = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        AgentBullet bullet = bulletObject.GetComponent<AgentBullet>();

        if (bullet != null)
        {
            bullet.SetDirection(shootDirection);
            bullet.SetOwner(this);
        }

        AddReward(missPenalty);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            AddReward(reachGoalReward);
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(wallPenalty);
        }
    }

    public void RewardShotHitPlayer()
    {
        AddReward(hitPlayerReward);
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

        float angleStep = 360f / rayCount;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * angleStep;
            Vector2 direction = DegreeToVector2(angle);
            Vector2 origin = body.position + direction * 0.1f;

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, rayDistance, rayMask);

            if (hit.collider != null)
            {
                GameObject hitObject = hit.collider.gameObject;
                bool hitPlayer = hitObject.CompareTag("Player");
                bool hitWall = hitObject.CompareTag("Wall") || hitObject.layer == LayerMask.NameToLayer("Wall");

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

        if (firePoint != null && goal != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(firePoint.position, goal.position);
        }

        if (firePoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(firePoint.position, firePoint.position + firePoint.right * 2f);
        }
    }
}