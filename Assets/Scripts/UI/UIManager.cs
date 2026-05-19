using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject levelsPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("UI Texts")]
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TextMeshProUGUI gameOverScore;
    [SerializeField] private TextMeshProUGUI gameOverTreasures;
    [SerializeField] private TextMeshProUGUI gameOverStreak;
    [SerializeField] private TextMeshProUGUI gameOverHighScore;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button mainMenuSettingsButton;

    [Header("Levels Buttons")]
    [SerializeField] private Button beginnerButton;
    [SerializeField] private Button intermediateButton;
    [SerializeField] private Button difficultButton;

    [Header("Pause Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseHomeButton;
    [SerializeField] private Button pauseSettingsButton;

    [Header("Game Over Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button gameOverHomeButton;

    [Header("Settings Buttons")]
    [SerializeField] private Button backButton;

    [Header("Dependencies")]
    public ChestMovement playerMovement;
    public ScoreManager scoreManager;

    private static bool skipMenu;
    private static bool skipLevelPanel;

    private void Awake()
    {
        startButton?.onClick.AddListener(StartButton);
        quitButton?.onClick.AddListener(QuitButton);
        mainMenuSettingsButton?.onClick.AddListener(ShowSettings);

        beginnerButton?.onClick.AddListener(Beginner);
        intermediateButton?.onClick.AddListener(Intermediate);
        difficultButton?.onClick.AddListener(Difficult);

        resumeButton?.onClick.AddListener(ResumeButton);
        pauseHomeButton?.onClick.AddListener(HomeButton);
        pauseSettingsButton?.onClick.AddListener(ShowSettings);

        restartButton?.onClick.AddListener(Restart);
        gameOverHomeButton?.onClick.AddListener(HomeButton);

        backButton?.onClick.AddListener(BackButton);
    }

    private void Start()
    {
        Time.timeScale = 0f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (!skipMenu)
        {
            mainMenuPanel?.SetActive(true);
            return;
        }

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        skipMenu = false;

        if (skipLevelPanel)
        {
            if (levelsPanel != null)
                levelsPanel.SetActive(false);
            skipLevelPanel = false;
            Time.timeScale = 1f;
        }
        else
        {
            levelsPanel?.SetActive(true);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnGameOver += ShowGameOverScreen;
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver -= ShowGameOverScreen;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pausePanel != null && !pausePanel.activeSelf && Time.timeScale > 0f)
                ShowPause();
            else if (pausePanel != null && pausePanel.activeSelf)
                ResumeButton();
        }
    }

    public void StartButton()
    {
        skipMenu = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitButton()
    {
        Application.Quit();
    }

    public void Beginner()
    {
        PlayerPrefs.SetInt("Level", 0);
        levelsPanel?.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Intermediate()
    {
        PlayerPrefs.SetInt("Level", 1);
        levelsPanel?.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Difficult()
    {
        PlayerPrefs.SetInt("Level", 2);
        levelsPanel?.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ShowPause()
    {
        pausePanel?.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeButton()
    {
        StartCoroutine(ResumeCountdown());
    }

    public void HomeButton()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        mainMenuPanel?.SetActive(true);
    }

    public void BackButton()
    {
        settingsPanel?.SetActive(false);
    }

    public void ShowSettings()
    {
        settingsPanel?.SetActive(true);
    }

    public void Restart()
    {
        skipMenu = true;
        skipLevelPanel = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ShowGameOverScreen()
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);

        if (scoreManager == null) return;

        if (gameOverScore != null)
            gameOverScore.text = $"Score: {scoreManager.CurrentScore}";

        if (gameOverTreasures != null)
            gameOverTreasures.text = $"Treasures: {scoreManager.TreasuresCollected}";

        if (gameOverStreak != null)
            gameOverStreak.text = $"Best Streak: {scoreManager.BestStreak}x";

        if (gameOverHighScore != null)
            gameOverHighScore.text = $"High Score: {scoreManager.HighScore}";
    }

    private IEnumerator ResumeCountdown()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (countdownPanel != null) countdownPanel.SetActive(true);

        if (playerMovement != null)
            playerMovement.enabled = false;

        Time.timeScale = 1f;

        if (countdownText != null)
        {
            countdownText.text = "3";
            yield return new WaitForSeconds(1f);
            countdownText.text = "2";
            yield return new WaitForSeconds(1f);
            countdownText.text = "1";
            yield return new WaitForSeconds(1f);
        }

        if (countdownPanel != null) countdownPanel.SetActive(false);
        if (playerMovement != null)
            playerMovement.enabled = true;
    }
}
