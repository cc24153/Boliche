using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Alvo")]
    public Transform target; // Arraste a Bola para este campo no Inspector
    public float distance = 4.0f; // Distância em relação à bola

    [Header("Sensibilidade e Limites")]
    public float sensitivity = 3.0f;
    public float minYAngle = -10.0f; // Limite para olhar para baixo
    public float maxYAngle = 60.0f;  // Limite para olhar para cima

    private float currentX = 0.0f;
    private float currentY = 0.0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;
    }

    void Update()
    {
        // Roda a câmara enquanto manténs o botão direito do rato pressionado
        if (Input.GetMouseButton(1)) 
        {
            Cursor.lockState = CursorLockMode.Confined;

            currentX += Input.GetAxis("Mouse X") * sensitivity;
            currentY -= Input.GetAxis("Mouse Y") * sensitivity;

            currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void LateUpdate()
    {
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        if (target != null)
        {
            // Modo Órbita: Gira ao redor do alvo mantendo a distância
            Vector3 dir = new Vector3(0, 0, -distance);
            transform.position = target.position + rotation * dir;
            transform.LookAt(target.position);
        }
        else
        {
            // Modo Livre: Gira no próprio eixo da câmara
            transform.rotation = rotation;
        }
    }
}
