using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CutsceneScript : MonoBehaviour, IInteractable, IInteractionSession
{
    [Serializable]
    private class ComicPanel
    {
        public Sprite artwork = null;
        [TextArea(2, 5)] public string dialogue = string.Empty;
    }

    [SerializeField] private GameObject cutscenePanel;
    [SerializeField] private Image artworkImage;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private ComicPanel[] panels;
    [SerializeField, Min(0f)] private float typewriterDelay = 0.035f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.5f;
    [SerializeField] private bool playAtStart;
    [SerializeField] private bool playOnlyOnce = true;
    [SerializeField] private string cutsceneId = string.Empty;
    [SerializeField] private CutsceneDialogues dialogueAfterCutscene;
    [SerializeField] private UnityEvent onCutsceneComplete;

    private const string PlayedKeyPrefix = "CutsceneScript.Played.";

    private int panelIndex;
    private bool isPlaying;
    private bool isTyping;
    private bool isFading;
    private bool isFinishing;
    private Coroutine typingCoroutine;
    private CanvasGroup cutsceneCanvasGroup;
    private PlayerMovement playerMovement;
    private bool previousMovementEnabled;
    private int lastInteractionFrame = -1;

    public bool IsInteractionActive => isPlaying;

    public static void ResetPlayedState(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            Debug.LogError("A cutscene ID is required to reset its played state.");
            return;
        }

        PlayerPrefs.DeleteKey(PlayedKeyPrefix + identifier.Trim());
        PlayerPrefs.Save();
    }

    private void Awake()
    {
        if (cutscenePanel == null || artworkImage == null || dialogueText == null)
        {
            Debug.LogError(
                "CutsceneScript requires a cutscene panel, artwork image, and dialogue text.",
                this);
            enabled = false;
            return;
        }

        cutscenePanel.SetActive(true);
        cutsceneCanvasGroup = cutscenePanel.GetComponent<CanvasGroup>();
        if (cutsceneCanvasGroup == null)
        {
            cutsceneCanvasGroup = cutscenePanel.AddComponent<CanvasGroup>();
        }

        RectTransform panelRect = cutscenePanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchoredPosition = Vector2.zero;
        }

        SetCutsceneVisibility(0f);
    }

    private void Start()
    {
        int launchMode = PlayerPrefs.GetInt(MainMenuScript.LaunchModeKey, 0);
        if (launchMode != 0)
        {
            PlayerPrefs.DeleteKey(MainMenuScript.LaunchModeKey);
            PlayerPrefs.Save();
        }

        if (launchMode == MainMenuScript.NewGameMode ||
            (launchMode == 0 && playAtStart))
        {
            Play();
        }
    }

    private void Update()
    {
        HandlePlaybackInput();
    }

    private void HandlePlaybackInput()
    {
        if (!isPlaying || isFading || isFinishing)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            FinishCutscene(false);
            return;
        }

        bool advancePressed = (keyboard != null
                && (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            || (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame);

        if (advancePressed)
        {
            interact();
        }
    }

    private void OnDisable()
    {
        if (isPlaying)
        {
            CompleteCutscene(false);
        }
    }

    public bool isInteractable()
    {
        return isPlaying
            || ((panels != null && panels.Length > 0)
                && (!playOnlyOnce || PlayerPrefs.GetInt(GetPlayedKey(), 0) != 1));
    }

    public void interact()
    {
        if (!isInteractable() || isFading || isFinishing || lastInteractionFrame == Time.frameCount)
        {
            return;
        }

        lastInteractionFrame = Time.frameCount;
        if (!isPlaying)
        {
            Play();
        }
        else if (isTyping)
        {
            ShowFullDialogue();
        }
        else if (panelIndex + 1 < panels.Length)
        {
            panelIndex++;
            ShowPanel();
        }
        else
        {
            FinishCutscene(true);
        }
    }

    public void Play()
    {
        if (isPlaying || (playOnlyOnce && PlayerPrefs.GetInt(GetPlayedKey(), 0) == 1))
        {
            return;
        }

        if (panels == null || panels.Length == 0)
        {
            Debug.LogError("CutsceneScript requires at least one comic panel.", this);
            return;
        }

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == null)
            {
                Debug.LogError($"CutsceneScript panel {i} is not configured.", this);
                return;
            }
        }

        isPlaying = true;
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            previousMovementEnabled = playerMovement.IsMovementEnabled;
            playerMovement.SetMovementEnabled(false);
        }

        panelIndex = 0;
        cutscenePanel.SetActive(true);
        SetCutsceneVisibility(0f);
        ShowPanel();
        StartCoroutine(FadeCutscene(1f));
    }

    private void ShowPanel()
    {
        StopTyping();
        ComicPanel panel = panels[panelIndex];
        artworkImage.sprite = panel.artwork;
        artworkImage.enabled = panel.artwork != null;

        if (typewriterDelay <= 0f)
        {
            dialogueText.SetText(panel.dialogue ?? string.Empty);
            isTyping = false;
            return;
        }

        typingCoroutine = StartCoroutine(TypeDialogue(panel.dialogue ?? string.Empty));
    }

    private IEnumerator TypeDialogue(string dialogue)
    {
        isTyping = true;
        dialogueText.SetText(string.Empty);

        foreach (char character in dialogue)
        {
            dialogueText.text += character;
            yield return new WaitForSecondsRealtime(typewriterDelay);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    private void ShowFullDialogue()
    {
        StopTyping();
        dialogueText.SetText(panels[panelIndex].dialogue ?? string.Empty);
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
    }

    private void FinishCutscene(bool invokeCompletion)
    {
        if (!isPlaying || isFinishing)
        {
            return;
        }

        StopTyping();
        if (fadeDuration > 0f && cutsceneCanvasGroup != null)
        {
            isFinishing = true;
            StartCoroutine(FadeOutAndFinish(invokeCompletion));
            return;
        }

        CompleteCutscene(invokeCompletion);
    }

    private IEnumerator FadeOutAndFinish(bool invokeCompletion)
    {
        yield return FadeCutscene(0f);
        CompleteCutscene(invokeCompletion);
    }

    private IEnumerator FadeCutscene(float targetAlpha)
    {
        isFading = true;
        float startAlpha = cutsceneCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            cutsceneCanvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        SetCutsceneVisibility(targetAlpha);
        isFading = false;
    }

    private void SetCutsceneVisibility(float alpha)
    {
        cutsceneCanvasGroup.alpha = alpha;
        bool visible = alpha > 0f;
        cutsceneCanvasGroup.interactable = visible;
        cutsceneCanvasGroup.blocksRaycasts = visible;
    }

    private void CompleteCutscene(bool invokeCompletion)
    {
        StopTyping();
        isPlaying = false;
        isFading = false;
        isFinishing = false;
        if (cutsceneCanvasGroup != null)
        {
            SetCutsceneVisibility(0f);
        }

        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(previousMovementEnabled);
            playerMovement = null;
        }

        if (invokeCompletion)
        {
            if (playOnlyOnce)
            {
                PlayerPrefs.SetInt(GetPlayedKey(), 1);
                PlayerPrefs.Save();
            }

            onCutsceneComplete?.Invoke();
            if (dialogueAfterCutscene != null)
            {
                dialogueAfterCutscene.Play();
            }
        }
    }

    private string GetPlayedKey()
    {
        string identifier = string.IsNullOrWhiteSpace(cutsceneId)
            ? $"{gameObject.scene.name}_{gameObject.name}"
            : cutsceneId.Trim();
        return PlayedKeyPrefix + identifier;
    }
}
