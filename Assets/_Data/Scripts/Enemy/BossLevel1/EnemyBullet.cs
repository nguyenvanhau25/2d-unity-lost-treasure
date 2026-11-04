using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    private Vector3 movementDirection;

    [SerializeField, Min(0f)] private float timeLife = 5f;

    [SerializeField, Min(0)] private int dmg = 2;
    void Start()
    {
        Destroy(gameObject, timeLife);
    }
    void Update()
    {
        if (movementDirection == Vector3.zero)
        {
            return;
        }
        transform.position += movementDirection * Time.deltaTime;
    }

    public void SetMovementDirection(Vector3 direction)
    {
        movementDirection = direction;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerDamReceive player = collision.GetComponentInParent<PlayerDamReceive>();

        if (player != null)
        {
            player.Deduct(dmg);

            Destroy(gameObject);
        }
    }
}
