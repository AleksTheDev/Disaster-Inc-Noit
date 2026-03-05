using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MeasureButton : MonoBehaviour
{
    public enum MeasureAction
    {
        Evacuate,
        BuildLavaBarrier,
        BuildLandslideBarrier,
        ReinforceBuildings,
        SeismicMonitor
    }

    [Header("Measure Settings")]
    public MeasureAction actionType;
    public float cost = 500000f;
    public float cooldown = 20f;

    [Header("UI")]
    public Image cooldownOverlay;
    public TextMeshProUGUI cooldownText;

    private float cooldownTimer = 0f;
    private bool onCooldown = false;
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnClick);

        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = 0f;
    }

    void Update()
    {
        if (!onCooldown) return;

        cooldownTimer -= Time.deltaTime;

        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = cooldownTimer / cooldown;

        if (cooldownText != null)
            cooldownText.text = $"{cooldownTimer:F0}s";

        if (cooldownTimer <= 0f)
        {
            onCooldown = false;
            cooldownTimer = 0f;

            if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
            if (cooldownText != null) cooldownText.text = "";
            if (button != null) button.interactable = true;
        }
    }

    public void OnClick()
    {
        if (onCooldown) return;
        if (DisasterSystem.Instance == null) return;

        if (actionType != MeasureAction.SeismicMonitor &&
            DisasterSystem.Instance.selectedDisaster == null)
        {
            Debug.Log("Няма избрано бедствие!");
            return;
        }

        if (MoneySystem.Instance != null && !MoneySystem.Instance.SpendMoney(cost))
        {
            Debug.Log("Недостатъчно средства!");
            return;
        }

        switch (actionType)
        {
            case MeasureAction.Evacuate:
                DisasterSystem.Instance.Evacuate();
                break;
            case MeasureAction.BuildLavaBarrier:
                DisasterSystem.Instance.BuildLavaBarrier();
                break;
            case MeasureAction.BuildLandslideBarrier:
                DisasterSystem.Instance.BuildLandslideBarrier();
                break;
            case MeasureAction.ReinforceBuildings:
                DisasterSystem.Instance.ReinforceBuildings();
                break;
            case MeasureAction.SeismicMonitor:
                DisasterSystem.Instance.SeismicMonitor();
                break;
        }

        onCooldown = true;
        cooldownTimer = cooldown;
        if (button != null) button.interactable = false;
    }
}