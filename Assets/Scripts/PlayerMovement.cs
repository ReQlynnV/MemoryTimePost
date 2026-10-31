using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 movementInput;
    private bool movementEnabled = true;

    public bool IsMovementEnabled => movementEnabled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

        if (GameSaveSystem.TryLoad(SceneManager.GetActiveScene().name, out GameSaveData saveData))
        {
            transform.position = new Vector3(saveData.playerX, saveData.playerY, saveData.playerZ);
        }
    }

    private void Update()
    {
        if (!movementEnabled)
        {
            movementInput = Vector2.zero;
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            movementInput = Vector2.zero;
            return;
        }

        movementInput = Vector2.zero;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            movementInput.y += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            movementInput.y -= 1f;
        }

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            movementInput.x -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            movementInput.x += 1f;
        }

        movementInput = Vector2.ClampMagnitude(movementInput, 1f);
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = movementEnabled ? movementInput * moveSpeed : Vector2.zero;
    }

    public void SetMovementEnabled(bool isEnabled)
    {
        movementEnabled = isEnabled;
        if (!movementEnabled)
        {
            movementInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
        }
    }
}
