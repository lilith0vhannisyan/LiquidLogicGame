using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("HUD")]
    [SerializeField] private GameObject hud;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI flaskCounterText;
    [SerializeField] private TextMeshProUGUI conveyorText;

    [Header("Win Screen")]
    [SerializeField] private GameObject winScreen;
    [SerializeField] private TextMeshProUGUI winLevelText;
    [SerializeField] private GameObject nextLevelButtonObj;
    [SerializeField] private GameObject retryWinButtonObj;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject retryLoseButtonObj;

    private int totalFlasks = 0;
    private int deliveredFlasks = 0;

    private void Awake() => Instance = this;

    private void Start()
    {
        hud.SetActive(true);
        winScreen.SetActive(false);
        gameOverPanel.SetActive(false);
        GameManager.Instance.LoadLevel(0);
    }


    public void SetupLevel(int levelNumber, int flaskTotal)
    {
        totalFlasks = flaskTotal;
        deliveredFlasks = 0;

        if (levelText != null)
            levelText.text = $"Level {levelNumber + 1}";

        UpdateFlaskCounter();
        UpdateConveyorDisplay();
    }

    public void OnFlaskDelivered()
    {
        deliveredFlasks++;
        UpdateFlaskCounter();
    }

    private void UpdateFlaskCounter()
    {
        string newText = $"Flasks: {deliveredFlasks}/{totalFlasks}";
        if (flaskCounterText != null && flaskCounterText.text != newText)
            flaskCounterText.text = newText;
    }

    public void UpdateConveyorDisplay()
    {
        if (conveyorText == null) return;
        int free = ConveyorManager.Instance.GetFreeSlotCount();
        int total = ConveyorManager.Instance.GetTotalSlotCount();
        string newText = $"Conveyor: {total - free}/{total}";
        if (conveyorText.text != newText)
            conveyorText.text = newText;
    }

    public void ShowWinScreen(int levelNumber)
    {
        if (winScreen == null) return;

        hud.SetActive(false);
        winScreen.SetActive(true);
        winScreen.transform.localScale = Vector3.zero;
        winScreen.transform
            .DOScale(Vector3.one, 0.5f)
            .SetEase(Ease.OutBack);

        if (winLevelText != null)
            winLevelText.text = $"Level {levelNumber + 1}\nComplete!!";

        // Show/hide next level button based on available levels
        bool hasNextLevel = GameManager.Instance.CurrentLevel + 1
                            < GameManager.Instance.TotalLevels;
        if (nextLevelButtonObj != null)
            nextLevelButtonObj.SetActive(hasNextLevel);

        // Retry always visible on win screen
        if (retryWinButtonObj != null)
            retryWinButtonObj.SetActive(true);
    }

    public void OnNextLevelButton()
    {
        winScreen.SetActive(false);
        hud.SetActive(true);
        GameManager.Instance.LoadNextLevel();
    }

    public void OnMenuButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void ShowGameOver()
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);
        gameOverPanel.transform.localScale = Vector3.zero;
        gameOverPanel.transform
            .DOScale(Vector3.one, 0.5f)
            .SetEase(Ease.OutBack);
    }

    public void OnRetryButton()
    {
        winScreen.SetActive(false);
        gameOverPanel.SetActive(false);
        hud.SetActive(true);
        GameManager.Instance.LoadLevel(GameManager.Instance.CurrentLevel);
    }
}