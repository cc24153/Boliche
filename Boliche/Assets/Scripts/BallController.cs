using UnityEngine;

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
        if (!isThrown)
        {
            // Mover para a esquerda e direita com A/D ou setas direcionais
            float moveInput = Input.GetAxis("Horizontal");
            transform.Translate(Vector3.right * moveInput * moveSpeed * Time.deltaTime);

            // Arremessar ao pressionar a barra de Espaço
            if (Input.GetKeyDown(KeyCode.Space))
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
