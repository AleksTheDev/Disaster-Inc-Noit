using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class BordersRenderer : MonoBehaviour
{
    public float globeRadius = 6f;
    public Material lineMaterial;
    public Transform earthTransform;

    private GameObject bordersParent;

    void Start()
    {
        bordersParent = new GameObject("BordersParent");

        TextAsset jsonFile = Resources.Load<TextAsset>("world-borders");
        if (jsonFile == null)
        {
            Debug.LogError("world-borders not found!");
            return;
        }

        string coordPattern = @"\[\s*(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\s*\]";
        string ringPattern = @"\[\s*\[\s*-?\d+\.?\d*\s*,\s*-?\d+\.?\d*\s*\][\s\S]*?\]\s*\]";

        MatchCollection rings = Regex.Matches(jsonFile.text, ringPattern);

        foreach (Match ring in rings)
        {
            MatchCollection coords = Regex.Matches(ring.Value, coordPattern);
            List<Vector3> points = new List<Vector3>();

            foreach (Match coord in coords)
            {
                float lon = float.Parse(coord.Groups[1].Value,
                    System.Globalization.CultureInfo.InvariantCulture);
                float lat = float.Parse(coord.Groups[2].Value,
                    System.Globalization.CultureInfo.InvariantCulture);
                points.Add(GeoToSphere(lon, lat));
            }

            if (points.Count < 2) continue;

            GameObject lineObj = new GameObject("Border");
            lineObj.transform.parent = bordersParent.transform;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = points.Count;
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.material = lineMaterial;
            lr.SetPositions(points.ToArray());
        }
    }

    void Update()
    {
        if (earthTransform != null)
        {
            bordersParent.transform.position = earthTransform.position;
            bordersParent.transform.rotation = earthTransform.rotation;
        }
    }

    Vector3 GeoToSphere(float lon, float lat)
    {
        float latRad = lat * Mathf.Deg2Rad;
        float lonRad = lon * Mathf.Deg2Rad;

        // Y е нагоре, X и Z са хоризонталната равнина
        // Blender използва Z-up, Unity използва Y-up
        float x = globeRadius * Mathf.Cos(latRad) * Mathf.Sin(lonRad);
        float y = globeRadius * Mathf.Sin(latRad);
        float z = globeRadius * Mathf.Cos(latRad) * Mathf.Cos(lonRad);

        return new Vector3(x, y, z);
    }
}