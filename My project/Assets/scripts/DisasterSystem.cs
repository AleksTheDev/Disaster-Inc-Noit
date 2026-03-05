using UnityEngine;
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

    [Header("Win Condition")]
    public int disastersToWin = 10;
    public GameObject winPanel;

    [Header("Pin Sprites")]
    public Sprite earthquakeSprite;
    public Sprite landslideSprite;
    public Sprite volcanoSprite;

    [Header("Infrastructure Sprites")]
    public Sprite lavaBarrierSprite;
    public Sprite landslideBarrierSprite;
    public Sprite reinforcedBuildingSprite;
    public Sprite seismicMonitorSprite;
    public Sprite nextEarthquakeSprite;

    [Header("UI")]
    public TextMeshProUGUI casualtiesText;
    public TextMeshProUGUI disasterNameText;
    public TextMeshProUGUI disastersSolvedText;

    public List<ActiveDisaster> activeDisasters = new List<ActiveDisaster>();
    public ActiveDisaster selectedDisaster;

    private int totalCasualties = 0;
    private int disastersSolved = 0;
    private float spawnTimer = 0f;
    private float nextSpawnTime;

    // Infrastructure tracking
    private Dictionary<string, List<GameObject>> infrastructurePins =
        new Dictionary<string, List<GameObject>>();

    // Seismic monitor prediction
    private string predictedEarthquakeCountry = null;
    private GameObject predictionPin = null;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
        if (winPanel != null) winPanel.SetActive(false);
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
        UpdateUI();
    }

    void SpawnRandomDisaster()
    {
        var countryList = new List<string>(bordersRenderer.countries.Keys);
        if (countryList.Count == 0) return;

        // If seismic monitor is active, spawn earthquake in predicted country
        string randomCountry;
        DisasterType type;

        if (predictedEarthquakeCountry != null)
        {
            randomCountry = predictedEarthquakeCountry;
            type = DisasterType.Earthquake;
            predictedEarthquakeCountry = null;
            if (predictionPin != null)
            {
                Destroy(predictionPin);
                predictionPin = null;
            }
        }
        else
        {
            randomCountry = countryList[Random.Range(0, countryList.Count)];
            type = (DisasterType)Random.Range(0, 3);
        }

        if (!bordersRenderer.TryGetCountryCenter(randomCountry, out Vector2 center))
            return;

        // Reduce casualties if infrastructure exists
        int casualties = baseCasualtiesPerSecond;
        if (infrastructurePins.ContainsKey(randomCountry))
            casualties = Mathf.RoundToInt(casualties * 0.3f);

        ActiveDisaster disaster = new ActiveDisaster
        {
            countryName = randomCountry,
            type = type,
            timeRemaining = disasterDuration,
            totalTime = disasterDuration,
            casualties = 0,
            casualtiesPerSecond = casualties,
            isResolved = false
        };

        disaster.pinObject = CreatePin(randomCountry, type);
        activeDisasters.Add(disaster);
    }

    GameObject CreatePin(string countryName, DisasterType type, Sprite overrideSprite = null)
    {
        if (!bordersRenderer.TryGetCountryCenter(countryName, out Vector2 center))
            return null;

        Vector3 localPos = bordersRenderer.GeoToSpherePublic(center.x, center.y);
        Vector3 worldPos = earthTransform.TransformPoint(localPos * 1.05f);

        GameObject pin = new GameObject("Pin_" + countryName);
        pin.transform.position = worldPos;
        pin.transform.parent = earthTransform;
        pin.transform.localScale = Vector3.one * 0.05f;

        SpriteRenderer sr = pin.AddComponent<SpriteRenderer>();
        sr.sprite = overrideSprite ?? type switch
        {
            DisasterType.Earthquake => earthquakeSprite,
            DisasterType.Landslide => landslideSprite,
            DisasterType.Volcano => volcanoSprite,
            _ => earthquakeSprite
        };
        sr.sortingOrder = 100;

        PinBillboard billboard = pin.AddComponent<PinBillboard>();
        billboard.mainCamera = mainCamera;
        billboard.earthTransform = earthTransform;
        billboard.pinDistance = bordersRenderer.globeRadius + 0.1f;

        return pin;
    }

    void AddInfrastructurePin(string countryName, Sprite sprite)
    {
        if (!bordersRenderer.TryGetCountryCenter(countryName, out Vector2 center))
            return;

        Vector3 localPos = bordersRenderer.GeoToSpherePublic(center.x, center.y);
        // Offset slightly so pins don't overlap
        Vector3 worldPos = earthTransform.TransformPoint(localPos * 1.06f);

        GameObject pin = new GameObject("Infra_" + countryName);
        pin.transform.position = worldPos;
        pin.transform.parent = earthTransform;
        pin.transform.localScale = Vector3.one * 0.04f;

        SpriteRenderer sr = pin.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 110;

        PinBillboard billboard = pin.AddComponent<PinBillboard>();
        billboard.mainCamera = mainCamera;
        billboard.earthTransform = earthTransform;
        billboard.pinDistance = bordersRenderer.globeRadius + 0.15f;

        if (!infrastructurePins.ContainsKey(countryName))
            infrastructurePins[countryName] = new List<GameObject>();

        infrastructurePins[countryName].Add(pin);
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
                ResolveDisaster(disaster);
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

    // 1. ЕВАКУАЦИЯ - работи за всички бедствия
    public void Evacuate()
    {
        if (selectedDisaster == null) { Debug.Log("Няма избрано бедствие!"); return; }

        ResolveDisaster(selectedDisaster);
        selectedDisaster.isResolved = true;
        selectedDisaster = null;

        disastersSolved++;
        CheckWinCondition();

        if (disasterNameText != null) disasterNameText.text = "";
    }

    // 2. ЛАВА БАРИЕРИ - само за вулкани, оставя постоянна иконка
    public void BuildLavaBarrier()
    {
        if (selectedDisaster == null) { Debug.Log("Няма избрано бедствие!"); return; }
        if (selectedDisaster.type != DisasterType.Volcano)
        {
            Debug.Log("Лава бариерите работят само за вулкани!");
            return;
        }

        AddInfrastructurePin(selectedDisaster.countryName, lavaBarrierSprite);
        ResolveDisaster(selectedDisaster);
        selectedDisaster.isResolved = true;
        selectedDisaster = null;

        disastersSolved++;
        CheckWinCondition();

        if (disasterNameText != null) disasterNameText.text = "";
    }

    // 3. БАРИЕРИ ОТ СВЛАЧИЩА - само за свлачища, оставя постоянна иконка
    public void BuildLandslideBarrier()
    {
        if (selectedDisaster == null) { Debug.Log("Няма избрано бедствие!"); return; }
        if (selectedDisaster.type != DisasterType.Landslide)
        {
            Debug.Log("Бариерите работят само за свлачища!");
            return;
        }

        AddInfrastructurePin(selectedDisaster.countryName, landslideBarrierSprite);
        ResolveDisaster(selectedDisaster);
        selectedDisaster.isResolved = true;
        selectedDisaster = null;

        disastersSolved++;
        CheckWinCondition();

        if (disasterNameText != null) disasterNameText.text = "";
    }

    // 4. УКРЕПВАНЕ НА СГРАДИ - само за земетресения, оставя постоянна иконка
    public void ReinforceBuildings()
    {
        if (selectedDisaster == null) { Debug.Log("Няма избрано бедствие!"); return; }
        if (selectedDisaster.type != DisasterType.Earthquake)
        {
            Debug.Log("Укрепването работи само за земетресения!");
            return;
        }

        AddInfrastructurePin(selectedDisaster.countryName, reinforcedBuildingSprite);
        ResolveDisaster(selectedDisaster);
        selectedDisaster.isResolved = true;
        selectedDisaster = null;

        disastersSolved++;
        CheckWinCondition();

        if (disasterNameText != null) disasterNameText.text = "";
    }

    // 5. ТЕКТОНСКИ МОНИТОРИНГ - предсказва следващото земетресение
    public void SeismicMonitor()
    {
        var countryList = new List<string>(bordersRenderer.countries.Keys);
        if (countryList.Count == 0) return;

        // Pick random country for next earthquake
        predictedEarthquakeCountry = countryList[Random.Range(0, countryList.Count)];

        // Remove old prediction pin
        if (predictionPin != null) Destroy(predictionPin);

        // Show prediction pin
        predictionPin = CreatePin(predictedEarthquakeCountry,
            DisasterType.Earthquake, seismicMonitorSprite);

        Debug.Log($"Следващо земетресение: {predictedEarthquakeCountry}");

        if (disasterNameText != null)
            disasterNameText.text = $"Предсказано: {predictedEarthquakeCountry}";
    }

    void CheckWinCondition()
    {
        if (disastersSolved >= disastersToWin)
        {
            Debug.Log("ПОБЕДА!");
            if (winPanel != null) winPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    void ResolveDisaster(ActiveDisaster disaster)
    {
        if (disaster.pinObject != null)
            Destroy(disaster.pinObject);
    }

    void UpdateUI()
    {
        if (casualtiesText != null)
            casualtiesText.text = $"{totalCasualties:N0}";

        if (disastersSolvedText != null)
            disastersSolvedText.text = $"{disastersSolved}/{disastersToWin}";
    }

    public void ApplyMeasure(float effectiveness)
    {
        Evacuate();
    }
}