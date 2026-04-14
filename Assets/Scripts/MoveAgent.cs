using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MoveAgent : Agent
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    public Transform player;
    public TankWeapon weapon;
    private float lastDistance;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        weapon.SetOwner(GetComponent<Agent>());
        // Debug.Log("Agent initialized");
    }

    public override void OnEpisodeBegin()
    {
        // Debug.Log("Episode initialized");
        // Reset position each episode
        lastDistance = Vector3.Distance(transform.position, player.position);
        transform.localPosition = Vector3.zero;
        rb.linearVelocity = Vector2.zero;

        transform.localPosition = new Vector3(
            Random.Range(-1f, 1f),
            0.5f,
            Random.Range(-1f, 1f)
        );
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Debug.Log("Observations Collected");
        // Observe the agent’s position
        sensor.AddObservation(transform.position);

        sensor.AddObservation(rb.linearVelocity); //helps stabilize jitter movement
        
        Vector3 toEnemy = (player.position - transform.position).normalized;
        sensor.AddObservation(toEnemy);

        sensor.AddObservation(weapon.turretPivot.up);
        sensor.AddObservation(weapon.CanShoot ? 1f : 0f); // observer shooting cooldown, The 1 and 0 are just signals to the neural network, not time values.

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];
        
        int rotate = actions.DiscreteActions[1];
        int shoot = actions.DiscreteActions[0];

        switch (rotate)
        {
            case 0: weapon.SetDirection(TankWeapon.Direction.Up); break;
            case 1: weapon.SetDirection(TankWeapon.Direction.Right); break;
            case 2: weapon.SetDirection(TankWeapon.Direction.Down); break;
            case 3: weapon.SetDirection(TankWeapon.Direction.Left); break;
        }        

        rb.linearVelocity = new Vector2(moveX, moveY) * moveSpeed;

        if (shoot == 1)
        {
            bool fired = weapon.TryShoot();

            if (fired)
            {
                // Debug.Log("AI is shooting");
                AddReward(0.10f); // small reward for valid execution
            }
            else
            {
                // Debug.Log("AI is being discouraged from shooting");
                AddReward(-0.001f); // optional: discourage spam
            }
        }

        float distance = Vector3.Distance(transform.position, player.position);

        // Dense reward: reward reduction in distance since last step
        float progress = lastDistance - distance;
        AddReward(progress * 2f);
        lastDistance = distance;

        // Optional small penalty for existing (stops idling)
        AddReward(-0.001f);

        // Success condition
        if (distance < 0.5f)
        {
            AddReward(10f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.Log(" Heuristics initialized");
        var c = actionsOut.ContinuousActions;
        var d = actionsOut.DiscreteActions;

        c[0] = Input.GetAxisRaw("Horizontal");
        c[1] = Input.GetAxisRaw("Vertical");

        d[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            AddReward(10f);
            Debug.Log("collided with player");
            EndEpisode();
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.1f);
        }
    }   
}