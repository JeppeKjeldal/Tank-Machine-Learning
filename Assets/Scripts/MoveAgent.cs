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
    // private int lastRotate;
    private float lastAimAlignment;
    private float lastDistance;
    private bool wasSeeingPlayer;
    private bool wasSeeingWall;
    private bool fired;
    private RayPerceptionSensorComponent2D rayComp;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        weapon.SetOwner(GetComponent<Agent>());
        rayComp = GetComponent<RayPerceptionSensorComponent2D>();
        // Debug.Log("Agent initialized");
    }

    public override void OnEpisodeBegin()
    {
        // Debug.Log("Episode initialized");
        // Reset position each episode
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
        wasSeeingWall = false;

        fired = false;
        lastAimAlignment = 0f;
        // lastRotate = 0;
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

        sensor.AddObservation(SensorSeesPlayer() ? 1f : 0f);   

        sensor.AddObservation(SeesInnerWall() ? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];
        
        int shoot = actions.DiscreteActions[0]; // to shoot or not
        int rotate = actions.DiscreteActions[1]; // to shoot up, right, down, left. or no chance

        rb.linearVelocity = new Vector2(moveX, moveY) * moveSpeed;

        float distance = Vector3.Distance(transform.position, player.position);

        float progress = lastDistance - distance;
        lastDistance = distance;

        bool seesPlayer = SensorSeesPlayer();
        bool seesWallNow = SeesInnerWall();

        Vector2 toPlayer = (player.position - transform.position).normalized;
        Vector2 velDir = rb.linearVelocity.sqrMagnitude > 0.01f
            ? rb.linearVelocity.normalized
            : Vector2.zero;

        float toward = Vector2.Dot(velDir, toPlayer);
        
        Vector2 delta = player.position - transform.position; // for vertital and horizontal alignment

        if(distance > 2f && seesPlayer)
        {
            AddReward(0.01f);              // vision good
            AddReward(toward * 0.05f);     // move toward visible target
        }

        if(!seesPlayer)
        {
            AddReward(-0.1f); // losing sight bad

            if(rb.linearVelocity.magnitude < 0.2f)
            {
                AddReward(-0.1f); // standing still blind = bad
            }
        }

        if(seesPlayer && !wasSeeingPlayer) //rewards reaqquiring sight.
        {
            AddReward(0.2f);  
        }

        if(!seesPlayer && seesWallNow && progress <= 0f) //wall ahead + not seeing player + not getting closer = punishment
        {
            AddReward(-0.02f);
        }

        if (wasSeeingWall && !seesWallNow) //rewards "leaving" a wall, so its not stuck to it. should teach to navigate around.
        {
            AddReward(0.005f);
        }

        wasSeeingWall = seesWallNow;

        // ------------------------------------ Spacing logic here ----------
        if (seesPlayer)
        {
            if (distance > 6f)
            {
                AddReward(toward * 0.03f);   // move closer
            }
            else if (distance >= 4f && distance <= 6f)
            {
                AddReward(0.03f);            // ideal zone
            }
            else if (distance < 4f)
            {
                AddReward(-toward * 0.03f);  // moving away is good
            }
        }

        if (distance < 7f && (Mathf.Abs(delta.x) < 0.6f || Mathf.Abs(delta.y) < 0.6f)) //helps align horizontal or vertical
        {
            AddReward(0.02f);
        }
        
        AddReward(-0.001f); // Idle/bad farmo spot punish

        //----------------------------------------- Weapon logic below ---------------------- 

        if (rotate != 0)
        {
            weapon.SetDirection((TankWeapon.Direction)(rotate - 1));
        }

        Vector2 turretDir = weapon.turretPivot.up;
        float aimAlignment = Vector2.Dot(turretDir.normalized, toPlayer); // align aim to the player(correct turret facing direction)

        if (rotate == 0 && seesPlayer && aimAlignment > 0.9f) // held correct aim
        {
            AddReward(0.004f); 
        }
        else if (rotate != 0 && aimAlignment > 0.9f) // rotated away from perfect aim
        {
            AddReward(-0.003f); 
        }

        if (seesPlayer && aimAlignment > lastAimAlignment)
        {
            AddReward(0.003f);
        }

        lastAimAlignment = aimAlignment;
        // lastRotate = rotate;

        if (seesPlayer)
        {
            AddReward(Mathf.Max(0f, aimAlignment) * 0.01f); //scaling reward based on aim when it sees player, when not shooting.
            
            if (aimAlignment < 0f)
            {
                AddReward(-0.003f);               
            }
        }

        fired = false;

        if (shoot == 1 && !weapon.CanShoot)
        {
            AddReward(-0.002f);
        }
        
        if (shoot == 1)
        {
            fired = weapon.TryShoot();

            if (fired)
            {
                if (!seesPlayer) //punish missing shots
                {
                    AddReward(-0.03f);
                }
                else
                {
                    // AddReward(Mathf.Max(0f, aimAlignment) * 0.15f); 
                    // scaling reward based on aim when it sees player and takes a shot
                    if (aimAlignment > 0.95f)
                        AddReward(0.20f);
                    else if (aimAlignment > 0.8f)
                        AddReward(0.08f);
                    else
                        AddReward(-0.03f);
                }  
            }

            if (seesPlayer && aimAlignment < 0f) //penalty for missing
            {
                AddReward(-0.005f);
            }
        }
    }

    private bool SensorSeesPlayer()
    {
        RayPerceptionInput input = rayComp.GetRayPerceptionInput();

        RayPerceptionOutput output =
            RayPerceptionSensor.Perceive(input, false);

        foreach (var ray in output.RayOutputs)
        {
            if (ray.HasHit && ray.HitTagIndex == 0) //index 0 is player
            {
                // Debug.Log("you made it to here!" + ray.HitGameObject);
                return true; // saw Player
            }
        }

        return false;
    }

    private bool SeesInnerWall()
    {
        RayPerceptionInput input = rayComp.GetRayPerceptionInput();

        RayPerceptionOutput output =
            RayPerceptionSensor.Perceive(input, false);

        foreach (var ray in output.RayOutputs)
        {
            if (ray.HasHit && ray.HitTagIndex == 2) //index 2 is InnerWall
            {
                return true; // saw Player
            }
        }

        return false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            AddReward(-1f);
            Debug.Log("collided with player");
            EndEpisode();
        }

        if (collision.gameObject.CompareTag("OuterWall"))
        {
            Debug.Log("collided with WALLLLLLLLLLLLLLLLLLLLLLL");
            AddReward(-1.5f);  
        }

        if (collision.gameObject.CompareTag("InnerWall"))
        {
            Debug.Log("collided with innerwall");
            AddReward(-0.5f);
        }
    }   
}

        // switch (rotate) //add if can see player then rotate, reward for correct rotate
        // {
        //     case 0: weapon.SetDirection(TankWeapon.Direction.Up); break;
        //     case 1: weapon.SetDirection(TankWeapon.Direction.Right); break;
        //     case 2: weapon.SetDirection(TankWeapon.Direction.Down); break;
        //     case 3: weapon.SetDirection(TankWeapon.Direction.Left); break;
        // }        

        // if (shoot == 1)
        // {
        //     bool fired = weapon.TryShoot();

        //     if (fired) //maybe add canseeplayer here also to reward shots when it sees the player
        //     {
        //         // Debug.Log("AI is shooting");
        //         AddReward(0.10f); // small reward for valid execution
        //     }
        //     else
        //     {
        //         // Debug.Log("AI is being discouraged from shooting");
        //         AddReward(-0.001f); // optional: discourage spam
        //     }
        // }

        // if (shoot == 1) // agent decision to shoot 
        // {
        //     weapon.TryShoot();
        // }
        
        // if (shoot == 1 && !seesPlayer) // Decided to take the shot while not seeing player, punish
        // {
        //     AddReward(-0.05f);
        // }

        // if (seesPlayer && aimAlignment > 0.8f) // sees player aimed correctly, Agent should be rewarded for correct aiming even when not shooting.
        // {
        //     AddReward(0.02f);
        // }

        // if (shoot == 1 && seesPlayer && aimAlignment > 0.8f) // Took the shot while seeing the player and correct aim. good.
        // {
        //     AddReward(0.1f);
        // }
        // else if (shoot == 1 && seesPlayer) // visible but poor aim
        // {
        //     AddReward(-0.02f); 
        // }



        // private bool HasLineOfSight()
        // {
        //     Vector2 dir = (player.position - transform.position).normalized;
        //     float dist = Vector2.Distance(transform.position, player.position);

        //     RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, dist);

        //     if (hit.collider == null) return true;

        //     return hit.collider.CompareTag("Player");
        // }

        // private float AngleToPlayer()
        // {
        //     Vector2 forward = transform.up;
        //     Vector2 toPlayer = (player.position - transform.position).normalized;

        //     return Vector2.SignedAngle(forward, toPlayer);
        // }    


        //bool currentlySeeingPlayer = SensorSeesPlayer();
        
        // if (currentlySeeingPlayer)
        // {
        //     AddReward(0.0015f); // maintain tracking
        // }

        // if (currentlySeeingPlayer && !wasSeeingPlayer)
        // {
        //     AddReward(0.05f); // newly found player
        // }

        // wasSeeingPlayer = currentlySeeingPlayer;

        // if (!currentlySeeingPlayer && rb.linearVelocity.magnitude > 0.5f)
        // {
        //     AddReward(0.0005f);
        // }

        // if (!currentlySeeingPlayer && distance < 6f && rb.linearVelocity.magnitude > 0.3f) //rewards looking for player not in sight but also not far away.
        // {
        //     AddReward(0.002f);
        // }

        // if (!currentlySeeingPlayer && distance < 6f && rb.linearVelocity.magnitude < 0.1f) // punish not moving around while technically close to player.
        // {
        //     AddReward(-0.002f);
        // }

        // bool seesWall = SeesInnerWall();

        // if (!currentlySeeingPlayer && seesWall && rb.linearVelocity.magnitude > 0.3f)
        // {
        //     AddReward(0.0015f); // encourages moving when blocked
        // }

        // bool hasLOS = HasLineOfSight();

        // if (!hasLOS)
        // {
        //     Vector2 toPlayer = (player.position - transform.position).normalized;
        //     Vector2 vel = rb.linearVelocity;
        //     Vector2 velDir = vel.sqrMagnitude > 0.0001f ? vel.normalized : Vector2.zero;

        //     float moveTowardLOS = Vector2.Dot(velDir, toPlayer);

        //     AddReward(moveTowardLOS * 0.0015f);
        // }

        // float angle = Mathf.Abs(AngleToPlayer()) / 180f;

        // // when not seeing player, reduce angle = good
        // if (!SensorSeesPlayer())
        // {
        //     float signedAngle = AngleToPlayer();
        //     float side = Mathf.Sign(signedAngle);

        //     AddReward(side * rb.linearVelocity.magnitude * 0.0005f); //FIX HERE IF BAD
        //     AddReward(-angle * 0.0015f);
        // }

        // //This enforces detour behavior for LoS
        // if (!currentlySeeingPlayer && !hasLOS)
        // {
        //     AddReward(0.0015f);
        // }
       
        // // Optional small penalty for existing (stops idling)
        // AddReward(-0.001f);



        // float wallPenalty = 0f;

        // Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.5f, LayerMask.GetMask("InnerWall"));

        // if (hit != null)
        // {
        //     wallPenalty += 0.02f;
        // }

        // AddReward(-wallPenalty);    



        // bool seesPlayer = CanSeePlayer();

        // if (seesPlayer && !wasSeeingPlayer)
        // {
        //     // Debug.Log("AI SEES PLAYER.............");
        //     AddReward(1f); // only on first sight
        // }
        
        // wasSeeingPlayer = seesPlayer;

        // if (CanSeePlayer())
        // {
        //     Vector2 intendedMove = new Vector2(moveX, moveY).normalized;
        //     Vector2 toPlayer = (player.position - transform.position).normalized;

        //     AddReward(Vector2.Dot(intendedMove, toPlayer) * 0.15f);
        // }

        // if (!CanSeePlayer()) //encourage exploration
        // {
        //     AddReward(0.002f * rb.linearVelocity.magnitude);
        // }




         // if (CanSeePlayer())
        // {
        //     AddReward(progress * 0.1f); //reward moving closer
        // }

        // RaycastHit2D hit = Physics2D.Raycast(transform.position, rb.linearVelocity.normalized, 0.5f);

        // if (hit.collider != null && hit.collider.CompareTag("Wall"))
        // {
        //     // Debug.Log("AGENT IS CLOSE TO A WALL");
        //     AddReward(-0.10f);
        // }

        // if (rb.linearVelocity.magnitude < 0.1f && (Mathf.Abs(moveX) > 0.3f || Mathf.Abs(moveY) > 0.3f))
        // {
        //     AddReward(-0.02f);
        // }

        // if (!CanSeePlayer() && rb.linearVelocity.magnitude > 0.1f)
        // {
        //     AddReward(-0.001f); // discourages blindly pushing into walls while target is hidden
        // }



        // private bool CanSeePlayer()
            // {
            //     Vector2 myPos = transform.position;
            //     Vector2 enemyPos = player.position;

            //     Vector2 delta = enemyPos - myPos;
            //     Vector2 dir = delta.normalized;

            //     Vector2 origin = myPos + dir * 0.6f; // offset outside own collider

            //     // Debug.DrawRay(origin, dir * delta.magnitude, Color.darkCyan);

            //     RaycastHit2D hit = Physics2D.Raycast(origin, dir, delta.magnitude);

            //     if (hit.collider != null)
            //     {
            //         Debug.DrawLine(origin, hit.point, Color.darkBlue); // actual hit point
            //     }
            //     else
            //     {
            //         Debug.DrawRay(origin, dir * delta.magnitude, Color.red);
            //     }

            //     return hit.collider != null &&
            //         hit.collider.CompareTag("Player");
            // }