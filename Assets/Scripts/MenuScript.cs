using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;

    private bool isPaused;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (pauseMenu == null)
        {
            Debug.LogError("MenuScript requires a pause menu GameObject.", this);
            enabled = false;
            return;
        }

        pauseMenu.SetActive(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            TogglePauseMenu();
        }
    }

    public void TogglePauseMenu()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused)
        {
            return;
        }

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        pauseMenu.SetActive(true);
        isPaused = true;
    }

    public void ResumeGame()
    {
        if (!isPaused)
        {
            return;
        }

        pauseMenu.SetActive(false);
        Time.timeScale = previousTimeScale;
        isPaused = false;
    }

    public void SaveGame()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player == null)
        {
            Debug.LogError("Cannot save the game because no PlayerMovement was found in the scene.", this);
            return;
        }

        GameSaveData saveData = new GameSaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            playerX = player.transform.position.x,
            playerY = player.transform.position.y,
            playerZ = player.transform.position.z
        };

        if (GameSaveSystem.Save(saveData))
        {
            Debug.Log("Game saved.", this);
        }
    }

    public void EndGame()
    {
        Time.timeScale = previousTimeScale;
        Application.Quit();
    }
}
