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
    private bool wasSeeingPlayer;

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
        
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector2.zero;

        transform.position = new Vector3(
            Random.Range(-8f, 8f),
            Random.Range(-4f, 4f),
            Random.Range(0, 0)
        );

        player.transform.position = new Vector3(
            Random.Range(-8f, 8f),
            Random.Range(-4f, 4f),
            Random.Range(0,0)
        );

        lastDistance = Vector3.Distance(transform.position, player.position);
        wasSeeingPlayer = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector2 myPos = transform.position; 
        Vector2 enemyPos = player.position;
        Vector2 delta = enemyPos - myPos;

        sensor.AddObservation(myPos); //own position
        sensor.AddObservation(rb.linearVelocity); //helps stabilize jitter movement

        sensor.AddObservation(delta.normalized);        // direction
        sensor.AddObservation(delta.magnitude / 10f);   // distance

        sensor.AddObservation((Vector2)weapon.turretPivot.up);

        sensor.AddObservation(weapon.CanShoot ? 1f : 0f); //observer shooting cooldown, The 1 and 0 are just signals to the neural network, not time values.

        sensor.AddObservation(CanSeePlayer() ? 1f : 0f);   
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];
        
        int rotate = actions.DiscreteActions[1];
        int shoot = actions.DiscreteActions[0];

        bool seesPlayer = CanSeePlayer();

        if (seesPlayer && !wasSeeingPlayer)
        {
            // Debug.Log("AI SEES PLAYER.............");
            AddReward(0.05f); // only on first sight
        }

        wasSeeingPlayer = seesPlayer;

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

    // public override void Heuristic(in ActionBuffers actionsOut)
    // {
    //     Debug.Log(" Heuristics initialized");
    //     var c = actionsOut.ContinuousActions;
    //     var d = actionsOut.DiscreteActions;

    //     c[0] = Input.GetAxisRaw("Horizontal");
    //     c[1] = Input.GetAxisRaw("Vertical");

    //     d[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    // }

    private bool CanSeePlayer()
    {
        Vector2 myPos = transform.position;
        Vector2 enemyPos = player.position;

        Vector2 delta = enemyPos - myPos;
        Vector2 dir = delta.normalized;

        Vector2 origin = myPos + dir * 0.6f; // offset outside own collider

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, delta.magnitude);

        return hit.collider != null &&
            hit.collider.CompareTag("Player");
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
            Debug.Log("collided with WALLLLLLLLLLLLLLLLLLLLLLL" + collision.otherCollider.name);
            AddReward(-1f);
        }
    }   
}