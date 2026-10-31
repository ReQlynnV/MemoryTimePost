using UnityEngine;
using UnityEngine.InputSystem;

public class InteractableDetector : MonoBehaviour
{
    private IInteractable interactableInRange = null;
    private bool isInRange;
    public GameObject interactionIcon;

    private void Awake()
    {
        if (interactionIcon == null)
        {
            Debug.LogError("InteractableDetector requires an interaction icon.", this);
            enabled = false;
            return;
        }

        interactionIcon.SetActive(false);
    }

    private void Update()
    {
        if (interactableInRange == null)
        {
            return;
        }

        bool hasActiveSession = interactableInRange is IInteractionSession session
            && session.IsInteractionActive;
        if (!interactableInRange.isInteractable() || (!isInRange && !hasActiveSession))
        {
            ClearInteractable();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        bool interactPressed = (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            || (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame);

        if (!interactPressed)
        {
            return;
        }

        interactableInRange.interact();
        hasActiveSession = interactableInRange is IInteractionSession activeSession
            && activeSession.IsInteractionActive;
        if (!interactableInRange.isInteractable() || (!isInRange && !hasActiveSession))
        {
            ClearInteractable();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out IInteractable interactable) && interactable.isInteractable())
        {
            interactableInRange = interactable;
            isInRange = true;
            interactionIcon.SetActive(true);
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out IInteractable interactable) && interactable == interactableInRange)
        {
            isInRange = false;
            if (interactableInRange is IInteractionSession session && session.IsInteractionActive)
            {
                interactionIcon.SetActive(false);
            }
            else
            {
                ClearInteractable();
            }
        }
    }

    private void ClearInteractable()
    {
        interactableInRange = null;
        isInRange = false;
        interactionIcon.SetActive(false);
    }
}
