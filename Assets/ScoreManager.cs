using UnityEngine;
using TMPro; // TextMeshPro için

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI Metin Bileşeni")]
    [SerializeField] private TextMeshProUGUI scoreText;

    private int currentScore = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        UpdateScoreUI();
    }

    public void AddScore(int amount = 1)
    {
        currentScore += amount;
        Debug.Log("Yeni Skor: " + currentScore);
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore;
        }
        else
        {
            Debug.LogError("ScoreText nesnesi Inspector'da atanmamış!");
        }
    }
}