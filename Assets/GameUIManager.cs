using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // Yeni Input System

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("UI Panelleri")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject gameOverPanel;

    private enum GameState { StartMenu, Playing, GameOver }
    private GameState currentState = GameState.StartMenu;

    // Yeni Input System aksiyonları (Time.timeScale = 0 olsa bile kusursuz çalışır)
    private InputAction spaceAction;
    private InputAction escapeAction;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // FPS kitleme ve kasma önleme
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;

        // Space ve ESC tuşlarını doğrudan Input System'e kaydediyoruz
        spaceAction = new InputAction("UI_Space", binding: "<Keyboard>/space");
        spaceAction.performed += ctx => OnSpacePressed();

        escapeAction = new InputAction("UI_Escape", binding: "<Keyboard>/escape");
        escapeAction.performed += ctx => OnEscapePressed();
    }

    void OnEnable()
    {
        spaceAction?.Enable();
        escapeAction?.Enable();
    }

    void OnDisable()
    {
        spaceAction?.Disable();
        escapeAction?.Disable();
    }

    void Start()
    {
        currentState = GameState.StartMenu;
        Time.timeScale = 0f; // Başlangıçta zamanı durdur

        if (startPanel != null) startPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void OnSpacePressed()
    {
        // Başlangıç ekranındaysak oyunu başlat
        if (currentState == GameState.StartMenu)
        {
            StartGame();
        }
        // Game Over ekranındaysak oyunu yeniden başlat
        else if (currentState == GameState.GameOver)
        {
            RestartGame();
        }
    }

    private void OnEscapePressed()
    {
        // ESC basıldığı an oyunu kapat
        QuitGame();
    }

    private void StartGame()
    {
        currentState = GameState.Playing;
        if (startPanel != null) startPanel.SetActive(false);
        Time.timeScale = 1f; // Zamanı başlat
    }

    public void ShowGameOver()
    {
        currentState = GameState.GameOver;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0f; // Oyunu dondur
    }

    private void RestartGame()
    {
        Time.timeScale = 1f; // Sahne yüklenmeden önce zamanı normale çevir
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    private void QuitGame()
    {
        Debug.Log("ESC basıldı, oyundan çıkılıyor...");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}