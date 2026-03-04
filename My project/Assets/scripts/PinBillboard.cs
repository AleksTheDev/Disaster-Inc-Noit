using UnityEngine;

public class PinBillboard : MonoBehaviour
{
    public Camera mainCamera;
    public Transform earthTransform;
    public float pinDistance = 6.3f;

    void LateUpdate()
    {
        if (mainCamera == null || earthTransform == null) return;

        Vector3 dirFromCenter = (transform.position - earthTransform.position).normalized;
        transform.position = earthTransform.position + dirFromCenter * pinDistance;

        transform.LookAt(mainCamera.transform);
        transform.Rotate(0, 180f, 0);
    }
}