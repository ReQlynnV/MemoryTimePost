using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ObjectDialogueData : MonoBehaviour, IInteractable, IInteractionSession
{
    public NPCDialogue dialogueData;
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public Image npcPortraitImage;
    public TextMeshProUGUI npcNameText;
    private int dialogueIndex;
    private bool isTyping;
    private bool isDialogueActive;
    private Coroutine typingCoroutine;
    private PlayerMovement playerMovement;

    public bool IsInteractionActive => isDialogueActive;

    private void Awake()
    {
        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogError("ObjectDialogueData requires a dialogue panel and dialogue text.", this);
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (isDialogueActive)
        {
            EndDialogue();
        }
    }

    public void interact()
    {
        if (!isInteractable())
        {
            return;
        }

        if (!isDialogueActive)
        {
            StartDialogue();
            return;
        }

        if (isTyping)
        {
            StopTyping();
            dialogueText.SetText(dialogueData.dialogueLines[dialogueIndex] ?? string.Empty);
            return;
        }

        if (dialogueIndex + 1 < dialogueData.dialogueLines.Length)
        {
            dialogueIndex++;
            typingCoroutine = StartCoroutine(TypeLine());
        }
        else
        {
            enddialogue();
        }
    }

    public bool isInteractable()
    {
        return dialogueData != null
            && dialogueData.dialogueLines != null
            && dialogueData.dialogueLines.Length > 0
            && dialoguePanel != null
            && dialogueText != null;
    }

    private void StartDialogue()
    {
        isDialogueActive = true;
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("ObjectDialogueData could not find a PlayerMovement component to pause.", this);
        }
        else
        {
            playerMovement.SetMovementEnabled(false);
        }

        dialogueIndex = 0;
        dialoguePanel.SetActive(true);
        if (npcNameText != null)
        {
            npcNameText.SetText(dialogueData.npcName ?? string.Empty);
        }

        if (npcPortraitImage != null)
        {
            npcPortraitImage.sprite = dialogueData.npcPortrait;
        }

        typingCoroutine = StartCoroutine(TypeLine());
    }

    private IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.SetText(string.Empty);

        string line = dialogueData.dialogueLines[dialogueIndex] ?? string.Empty;
        foreach (char character in line)
        {
            dialogueText.text += character;
            yield return new WaitForSeconds(dialogueData.typingSpeed);
        }

        isTyping = false;
        typingCoroutine = null;
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

    public void enddialogue()
    {
        if (isDialogueActive)
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        StopTyping();
        isDialogueActive = false;
        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(true);
            playerMovement = null;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }
}
