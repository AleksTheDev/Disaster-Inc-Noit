using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
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
    public GameObject disasterPanel;
    public TextMeshProUGUI disasterNameText;
    public Transform pinParent;

    public List<ActiveDisaster> activeDisasters = new List<ActiveDisaster>();
    private ActiveDisaster selectedDisaster;
    private int totalCasualties = 0;
    private float funds = 160000f;
    private float spawnTimer;
    private float nextSpawnTime;

    // Country lon/lat cache
    private Dictionary<string, Vector2> countryCenters = new Dictionary<string, Vector2>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
        spawnTimer = 0f;

        BuildCountryCenters();
        UpdateUI();
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
        UpdatePinPositions();
        UpdateUI();
    }

    void BuildCountryCenters()
    {
        // Calculate center point for each country from border coords
        foreach (var kvp in bordersRenderer.countries)
        {
            float lonSum = 0, latSum = 0;
            int count = 0;

            foreach (var lr in kvp.Value.lineRenderers)
            {
                // Use positions already on globe
            }

            // Use first polygon center from borders
            countryCenters[kvp.Key] = Vector2.zero;
        }

        // Better: parse centers directly from border data
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

        // Create pin
        disaster.pinObject = CreatePin(randomCountry, type);
        activeDisasters.Add(disaster);

        Debug.Log($"Disaster spawned: {type} in {randomCountry}");
    }

    GameObject CreatePin(string countryName, DisasterType type)
    {
        if (!countryCenters.ContainsKey(countryName)) return null;

        GameObject pin = new GameObject("Pin_" + countryName);
        pin.transform.parent = pinParent != null ? pinParent : earthTransform;

        SpriteRenderer sr = pin.AddComponent<SpriteRenderer>();
        sr.sprite = type switch
        {
            DisasterType.Earthquake => earthquakeSprite,
            DisasterType.Landslide => landslideSprite,
            DisasterType.Volcano => volcanoSprite,
            _ => earthquakeSprite
        };
        sr.sortingOrder = 10;

        Vector2 center = countryCenters[countryName];
        pin.transform.localPosition = bordersRenderer.GeoToSpherePublic(center.x, center.y) * 1.05f;
        pin.transform.localScale = Vector3.one * 0.3f;

        return pin;
    }

    void UpdatePinPositions()
    {
        foreach (var disaster in activeDisasters)
        {
            if (disaster.pinObject == null) continue;

            // Make pin face camera
            disaster.pinObject.transform.LookAt(mainCamera.transform);
            disaster.pinObject.transform.Rotate(0, 180f, 0);
        }
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
            disaster.casualties += Mathf.RoundToInt(disaster.casualtiesPerSecond * Time.deltaTime);
            totalCasualties += Mathf.RoundToInt(disaster.casualtiesPerSecond * Time.deltaTime);

            if (disaster.timeRemaining <= 0)
            {
                // Time ran out - disaster unresolved
                ResolveDisaster(disaster, false);
                toRemove.Add(disaster);
            }
        }

        foreach (var d in toRemove)
            activeDisasters.Remove(d);
    }

    public void OnCountryClicked(string countryName)
    {
        // Find active disaster in this country
        ActiveDisaster disaster = activeDisasters.Find(d => d.countryName == countryName);

        if (disaster != null)
        {
            selectedDisaster = disaster;
            ShowDisasterPanel(disaster);
        }
        else
        {
            HideDisasterPanel();
        }
    }

    void ShowDisasterPanel(ActiveDisaster disaster)
    {
        if (disasterPanel == null) return;
        disasterPanel.SetActive(true);

        if (disasterNameText != null)
            disasterNameText.text = $"{disaster.type} - {disaster.countryName}";
    }

    void HideDisasterPanel()
    {
        if (disasterPanel != null)
            disasterPanel.SetActive(false);
        selectedDisaster = null;
    }
    
    public void ApplyMeasure(float effectiveness)
    {
        if (selectedDisaster == null) return;

        ResolveDisaster(selectedDisaster, true);
        selectedDisaster.isResolved = true;
        
        float timeBonus = selectedDisaster.timeRemaining / selectedDisaster.totalTime;
        float reward = 50000f * effectiveness * (1f + timeBonus);
        funds += reward;

        HideDisasterPanel();
        Debug.Log($"Disaster resolved! Reward: {reward:F0}");
    }

    void ResolveDisaster(ActiveDisaster disaster, bool success)
    {
        if (disaster.pinObject != null)
            Destroy(disaster.pinObject);

        if (success)
            Debug.Log($"Successfully resolved {disaster.type} in {disaster.countryName}");
        else
            Debug.Log($"Failed to resolve {disaster.type} in {disaster.countryName}! Casualties: {disaster.casualties}");
    }

    void UpdateUI()
    {
        if (casualtiesText != null)
            casualtiesText.text = $"{totalCasualties:N0}";

        if (fundsText != null)
            fundsText.text = $"${funds:N0}";

        // Update selected disaster panel
        if (selectedDisaster != null && !selectedDisaster.isResolved)
            ShowDisasterPanel(selectedDisaster);
    }
}