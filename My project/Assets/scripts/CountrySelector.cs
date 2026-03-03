using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CountrySelector : MonoBehaviour
{
    [Header("References")]
    public BordersRenderer bordersRenderer;
    public Camera mainCamera;
    public Transform earthTransform;

    [Header("UI")]
    public TextMeshProUGUI countryNameText;

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            TrySelectCountry();
    }

    void TrySelectCountry()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == earthTransform || hit.transform.IsChildOf(earthTransform))
            {
                Vector3 localHit = earthTransform.InverseTransformPoint(hit.point);
                string countryName = bordersRenderer.FindClosestCountry(localHit);

                if (countryName != null)
                {
                    bordersRenderer.SelectCountry(countryName);
                    if (countryNameText != null)
                        countryNameText.text = countryName;
                }
            }
        }
    }
}