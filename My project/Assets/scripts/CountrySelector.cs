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
                Vector3 centerToHit = hit.point - earthTransform.position;

                GameObject bordersParent = GameObject.Find("BordersParent");
                if (bordersParent != null)
                {
                    Vector3 localHit = bordersParent.transform.InverseTransformDirection(centerToHit).normalized * bordersRenderer.globeRadius;
                    string countryName = bordersRenderer.FindClosestCountry(localHit);

                    if (countryName != null)
                    {
                        bordersRenderer.SelectCountry(countryName);

                        if (countryNameText != null)
                            countryNameText.text = countryName;
                        DisasterSystem.Instance?.OnCountryClicked(countryName);
                    }
                }
            }
        }
    }
}