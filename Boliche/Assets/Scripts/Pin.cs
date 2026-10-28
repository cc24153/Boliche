using UnityEngine;

public class Pin : MonoBehaviour
{
    // Retorna 'true' se o pino estiver inclinado mais de 30 graus em relação à vertical
    public bool IsFallen()
    {
        return Vector3.Angle(transform.up, Vector3.up) > 30f;
    }
}
