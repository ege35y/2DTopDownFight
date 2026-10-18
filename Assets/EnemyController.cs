using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class EnemyController2D : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float moveSpeed = 3f;
    [Tooltip("Düşmanın durup saldırıya hazırlandığı mesafe")]
    [SerializeField] private float attackRange = 1f;

    [Header("Saldırı ve Dodge Ayarları")]
    [SerializeField] private int attackDamage = 1;
    [Tooltip("Saldırıya başlamadan önce bekleme süresi (Dodge penceresi)")]
    [SerializeField] private float attackWindup = 0.5f;
    [Tooltip("Saldırılar arası bekleme süresi")]
    [SerializeField] private float attackCooldown = 1.5f;
    [Tooltip("Vurduğunda oyuncuyu geriye itme gücü")]
    [SerializeField] private float knockbackForce = 10f;
    [Tooltip("Geriye savrulma süresi")]
    [SerializeField] private float knockbackDuration = 0.2f;

    private Transform playerTransform;
    private Rigidbody2D rb;
    private Animator anim;
    private bool isAttacking = false;
    private bool isKnockedBack = false;
    private float nextAttackAvailableTime = 0f;

    private static readonly int AttackTrigger = Animator.StringToHash("Attack");

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    void FixedUpdate()
    {
        // Geriye sekiyorsa yapay zeka hareketini durdur
        if (isKnockedBack) return;

        // Oyuncu yoksa veya inaktifse dur
        if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
        {
            StopMovement();
            return;
        }

        // Saldırı hazırlığında veya animasyon oynarken hareket etmesin
        if (isAttacking)
        {
            StopMovement();
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        // Menzil dışındaysa oyuncuya doğru koş
        if (distance > attackRange)
        {
            Vector2 direction = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);

            // Sağa / Sola yüzünü dön
            if (direction.x > 0.05f)
            {
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            else if (direction.x < -0.05f)
            {
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
        }
        else
        {
            // Menzile girdi: Dur ve bekleme süresi dolduysa saldırı döngüsünü başlat
            StopMovement();

            if (Time.time >= nextAttackAvailableTime)
            {
                StartCoroutine(PerformAttackRoutine());
            }
        }
    }

    public void ApplyKnockback(Vector2 direction, float force, float duration = 0.15f)
    {
        if (!gameObject.activeInHierarchy || !isActiveAndEnabled) return;

        StartCoroutine(KnockbackRoutine(direction, force, duration));
    }

    private IEnumerator KnockbackRoutine(Vector2 direction, float force, float duration)
    {
        isKnockedBack = true;

        #if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = direction.normalized * force;
        #else
            rb.velocity = direction.normalized * force;
        #endif

        yield return new WaitForSeconds(duration);

        StopMovement();
        isKnockedBack = false;
    }

    private void StopMovement()
    {
        #if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
        #else
            rb.velocity = Vector2.zero;
        #endif
    }

    private IEnumerator PerformAttackRoutine()
    {
        isAttacking = true;

        anim.SetTrigger(AttackTrigger);
        yield return new WaitForSeconds(attackWindup);

        if (playerTransform != null && playerTransform.gameObject.activeInHierarchy)
        {
            float distance = Vector2.Distance(transform.position, playerTransform.position);

            if (distance <= attackRange + 0.4f)
            {
                PlayerMovement playerMovement = playerTransform.GetComponent<PlayerMovement>();
                if (playerMovement != null)
                {
                    Vector2 knockbackDir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                    playerMovement.ApplyKnockback(knockbackDir, knockbackForce, knockbackDuration);
                }

                Health playerHealth = playerTransform.GetComponent<Health>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                }
            }
        }

        nextAttackAvailableTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}