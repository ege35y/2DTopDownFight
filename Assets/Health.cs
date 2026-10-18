using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Can Ayarları")]
    [Tooltip("Player için 6, Enemy için 2")]
    [SerializeField] private int maxHealth = 2;
    [SerializeField] private bool isPlayer = false;

    [Header("UI Can Barı")]
    [SerializeField] private HealthBar healthBar;

    [Header("Hasar Efekti (Flash)")]
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private Color flashColor = Color.red;

    [Header("Ölüm Ayarları")]
    [Tooltip("Ölüm animasyonunun oynaması için gereken süre")]
    [SerializeField] private float deathDelay = 1.0f;

    private int currentHealth;
    private bool isDead = false;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;

    private Animator anim;
    private Rigidbody2D rb;

    private static readonly int DeathTrigger = Animator.StringToHash("Death");

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (isPlayer && maxHealth == 2)
        {
            maxHealth = 6;
        }
        currentHealth = maxHealth;

        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<HealthBar>();
        }
    }

    void Start()
    {
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0);

        // Can barını güncelle
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }

        // Hasar Efekti (Kırmızı Flash)
        if (gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(DamageFlashRoutine());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        isDead = true;

        // Fiziği tamamen kilitle
        if (rb != null)
        {
            #if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector2.zero;
            #else
                rb.velocity = Vector2.zero;
            #endif
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Kontrolleri kapat
        var playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null) playerMovement.enabled = false;

        var enemyMovement = GetComponent<EnemyController2D>();
        if (enemyMovement != null) enemyMovement.enabled = false;

        // Çarpışmaları kapat
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Ölünce can barını gizle
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }

        // Düşman öldüğünde skoru 1 artır
        if (!isPlayer && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(1);
        }

        bool hasDeathParam = HasParameter(anim, "Death");

        if (hasDeathParam)
        {
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            anim.ResetTrigger("Attack");
            anim.SetTrigger(DeathTrigger);

            StartCoroutine(DeathSequenceRoutine());
        }
        else
        {
            if (isPlayer)
            {
                Time.timeScale = 0f;
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private IEnumerator DeathSequenceRoutine()
    {
        // Realtime: timeScale'den etkilenmeden animasyon süresi kadar bekle
        yield return new WaitForSecondsRealtime(deathDelay);

        if (isPlayer)
        {
            Debug.Log("OYUNCU ÖLÜM ANİMASYONU BİTTİ - GAME OVER EKRANI AÇILIYOR!");
            
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowGameOver();
            }
            else
            {
                Time.timeScale = 0f;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private bool HasParameter(Animator animator, string paramName)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    void OnDisable()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}