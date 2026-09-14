using System.Collections;
using UnityEngine;

public class Entity : MonoBehaviour
{
    #region Components
    public Animator anim { get; private set; }
    public Rigidbody2D rb { get; private set; }
    public EntityFX fx { get; private set; }
    public SpriteRenderer sr { get; private set; }
    public CharacterStats stats { get; private set; }
    public CapsuleCollider2D cd { get; private set; }
    #endregion


    [Header("Knockback Info")]
    [SerializeField] protected Vector2 knockbackDirection;
    [SerializeField] protected float knockbackDuration;
    protected bool isKnocked;

    [Header("Collision Info")]
    public Transform attackCheak;
    public float attackCheakRidus;
    [SerializeField] protected Transform groundCheak;
    [SerializeField] protected float groundCheakDistance;
    [SerializeField] protected Transform wallCheak;
    [SerializeField] protected float wallCheakDistance;
    [SerializeField] protected LayerMask whatIsGround;


    public int facingDir { get; private set; } = 1;
    protected bool facingRight = true;

    public System.Action onFilpped;

    protected virtual void Awake()
    {

    }

    protected virtual void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        fx = GetComponentInChildren<EntityFX>();
        stats = GetComponent<CharacterStats>();
        cd = GetComponent<CapsuleCollider2D>();
    }

    protected virtual void Update()
    {

    }

    public virtual void SlowEntityBy(float _slowPercentage,float slowDuration)
    {

    }

    protected virtual void ReturnDefaultSpeed()
    {
        anim.speed = 1;
    }

    public virtual void DamageImpact() => StartCoroutine("HitKnockback");
    

    protected virtual IEnumerator HitKnockback()
    {
        isKnocked = true;
        rb.velocity = new Vector2(knockbackDirection.x * -facingDir, knockbackDirection.y);
        yield return new WaitForSeconds(knockbackDuration);
        isKnocked = false;
    }

    //速度
    #region Velocity
    public void SetZeroVelocity()
    {
        if (isKnocked)
            return;
        rb.velocity = new Vector2(0, 0);  //速度为0函数
    }


    public void SetVelocity(float _xVelocity, float _yVelocity)
    {

        if (isKnocked)
            return;

        rb.velocity = new Vector2(_xVelocity, _yVelocity);
        FlipController(_xVelocity);
    }
    #endregion

    //碰撞检测器
    #region Collision
    public virtual bool IsGroundDetected() => Physics2D.Raycast(groundCheak.position, Vector2.down, groundCheakDistance, whatIsGround);
    public virtual bool IsWallDetected() => Physics2D.Raycast(wallCheak.position, Vector2.right * facingDir, wallCheakDistance, whatIsGround);

    public bool TryGetGroundPointBelow(Vector2 origin, float distance, out Vector2 point)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, distance, whatIsGround);

        point = hit.collider != null ? hit.point : origin + Vector2.down * distance;
        return hit.collider != null;
    }

    public virtual void OnDrawGizmos()
    {
        if (Application.isPlaying)
            return;

        Gizmos.color = new Color(1f, 0.65f, 0.1f, 0.65f);

        if (groundCheak != null)
            Gizmos.DrawLine(groundCheak.position, new Vector3(groundCheak.position.x, groundCheak.position.y - groundCheakDistance));

        if (wallCheak != null)
            Gizmos.DrawLine(wallCheak.position, new Vector3(wallCheak.position.x + wallCheakDistance, wallCheak.position.y));

        if (attackCheak != null)
            Gizmos.DrawWireSphere(attackCheak.position, attackCheakRidus);
    }
    #endregion

    //反转
    #region Flip
    public virtual void Flip()
    {
        facingDir = facingDir * -1;
        facingRight = !facingRight;
        transform.Rotate(0, 180, 0);

        if (onFilpped != null)
            onFilpped();
    }


    public virtual void FlipController(float _x)
    {
        if (_x > 0 && !facingRight)
            Flip();
        else if (_x < 0 && facingRight)
            Flip();
    }
    #endregion

    public void MakeTransprent(bool _transprent)
    {
        if(_transprent)
            sr.color = Color.clear;
        else
            sr.color = Color.white;
    }


    public virtual void Die()
    {

    }

}
