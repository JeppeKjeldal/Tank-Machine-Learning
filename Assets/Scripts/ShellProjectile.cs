using UnityEngine;
using Unity.MLAgents;

public class ShellProjectile : MonoBehaviour
{
    public float speed = 8f;
    public float lifeTime = 5f;

    private Rigidbody2D rb;

    private Agent ownerAgent;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        rb.linearVelocity = -transform.up * speed;

        Destroy(gameObject, lifeTime);
    }

    public void Init(Agent agent)
    {
        ownerAgent = agent;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            ownerAgent.AddReward(5f);
            Debug.Log("Agent rewarded for hitting player");
            ownerAgent.EndEpisode();
        }

        Destroy(gameObject);
    }
}