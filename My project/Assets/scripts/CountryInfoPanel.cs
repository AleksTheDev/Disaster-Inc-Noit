using UnityEngine;
using UnityEngine.UI;

public class CountryInfoPanel : MonoBehaviour
{
    public GameObject Panel;
    public Text CountryNameText;
    public Text PopulationText;
    void Start()
    {
        Panel.SetActive(false);
    }

    public void Show(Country country)
    {
        Panel.SetActive(true);

        if (country != null)
        {
            CountryNameText.text = country.name;
            PopulationText.text = "Population: " + country.population.ToString("N0");
        }
        else
        {
            CountryNameText.text = "Unknown";
            PopulationText.text = "No data available";
        }
    }
}