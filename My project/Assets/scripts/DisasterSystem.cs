using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public enum DisasterType { Earthquake, Landslide, Volcano }

public class ActiveDisaster
{
    public string countryName;
    public DisasterType type;
    public float timeRemaining;
    public float totalTime;
    public int casualties;
    public int casualtiesPerSecond;
    public GameObject pinObject;
    public bool isResolved;
}

public class DisasterSystem : MonoBehaviour
{
    public static DisasterSystem Instance;

    [Header("References")]
    public BordersRenderer bordersRenderer;
    public Camera mainCamera;
    public Transform earthTransform;

    [Header("Disaster Settings")]
    public float minSpawnInterval = 10f;
    public float maxSpawnInterval = 30f;
    public float disasterDuration = 30f;
    public int baseCasualtiesPerSecond = 100;

    [Header("Pin Prefabs")]
    public Sprite earthquakeSprite;
    public Sprite landslideSprite;
    public Sprite volcanoSprite;

    [Header("UI")]
    public TextMeshProUGUI casualtiesText;
    public TextMeshProUGUI fundsText;
    public TextMeshProUGUI disasterNameText;

    public List<ActiveDisaster> activeDisasters = new List<ActiveDisaster>();
    public ActiveDisaster selectedDisaster;

    private int totalCasualties = 0;
    private float funds = 160000f;
    private float spawnTimer = 0f;
    private float nextSpawnTime;

    private Dictionary<string, Vector2> countryCenters = new Dictionary<string, Vector2>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
        UpdateUI();
        StartCoroutine(InitAfterBorders());
    }

    IEnumerator InitAfterBorders()
    {
        yield return new WaitForSeconds(1f);
        BuildCountryCenters();
        Debug.Log($"Built centers for {countryCenters.Count} countries");
        SpawnRandomDisaster();
        SpawnRandomDisaster();
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= nextSpawnTime)
        {
            spawnTimer = 0f;
            nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            SpawnRandomDisaster();
        }

        UpdateDisasters();
        UpdateUI();
    }

    void BuildCountryCenters()
    {
        var borders = bordersRenderer.GetBorderData();
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
            countryCenters[kvp.Key] = new Vector2(
                kvp.Value.lonSum / kvp.Value.count,
                kvp.Value.latSum / kvp.Value.count
            );
        }
    }

    void SpawnRandomDisaster()
    {
        var countryList = new List<string>(bordersRenderer.countries.Keys);
        if (countryList.Count == 0) return;

        string randomCountry = countryList[Random.Range(0, countryList.Count)];
        DisasterType type = (DisasterType)Random.Range(0, 3);

        ActiveDisaster disaster = new ActiveDisaster
        {
            countryName = randomCountry,
            type = type,
            timeRemaining = disasterDuration,
            totalTime = disasterDuration,
            casualties = 0,
            casualtiesPerSecond = baseCasualtiesPerSecond,
            isResolved = false
        };

        disaster.pinObject = CreatePin(randomCountry, type);
        activeDisasters.Add(disaster);

        Debug.Log($"Spawned {type} in {randomCountry}");
    }

    GameObject CreatePin(string countryName, DisasterType type)
    {
        if (!countryCenters.ContainsKey(countryName))
        {
            Debug.LogWarning($"No center found for {countryName}");
            return null;
        }

        Vector2 center = countryCenters[countryName];
        Vector3 localPos = bordersRenderer.GeoToSpherePublic(center.x, center.y);
        Vector3 worldPos = earthTransform.TransformPoint(localPos * 1.05f);

        GameObject pin = new GameObject("Pin_" + countryName);
        pin.transform.position = worldPos;
        pin.transform.parent = earthTransform;
        pin.transform.localScale = Vector3.one * 0.3f;

        SpriteRenderer sr = pin.AddComponent<SpriteRenderer>();
        sr.sprite = type switch
        {
            DisasterType.Earthquake => earthquakeSprite,
            DisasterType.Landslide => landslideSprite,
            DisasterType.Volcano => volcanoSprite,
            _ => earthquakeSprite
        };
        sr.sortingOrder = 100;
        sr.sortingLayerName = "Default";

        PinBillboard billboard = pin.AddComponent<PinBillboard>();
        billboard.mainCamera = mainCamera;
        billboard.earthTransform = earthTransform;
        billboard.pinDistance = bordersRenderer.globeRadius + 0.1f;

        return pin;
    }

    void UpdateDisasters()
    {
        List<ActiveDisaster> toRemove = new List<ActiveDisaster>();

        foreach (var disaster in activeDisasters)
        {
            if (disaster.isResolved)
            {
                toRemove.Add(disaster);
                continue;
            }

            disaster.timeRemaining -= Time.deltaTime;
            int newCasualties = Mathf.RoundToInt(disaster.casualtiesPerSecond * Time.deltaTime);
            disaster.casualties += newCasualties;
            totalCasualties += newCasualties;

            if (disaster.timeRemaining <= 0)
            {
                ResolveDisaster(disaster, false);
                toRemove.Add(disaster);
            }
        }

        foreach (var d in toRemove)
            activeDisasters.Remove(d);
    }

    public void OnCountryClicked(string countryName)
    {
        ActiveDisaster disaster = activeDisasters.Find(d => d.countryName == countryName);

        if (disaster != null)
        {
            selectedDisaster = disaster;
            if (disasterNameText != null)
                disasterNameText.text = $"{disaster.type} - {disaster.countryName}";
        }
        else
        {
            selectedDisaster = null;
            if (disasterNameText != null)
                disasterNameText.text = "";
        }
    }

    public void ApplyMeasure(float effectiveness)
    {
        if (selectedDisaster == null)
        {
            Debug.Log("Няма избрано бедствие!");
            return;
        }

        float timeBonus = selectedDisaster.timeRemaining / selectedDisaster.totalTime;
        float reward = 50000f * effectiveness * (1f + timeBonus);
        funds += reward;

        ResolveDisaster(selectedDisaster, true);
        selectedDisaster.isResolved = true;
        selectedDisaster = null;

        if (disasterNameText != null)
            disasterNameText.text = "";
    }

    void ResolveDisaster(ActiveDisaster disaster, bool success)
    {
        if (disaster.pinObject != null)
            Destroy(disaster.pinObject);

        Debug.Log(success
            ? $"Resolved {disaster.type} in {disaster.countryName}!"
            : $"Failed {disaster.type} in {disaster.countryName}! Casualties: {disaster.casualties}");
    }

    void UpdateUI()
    {
        if (casualtiesText != null)
            casualtiesText.text = $"{totalCasualties:N0}";

        if (fundsText != null)
            fundsText.text = $"${funds:N0}";
    }
}