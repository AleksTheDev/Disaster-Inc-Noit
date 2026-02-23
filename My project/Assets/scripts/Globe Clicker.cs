using UnityEngine;

public class GlobeClicker : MonoBehaviour
{
    public GameObject Earth;
    public CountryInfoPanel InfoPanel;
    public float LongitudeOffset = 0f;

    private CountryHighlight highlight;

    void Start()
    {
        highlight = Earth.GetComponent<CountryHighlight>();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        if (hit.collider.gameObject != Earth &&
            hit.collider.transform.parent?.gameObject != Earth) return;

        Vector3 localPoint = Earth.transform.InverseTransformPoint(hit.point).normalized;

        float lat = Mathf.Asin(localPoint.y) * Mathf.Rad2Deg;
        float lng = Mathf.Atan2(localPoint.z, localPoint.x) * Mathf.Rad2Deg;

        lng = (lng + LongitudeOffset + 360f) % 360f;
        if (lng > 180f) lng -= 360f;

        CountryLookup lookup = Earth.GetComponent<CountryLookup>();
        if (lookup == null) return;

        Country country = lookup.GetCountryAtLatLng(lat, lng);
        InfoPanel.Show(country);

        if (highlight != null)
            highlight.HighlightCountry(country);
    }
}