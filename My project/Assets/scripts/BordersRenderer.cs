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

    public GameObject bordersParent;

    private List<(List<Vector2> coords, LineRenderer lr, string countryName)> borders =
        new List<(List<Vector2>, LineRenderer, string)>();

    public Dictionary<string, CountryData> countries = new Dictionary<string, CountryData>();
    public CountryData selectedCountry;

    private Dictionary<string, Vector2> countryCenters = new Dictionary<string, Vector2>();

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

        BuildCountryCenters();
        UpdateBorders();
    }

    void BuildCountryCenters()
    {
        Dictionary<string, (float lonSum, float latSum, int count)> sums =
            new Dictionary<string, (float, float, int)>();

        foreach (var border in borders)
        {
            if (!sums.ContainsKey(border.countryName))
                sums[border.countryName] = (0, 0, 0);

            var s = sums[border.countryName];
            foreach (var coord in border.coords)
            {
                s.lonSum += coord.x;
                s.latSum += coord.y;
                s.count++;
            }
            sums[border.countryName] = s;
        }

        foreach (var kvp in sums)
        {
            if (kvp.Value.count < 50) continue;

            countryCenters[kvp.Key] = new Vector2(
                kvp.Value.lonSum / kvp.Value.count,
                kvp.Value.latSum / kvp.Value.count
            );
        }
    }

    public bool TryGetCountryCenter(string country, out Vector2 center)
    {
        return countryCenters.TryGetValue(country, out center);
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
            {
                lr.material = lineMaterial;
                lr.startWidth = 0.05f;
                lr.endWidth = 0.05f;
            }

        if (countries.TryGetValue(name, out CountryData country))
        {
            selectedCountry = country;
            foreach (var lr in country.lineRenderers)
            {
                lr.material = selectedMaterial;
                lr.startWidth = 0.08f;
                lr.endWidth = 0.08f;
            }
        }
    }

    public string FindClosestCountry(Vector3 localPoint)
    {
        float lat = Mathf.Asin(Mathf.Clamp(localPoint.y / globeRadius, -1f, 1f)) * Mathf.Rad2Deg;
        float lon = Mathf.Atan2(localPoint.x, localPoint.z) * Mathf.Rad2Deg;

        lon -= longitudeOffset;
        lat -= latitudeOffset;
        if (flipLongitude) lon = -lon;

        string bestCountry = null;
        float bestDist = float.MaxValue;

        foreach (var kvp in countryCenters)
        {
            float centerLon = kvp.Value.x;
            float centerLat = kvp.Value.y;

            float dLat = (centerLat - lat) * Mathf.Deg2Rad;
            float dLon = (centerLon - lon) * Mathf.Deg2Rad;

            float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                      Mathf.Cos(lat * Mathf.Deg2Rad) * Mathf.Cos(centerLat * Mathf.Deg2Rad) *
                      Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);

            float dist = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

            if (dist < bestDist)
            {
                bestDist = dist;
                bestCountry = kvp.Key;
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

    public Vector3 GeoToSpherePublic(float lon, float lat)
    {
        return GeoToSphere(lon, lat);
    }

    public void SetLongitudeOffset(float value) { longitudeOffset = value; UpdateBorders(); }
    public void SetLatitudeOffset(float value) { latitudeOffset = value; UpdateBorders(); }
    public void SetFlipLongitude(bool value) { flipLongitude = value; UpdateBorders(); }
    public void SetGlobeRadius(float value) { globeRadius = value; UpdateBorders(); }
    public void SetLatScale(float value) { latScale = value; UpdateBorders(); }
}