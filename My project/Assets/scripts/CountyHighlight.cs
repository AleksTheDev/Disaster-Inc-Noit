using UnityEngine;

public class CountryHighlight : MonoBehaviour
{
    public Material HighlightMaterial;
    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = HighlightMaterial;
        lineRenderer.startWidth = 0.03f;
        lineRenderer.endWidth = 0.03f;
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 0;
    }

    public void HighlightCountry(Country country)
    {
        if (country == null)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        int stepsPerEdge = 20;
        int totalPoints = stepsPerEdge * 4;
        lineRenderer.positionCount = totalPoints;

        int idx = 0;

        for (int i = 0; i < stepsPerEdge; i++)
        {
            float t = (float)i / stepsPerEdge;
            float lng = Mathf.Lerp(country.minLng, country.maxLng, t);
            lineRenderer.SetPosition(idx++, LatLngToLocal(country.minLat, lng));
        }

        for (int i = 0; i < stepsPerEdge; i++)
        {
            float t = (float)i / stepsPerEdge;
            float lat = Mathf.Lerp(country.minLat, country.maxLat, t);
            lineRenderer.SetPosition(idx++, LatLngToLocal(lat, country.maxLng));
        }

        for (int i = 0; i < stepsPerEdge; i++)
        {
            float t = (float)i / stepsPerEdge;
            float lng = Mathf.Lerp(country.maxLng, country.minLng, t);
            lineRenderer.SetPosition(idx++, LatLngToLocal(country.maxLat, lng));
        }
        
        for (int i = 0; i < stepsPerEdge; i++)
        {
            float t = (float)i / stepsPerEdge;
            float lat = Mathf.Lerp(country.maxLat, country.minLat, t);
            lineRenderer.SetPosition(idx++, LatLngToLocal(lat, country.minLng));
        }
    }

    private Vector3 LatLngToLocal(float lat, float lng)
    {

        float r = 0.5f;
        float latRad = lat * Mathf.Deg2Rad;
        float lngRad = lng * Mathf.Deg2Rad;
        return new Vector3(
            r * Mathf.Cos(latRad) * Mathf.Cos(lngRad),
            r * Mathf.Sin(latRad),
            r * Mathf.Cos(latRad) * Mathf.Sin(lngRad)
        );
    }
}