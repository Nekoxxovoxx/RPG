using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Environment
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector2 localPointA;
        [SerializeField] private Vector2 localPointB = new Vector2(4f, 0);
        [SerializeField] private float speed = 2.5f;
        [SerializeField] private float waitTime = 0.2f;
        [SerializeField, Min(0f)] private float riderTopTolerance = 0.25f;

        private Rigidbody2D rb;
        private Collider2D platformCollider;
        private Vector2 pointA;
        private Vector2 pointB;
        private Vector2 target;
        private float waitTimer;
        private readonly List<Rigidbody2D> riders = new List<Rigidbody2D>();

        public void Configure(Vector2 pointAOffset, Vector2 pointBOffset, float moveSpeed, float pauseTime)
        {
            localPointA = pointAOffset;
            localPointB = pointBOffset;
            speed = moveSpeed;
            waitTime = pauseTime;
            RefreshPoints();
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            platformCollider = GetComponent<Collider2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void Start()
        {
            RefreshPoints();
        }

        private void FixedUpdate()
        {
            if (waitTimer > 0)
            {
                waitTimer -= Time.fixedDeltaTime;
                return;
            }

            Vector2 previousPosition = rb.position;
            Vector2 next = Vector2.MoveTowards(previousPosition, target, speed * Time.fixedDeltaTime);
            rb.MovePosition(next);
            MoveRiders(next - previousPosition);

            if (Vector2.Distance(next, target) > 0.02f)
                return;

            target = target == pointA ? pointB : pointA;
            waitTimer = waitTime;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryAddRider(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (IsStandingOnTop(collision))
                TryAddRider(collision);
            else
                TryRemoveRider(collision);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            TryRemoveRider(collision);
        }

        private void TryAddRider(Collision2D collision)
        {
            if (!IsStandingOnTop(collision))
                return;

            Player player = collision.collider.GetComponentInParent<Player>();

            if (player == null)
                return;

            Rigidbody2D playerRb = player.rb != null ? player.rb : player.GetComponent<Rigidbody2D>();

            if (playerRb == null || riders.Contains(playerRb))
                return;

            riders.Add(playerRb);
        }

        private void TryRemoveRider(Collision2D collision)
        {
            Player player = collision.collider.GetComponentInParent<Player>();

            if (player == null)
                return;

            Rigidbody2D playerRb = player.rb != null ? player.rb : player.GetComponent<Rigidbody2D>();

            if (playerRb != null)
                riders.Remove(playerRb);
        }

        private bool IsStandingOnTop(Collision2D collision)
        {
            if (platformCollider == null)
                platformCollider = GetComponent<Collider2D>();

            Player player = collision.collider.GetComponentInParent<Player>();

            if (player == null || platformCollider == null)
                return false;

            Bounds playerBounds = collision.collider.bounds;
            Bounds platformBounds = platformCollider.bounds;

            return playerBounds.min.y >= platformBounds.max.y - riderTopTolerance;
        }

        private void MoveRiders(Vector2 platformDelta)
        {
            if (platformDelta == Vector2.zero)
                return;

            for (int i = riders.Count - 1; i >= 0; i--)
            {
                Rigidbody2D rider = riders[i];

                if (rider == null)
                {
                    riders.RemoveAt(i);
                    continue;
                }

                rider.position += platformDelta;
            }
        }

        private void RefreshPoints()
        {
            pointA = (Vector2)transform.position + localPointA;
            pointB = (Vector2)transform.position + localPointB;
            target = pointB;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 a = transform.position + (Vector3)localPointA;
            Vector3 b = transform.position + (Vector3)localPointB;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(a, b);
            Gizmos.DrawWireSphere(a, 0.18f);
            Gizmos.DrawWireSphere(b, 0.18f);
        }
    }
}
