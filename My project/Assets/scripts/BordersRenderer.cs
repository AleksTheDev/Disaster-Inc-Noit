using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class CountryData
{
    public string name;
    public List<LineRenderer> lineRenderers = new List<LineRenderer>();
}

public class BordersRenderer : MonoBehaviour
{
    [Header("Globe Settings")]
    public float globeRadius = 6f;

    [Header("Map Orientation")]
    public bool flipLongitude = false;
    [Range(-180f, 180f)]
    public float longitudeOffset = 0f;
    [Range(-90f, 90f)]
    public float latitudeOffset = 0f;

    [Header("Map Shape")]
    [Range(0.5f, 1.5f)]
    public float latScale = 1f;

    [Header("Rendering")]
    public Material lineMaterial;
    public Material selectedMaterial;
    public Transform earthTransform;

    private GameObject bordersParent;

    private List<(List<Vector2> coords, LineRenderer lr, string countryName)> borders =
        new List<(List<Vector2>, LineRenderer, string)>();

    public Dictionary<string, CountryData> countries = new Dictionary<string, CountryData>();
    public CountryData selectedCountry;

    void Start()
    {
        bordersParent = new GameObject("BordersParent");

        TextAsset jsonFile = Resources.Load<TextAsset>("world-borders");
        if (jsonFile == null)
        {
            Debug.LogError("world-borders not found in Resources!");
            return;
        }

        string coordPattern = @"\[\s*(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\s*\]";
        string ringPattern = @"\[\s*\[\s*-?\d+\.?\d*\s*,\s*-?\d+\.?\d*\s*\][\s\S]*?\]\s*\]";

        // Split JSON into per-country blocks by "name" field
        string[] countryBlocks = Regex.Split(jsonFile.text, @"(?=""name"")");

        foreach (string block in countryBlocks)
        {
            Match nameMatch = Regex.Match(block, @"""name""\s*:\s*""([^""]*)""");
            string countryName = nameMatch.Success ? nameMatch.Groups[1].Value : "Unknown";

            MatchCollection rings = Regex.Matches(block, ringPattern);
            if (rings.Count == 0) continue;

            if (!countries.ContainsKey(countryName))
                countries[countryName] = new CountryData { name = countryName };

            foreach (Match ring in rings)
            {
                MatchCollection coords = Regex.Matches(ring.Value, coordPattern);
                List<Vector2> geoCoords = new List<Vector2>();

                foreach (Match coord in coords)
                {
                    float lon = float.Parse(coord.Groups[1].Value,
                        System.Globalization.CultureInfo.InvariantCulture);
                    float lat = float.Parse(coord.Groups[2].Value,
                        System.Globalization.CultureInfo.InvariantCulture);
                    geoCoords.Add(new Vector2(lon, lat));
                }

                if (geoCoords.Count < 2) continue;

                GameObject lineObj = new GameObject("Border_" + countryName);
                lineObj.transform.parent = bordersParent.transform;

                LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                lr.positionCount = geoCoords.Count;
                lr.useWorldSpace = false;
                lr.loop = true;
                lr.startWidth = 0.05f;
                lr.endWidth = 0.05f;
                lr.material = lineMaterial;

                countries[countryName].lineRenderers.Add(lr);
                borders.Add((geoCoords, lr, countryName));
            }
        }

        UpdateBorders();
    }

    void Update()
    {
        if (earthTransform != null)
        {
            bordersParent.transform.position = earthTransform.position;
            bordersParent.transform.rotation = earthTransform.rotation;
        }
    }

    public void SelectCountry(string name)
    {
        if (selectedCountry != null)
            foreach (var lr in selectedCountry.lineRenderers)
                lr.material = lineMaterial;

        if (countries.TryGetValue(name, out CountryData country))
        {
            selectedCountry = country;
            foreach (var lr in country.lineRenderers)
                lr.material = selectedMaterial;

            Debug.Log($"Selected: {name}");
        }
    }

    public string FindClosestCountry(Vector3 localPoint)
    {
        float lat = Mathf.Asin(localPoint.y / globeRadius) * Mathf.Rad2Deg;
        float lon = Mathf.Atan2(localPoint.x, localPoint.z) * Mathf.Rad2Deg;

        lon -= longitudeOffset;
        lat -= latitudeOffset;
        if (flipLongitude) lon = -lon;

        string bestCountry = null;
        float bestDist = float.MaxValue;

        foreach (var border in borders)
        {
            foreach (var coord in border.coords)
            {
                float dist = Vector2.Distance(new Vector2(lon, lat), coord);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestCountry = border.countryName;
                }
            }
        }

        return bestCountry;
    }

    public void UpdateBorders()
    {
        foreach (var border in borders)
        {
            List<Vector3> points = new List<Vector3>();
            foreach (var geo in border.coords)
                points.Add(GeoToSphere(geo.x, geo.y));

            border.lr.positionCount = points.Count;
            border.lr.SetPositions(points.ToArray());
        }
    }

    Vector3 GeoToSphere(float lon, float lat)
    {
        if (flipLongitude) lon = -lon;
        lon += longitudeOffset;
        lat += latitudeOffset;

        float latRad = lat * Mathf.Deg2Rad;
        float lonRad = lon * Mathf.Deg2Rad;

        float adjustedLat = Mathf.Atan(Mathf.Tan(latRad) * latScale);
        float radiusXZ = globeRadius * Mathf.Cos(adjustedLat);

        float x = radiusXZ * Mathf.Sin(lonRad);
        float y = globeRadius * Mathf.Sin(adjustedLat);
        float z = radiusXZ * Mathf.Cos(lonRad);

        return new Vector3(x, y, z);
    }

    public void SetLongitudeOffset(float value) { longitudeOffset = value; UpdateBorders(); }
    public void SetLatitudeOffset(float value) { latitudeOffset = value; UpdateBorders(); }
    public void SetFlipLongitude(bool value) { flipLongitude = value; UpdateBorders(); }
    public void SetGlobeRadius(float value) { globeRadius = value; UpdateBorders(); }
    public void SetLatScale(float value) { latScale = value; UpdateBorders(); }
}