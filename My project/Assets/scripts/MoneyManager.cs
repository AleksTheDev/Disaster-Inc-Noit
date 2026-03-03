using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MoneySystem : MonoBehaviour
{
    public static MoneySystem Instance;

    [Header("Money Settings")]
    public float startingMoney = 160000f;
    public float incomePerDay = 160000f;

    [Header("UI References")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI incomeText;

    private float currentMoney;
    private float dayTimer;

    public float CurrentMoney => currentMoney;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentMoney = startingMoney;
        GameTime.Instance.onNewDay.AddListener(OnNewDay);
        UpdateUI();
    }

    void OnNewDay(int day, int month, int year)
    {
        AddMoney(incomePerDay);
    }
    public void AddMoney(float amount)
    {
        currentMoney += amount;
        UpdateUI();
    }

    public bool SpendMoney(float amount)
    {
        if (currentMoney < amount)
        {
            Debug.Log("Недостатъчно средства!");
            return false;
        }
        currentMoney -= amount;
        UpdateUI();
        return true;
    }

    void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = $"${currentMoney:N0}";
        if (incomeText != null)
            incomeText.text = $"${incomePerDay:N0}/ден";
    }
}