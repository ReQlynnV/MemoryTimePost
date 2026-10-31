using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuScript : MonoBehaviour
{
    public const string LaunchModeKey = "MainMenu.LaunchMode";
    public const int NewGameMode = 1;
    public const int ContinueGameMode = 2;

    [SerializeField] private Button startButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private string gameplaySceneName = "Prologue Gameplay";
    [SerializeField] private string prologueCutsceneId = "0";

    private void Awake()
    {
        ResolveButtons();
        BindButtons();
        RefreshContinueButton();
    }

    public void RefreshContinueButton()
    {
        if (continueButton != null)
        {
            continueButton.interactable = HasValidSave();
        }
    }

    public void StartNewGame()
    {
        if (!CanLoadGameplayScene())
        {
            return;
        }

        if (!GameSaveSystem.DeleteSave())
        {
            return;
        }

        CutsceneScript.ResetPlayedState(prologueCutsceneId);
        PlayerPrefs.SetInt(LaunchModeKey, NewGameMode);
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void ContinueGame()
    {
        if (!GameSaveSystem.TryLoad(gameplaySceneName, out GameSaveData saveData))
        {
            Debug.LogWarning("Cannot continue because no valid save exists for the gameplay scene.", this);
            RefreshContinueButton();
            return;
        }

        if (!CanLoadGameplayScene())
        {
            return;
        }

        PlayerPrefs.SetInt(LaunchModeKey, ContinueGameMode);
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        SceneManager.LoadScene(saveData.sceneName);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private bool HasValidSave()
    {
        return !string.IsNullOrWhiteSpace(gameplaySceneName)
            && GameSaveSystem.TryLoad(gameplaySceneName, out _);
    }

    private void ResolveButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        startButton = FindButton(buttons, startButton, "Start");
        continueButton = FindButton(buttons, continueButton, "Continue");
        exitButton = FindButton(buttons, exitButton, "Exit");

        if (startButton == null || continueButton == null || exitButton == null)
        {
            Debug.LogError(
                "MainMenuScript requires Start, Continue, and Exit buttons assigned or named under its GameObject.",
                this);
        }
    }

    private static Button FindButton(Button[] buttons, Button assignedButton, string buttonName)
    {
        if (assignedButton != null)
        {
            return assignedButton;
        }

        foreach (Button button in buttons)
        {
            if (string.Equals(button.gameObject.name, buttonName, System.StringComparison.OrdinalIgnoreCase))
            {
                return button;
            }
        }

        return null;
    }

    private void BindButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartNewGame);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueGame);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitGame);
        }
    }

    private bool CanLoadGameplayScene()
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogError("MainMenuScript requires a gameplay scene name.", this);
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            Debug.LogError(
                $"Gameplay scene '{gameplaySceneName}' is not included in Build Settings.",
                this);
            return false;
        }

        return true;
    }
}
