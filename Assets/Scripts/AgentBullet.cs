using UnityEngine;

public class AgentBullet : MonoBehaviour
{
    public float speed = 12f;
    public float lifeTime = 3f;

    private Vector2 direction;
    private MoveAgent owner;

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
        Destroy(gameObject, lifeTime);
    }

    public void SetOwner(MoveAgent agent)
    {
        owner = agent;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.gameObject == owner.gameObject)
            return;

        if (other.CompareTag("Player"))
        {
            owner?.RewardShotHitPlayer();
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}