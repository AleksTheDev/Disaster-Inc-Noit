using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class GameTime : MonoBehaviour
{
    public static GameTime Instance;

    [Header("Time Settings")]
    public float secondsPerDay = 1f;

    [Header("Starting Date")]
    public int startDay = 1;
    public int startMonth = 1;
    public int startYear = 2025;

    [Header("UI")]
    public TextMeshProUGUI dateText;

    private int currentDay;
    private int currentMonth;
    private int currentYear;
    private float timer;

    private int[] daysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    public UnityEvent<int, int, int> onNewDay;

    public int Day => currentDay;
    public int Month => currentMonth;
    public int Year => currentYear;
    public string DateString => $"{currentDay:00}/{currentMonth:00}/{currentYear}";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentDay = startDay;
        currentMonth = startMonth;
        currentYear = startYear;
        timer = 0f;
        UpdateDateUI();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= secondsPerDay)
        {
            timer = 0f;
            AdvanceDay();
        }

        UpdateDateUI();
    }

    void AdvanceDay()
    {
        currentDay++;

        int maxDays = daysInMonth[currentMonth - 1];

        if (currentMonth == 2 && IsLeapYear(currentYear))
            maxDays = 29;

        if (currentDay > maxDays)
        {
            currentDay = 1;
            currentMonth++;

            if (currentMonth > 12)
            {
                currentMonth = 1;
                currentYear++;
            }
        }

        onNewDay?.Invoke(currentDay, currentMonth, currentYear);
    }

    void UpdateDateUI()
    {
        if (dateText != null)
            dateText.text = DateString;
    }

    bool IsLeapYear(int year)
    {
        return (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
    }

    public void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
    }
}