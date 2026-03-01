using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class BordersRenderer : MonoBehaviour
{
    [Header("Globe Settings")]
    public float globeRadius = 6f; // Radius of the sphere

    [Header("Map Orientation")]
    public bool flipLongitude = false; // Flip east/west
    [Range(-180f, 180f)]
    public float longitudeOffset = 0f;  // Rotate map horizontally
    [Range(-90f, 90f)]
    public float latitudeOffset = 0f;   // Tilt map vertically

    [Header("Map Shape")]
    [Range(0.5f, 1.5f)]
    public float latScale = 1f; // Compress/stretch the poles

    [Header("Rendering")]
    public Material lineMaterial;   // Material for borders
    public Transform earthTransform; // Optional parent transform

    private GameObject bordersParent;

    // Store the geographic coordinates and LineRenderer for dynamic updates
    private List<(List<Vector2> coords, LineRenderer lr)> borders =
        new List<(List<Vector2>, LineRenderer)>();

    void Start()
    {
        bordersParent = new GameObject("BordersParent");

        TextAsset jsonFile = Resources.Load<TextAsset>("world-borders");
        if (jsonFile == null)
        {
            Debug.LogError("world-borders not found in Resources!");
            return;
        }

        // Regex patterns for extracting coordinates
        string coordPattern = @"\[\s*(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\s*\]";
        string ringPattern = @"\[\s*\[\s*-?\d+\.?\d*\s*,\s*-?\d+\.?\d*\s*\][\s\S]*?\]\s*\]";

        MatchCollection rings = Regex.Matches(jsonFile.text, ringPattern);

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

            // Create LineRenderer
            GameObject lineObj = new GameObject("Border");
            lineObj.transform.parent = bordersParent.transform;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = geoCoords.Count;
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.material = lineMaterial;

            borders.Add((geoCoords, lr));
        }

        // Initial update
        UpdateBorders();
    }

    void Update()
    {
        // Match parent transform if assigned
        if (earthTransform != null)
        {
            bordersParent.transform.position = earthTransform.position;
            bordersParent.transform.rotation = earthTransform.rotation;
        }
    }

    /// <summary>
    /// Convert geographic coordinates to 3D sphere coordinates
    /// </summary>
    Vector3 GeoToSphere(float lon, float lat)
{
    if (flipLongitude)
        lon = -lon;

    lon += longitudeOffset;
    lat += latitudeOffset;

    float latRad = lat * Mathf.Deg2Rad;
    float lonRad = lon * Mathf.Deg2Rad;

    // Nonlinear adjustment to keep circular sphere while compressing poles
    float adjustedLat = Mathf.Atan(Mathf.Tan(latRad) * latScale);

    float radiusXZ = globeRadius * Mathf.Cos(adjustedLat);
    float x = radiusXZ * Mathf.Sin(lonRad);
    float y = globeRadius * Mathf.Sin(adjustedLat);
    float z = radiusXZ * Mathf.Cos(lonRad);

    return new Vector3(x, y, z);
}

    /// <summary>
    /// Recalculate border positions with current offsets and scaling
    /// </summary>
    public void UpdateBorders()
    {
        foreach (var border in borders)
        {
            List<Vector3> points = new List<Vector3>();
            foreach (var geo in border.coords)
            {
                points.Add(GeoToSphere(geo.x, geo.y));
            }

            border.lr.positionCount = points.Count;
            border.lr.SetPositions(points.ToArray());
        }
    }

    // UI-friendly setters for sliders/buttons
    public void SetLongitudeOffset(float value)
    {
        longitudeOffset = value;
        UpdateBorders();
    }

    public void SetLatitudeOffset(float value)
    {
        latitudeOffset = value;
        UpdateBorders();
    }

    public void SetFlipLongitude(bool value)
    {
        flipLongitude = value;
        UpdateBorders();
    }

    public void SetGlobeRadius(float value)
    {
        globeRadius = value;
        UpdateBorders();
    }

    public void SetLatScale(float value)
    {
        latScale = value;
        UpdateBorders();
    }
}