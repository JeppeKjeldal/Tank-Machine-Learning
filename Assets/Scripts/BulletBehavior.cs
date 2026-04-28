using UnityEngine;

public class BulletBehavior : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            PointsManager.Instance.AddPlayerPoints();
            FindAnyObjectByType<MoveAgent>().MoveToRandomPosition();
        }

        Destroy(gameObject);
    }
}
