using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;

public class CutsceneDialogues : MonoBehaviour
{
    [SerializeField] private NPCDialogue dialogueData;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private Image speakerPortraitImage;
    [SerializeField] private PlayableDirector timelineDirector;
    [SerializeField] private UnityEvent onDialogueComplete;

    private int dialogueIndex;
    private bool isActive;
    private bool isTyping;
    private Coroutine typingCoroutine;
    private PlayerMovement playerMovement;
    private bool previousMovementEnabled;

    public bool IsDialogueActive => isActive;

    private void Awake()
    {
        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogError(
                "CutsceneDialogues requires a dialogue panel and dialogue text.",
                this);
            enabled = false;
            return;
        }

        dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame))
        {
            FinishDialogue(true);
            return;
        }

        bool advancePressed = (keyboard != null
                && (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            || (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame);

        if (advancePressed)
        {
            AdvanceDialogue();
        }
    }

    private void OnDisable()
    {
        if (isActive)
        {
            FinishDialogue(false);
        }
    }

    public void Play()
    {
        if (isActive)
        {
            return;
        }

        if (dialogueData == null || dialogueData.dialogueLines == null
            || dialogueData.dialogueLines.Length == 0)
        {
            Debug.LogWarning(
                "CutsceneDialogues has no dialogue lines configured; skipping the prologue dialogue.",
                this);
            return;
        }

        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogError(
                "CutsceneDialogues cannot play without a dialogue panel and dialogue text.",
                this);
            return;
        }

        isActive = true;
        dialogueIndex = 0;
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            previousMovementEnabled = playerMovement.IsMovementEnabled;
            playerMovement.SetMovementEnabled(false);
        }

        if (speakerNameText != null)
        {
            speakerNameText.SetText(dialogueData.npcName ?? string.Empty);
        }

        if (speakerPortraitImage != null)
        {
            speakerPortraitImage.sprite = dialogueData.npcPortrait;
            speakerPortraitImage.enabled = dialogueData.npcPortrait != null;
        }

        dialoguePanel.SetActive(true);
        if (timelineDirector != null)
        {
            if (timelineDirector.playableAsset == null)
            {
                Debug.LogError(
                    "CutsceneDialogues has a Timeline Director without a playable asset.",
                    this);
            }
            else
            {
                timelineDirector.Play();
            }
        }

        ShowCurrentLine();
    }

    public void Advance()
    {
        if (isActive)
        {
            AdvanceDialogue();
        }
    }

    private void AdvanceDialogue()
    {
        if (isTyping)
        {
            ShowFullLine();
            return;
        }

        if (dialogueIndex + 1 < dialogueData.dialogueLines.Length)
        {
            dialogueIndex++;
            ShowCurrentLine();
            return;
        }

        FinishDialogue(true);
    }

    private void ShowCurrentLine()
    {
        StopTyping();
        string line = dialogueData.dialogueLines[dialogueIndex] ?? string.Empty;
        float typingSpeed = dialogueData.typingSpeed;
        if (typingSpeed <= 0f)
        {
            dialogueText.SetText(line);
            return;
        }

        typingCoroutine = StartCoroutine(TypeLine(line, typingSpeed));
    }

    private IEnumerator TypeLine(string line, float typingSpeed)
    {
        isTyping = true;
        dialogueText.SetText(string.Empty);

        foreach (char character in line)
        {
            dialogueText.text += character;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    private void ShowFullLine()
    {
        StopTyping();
        dialogueText.SetText(dialogueData.dialogueLines[dialogueIndex] ?? string.Empty);
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

    private void FinishDialogue(bool invokeCompletion)
    {
        StopTyping();
        isActive = false;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(previousMovementEnabled);
            playerMovement = null;
        }

        if (invokeCompletion)
        {
            onDialogueComplete?.Invoke();
        }
    }
}
