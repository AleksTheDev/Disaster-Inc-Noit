using UnityEngine;

public class PinBillboard : MonoBehaviour
{
    public Camera mainCamera;

    void LateUpdate()
    {
        if (mainCamera == null) return;
        transform.LookAt(mainCamera.transform);
        transform.Rotate(0, 180f, 0);
    }
}