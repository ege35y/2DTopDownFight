using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Saldırı Ayarları")]
    [Tooltip("Her vuruş 1 hasar")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackRange = 1.3f;
    [SerializeField] private float attackForwardOffset = 1.0f;
    [Tooltip("Saldırı animasyonunun tam süresi (animasyon bitene kadar dönüşü kilitler)")]
    [SerializeField] private float attackDuration = 0.35f;
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Düşmanı İtme (Knockback) Ayarları")]
    [Tooltip("Düşmana vurulduğunda hafif geriye sekme gücü")]
    [SerializeField] private float enemyKnockbackForce = 4.5f;
    [SerializeField] private float enemyKnockbackDuration = 0.12f;

    private InputAction moveAction;
    private InputAction attackAction;
    private Vector2 rawMovementInput;
    private Vector2 normalizedInput;
    private Rigidbody2D rb;
    private Animator anim;

    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private bool isKnockedBack = false;
    private float originalScaleX;
    private float lockedFacingDirection = 1f;

    private static readonly int RunningParam = Animator.StringToHash("Running");
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        originalScaleX = Mathf.Abs(transform.localScale.x);
        lockedFacingDirection = transform.localScale.x >= 0 ? 1f : -1f;

        // Hareket Aksiyonu
        moveAction = new InputAction("Move");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // Saldırı Aksiyonu
        attackAction = new InputAction("Attack", binding: "<Keyboard>/space");
        attackAction.performed += ctx => TryAttack();
    }

    void OnEnable()
    {
        moveAction.Enable();
        attackAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        attackAction.Disable();
    }

    void Update()
    {
        rawMovementInput = moveAction.ReadValue<Vector2>();
        normalizedInput = rawMovementInput.magnitude > 1f ? rawMovementInput.normalized : rawMovementInput;

        // Saldırı esnasında yönü zorla kilitli tut
        if (isAttacking)
        {
            transform.localScale = new Vector3(lockedFacingDirection * originalScaleX, transform.localScale.y, transform.localScale.z);
            return;
        }

        // Geri savrulurken yön değiştirmeyi engelle
        if (isKnockedBack) return;

        // Normal hareket yönü
        if (normalizedInput.x > 0.01f)
        {
            lockedFacingDirection = 1f;
            transform.localScale = new Vector3(originalScaleX, transform.localScale.y, transform.localScale.z);
        }
        else if (normalizedInput.x < -0.01f)
        {
            lockedFacingDirection = -1f;
            transform.localScale = new Vector3(-originalScaleX, transform.localScale.y, transform.localScale.z);
        }
    }

    void FixedUpdate()
    {
        if (isKnockedBack) return;

        if (isAttacking)
        {
            ApplyVelocity(Vector2.zero);
            if (anim != null) anim.SetBool(RunningParam, false);
            return;
        }

        ApplyVelocity(normalizedInput * moveSpeed);

        if (anim != null)
        {
            bool isMoving = normalizedInput.sqrMagnitude > 0.01f;
            anim.SetBool(RunningParam, isMoving);
        }
    }

    public void ApplyKnockback(Vector2 direction, float force, float duration = 0.2f)
    {
        if (!gameObject.activeInHierarchy || !isActiveAndEnabled) return;

        StartCoroutine(KnockbackRoutine(direction, force, duration));
    }

    private IEnumerator KnockbackRoutine(Vector2 direction, float force, float duration)
    {
        isKnockedBack = true;

        ApplyVelocity(direction.normalized * force);

        yield return new WaitForSeconds(duration);

        ApplyVelocity(Vector2.zero);
        isKnockedBack = false;
    }

    private void ApplyVelocity(Vector2 targetVelocity)
    {
        #if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = targetVelocity;
        #else
            rb.velocity = targetVelocity;
        #endif
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime || isAttacking || isKnockedBack) return;

        nextAttackTime = Time.time + attackCooldown;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // Saldırı anında yakındaki canlı düşmana bak ve yönü ona kilitle
        Collider2D[] nearbyEnemies = (enemyLayer.value != 0)
            ? Physics2D.OverlapCircleAll(transform.position, attackRange + attackForwardOffset, enemyLayer)
            : Physics2D.OverlapCircleAll(transform.position, attackRange + attackForwardOffset);

        Collider2D closestTarget = null;
        float shortestDist = Mathf.Infinity;

        foreach (var col in nearbyEnemies)
        {
            if (col.gameObject == gameObject) continue;
            Health h = col.GetComponentInParent<Health>();
            if (h != null && !h.IsDead)
            {
                float d = Vector2.Distance(transform.position, col.transform.position);
                if (d < shortestDist)
                {
                    shortestDist = d;
                    closestTarget = col;
                }
            }
        }

        if (closestTarget != null)
        {
            float dirToEnemy = closestTarget.transform.position.x - transform.position.x;
            if (Mathf.Abs(dirToEnemy) > 0.1f)
            {
                lockedFacingDirection = Mathf.Sign(dirToEnemy);
            }
        }
        else
        {
            lockedFacingDirection = transform.localScale.x >= 0 ? 1f : -1f;
        }

        // Kilitlenen yönü karaktere uygula
        transform.localScale = new Vector3(lockedFacingDirection * originalScaleX, transform.localScale.y, transform.localScale.z);

        if (anim != null)
        {
            anim.SetTrigger(AttackTrigger);
        }

        PerformAttackDamage(lockedFacingDirection);

        // Animasyon ve vuruş toparlanma süresi boyunca kilitli kalır
        yield return new WaitForSeconds(attackDuration);
        isAttacking = false;
    }

    public void PerformAttackDamage(float facingDir)
    {
        Vector2 hitCenter = (Vector2)transform.position + new Vector2(facingDir * attackForwardOffset, 0f);

        Collider2D[] hitColliders = (enemyLayer.value != 0)
            ? Physics2D.OverlapCircleAll(hitCenter, attackRange, enemyLayer)
            : Physics2D.OverlapCircleAll(hitCenter, attackRange);

        Collider2D closestEnemy = null;
        float shortestDistance = Mathf.Infinity;

        foreach (Collider2D hit in hitColliders)
        {
            if (hit.gameObject == gameObject) continue;

            Health h = hit.GetComponentInParent<Health>();
            if (h != null && !h.IsDead)
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < shortestDistance)
                {
                    shortestDistance = dist;
                    closestEnemy = hit;
                }
            }
        }

        if (closestEnemy != null)
        {
            // 1. Düşmanı hafifçe geriye fırlat
            EnemyController2D enemyController = closestEnemy.GetComponentInParent<EnemyController2D>();
            if (enemyController != null)
            {
                Vector2 knockbackDir = ((Vector2)closestEnemy.transform.position - (Vector2)transform.position).normalized;
                enemyController.ApplyKnockback(knockbackDir, enemyKnockbackForce, enemyKnockbackDuration);
            }

            // 2. Hasar ver
            Health enemyHealth = closestEnemy.GetComponentInParent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        float facingDir = transform.localScale.x >= 0 ? 1f : -1f;
        Vector2 hitCenter = (Vector2)transform.position + new Vector2(facingDir * attackForwardOffset, 0f);
        Gizmos.DrawWireSphere(hitCenter, attackRange);
    }
}