using UnityEngine;
using UnityEngine.InputSystem;

public class BallController : MonoBehaviour
{
    [Header("Configurações do Arremesso")]
    public float moveSpeed = 3f;
    public float throwForce = 1200f;

    private Rigidbody rb;
    private bool isThrown = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (!isThrown && keyboard != null)
        {
            // Mover para a esquerda e direita com A/D ou setas direcionais
            int right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0;
            int left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0;
            float moveInput = right - left;
            transform.Translate(Vector3.right * moveInput * moveSpeed * Time.deltaTime);

            // Arremessar ao pressionar a barra de Espaço
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                ThrowBall();
            }
        }
    }

    void ThrowBall()
    {
        isThrown = true;
        rb.AddForce(Vector3.forward * throwForce, ForceMode.Impulse);
    }
}
