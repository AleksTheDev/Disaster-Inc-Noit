using UnityEngine;
using UnityEngine.UI;

public class SlideShow : MonoBehaviour
{
    [Header("Slide Set A (Info Images)")]
    public GameObject[] slidesA;

    [Header("Slide Set B (Source Images)")]
    public GameObject[] slidesB;

    [Header("UI References")]
    public Button nextButton;
    public Button prevButton;

    private int currentIndex = 0;

    void Start()
    {
        if (slidesA.Length == 0 || slidesB.Length == 0) return;

        nextButton.onClick.AddListener(NextSlide);
        prevButton.onClick.AddListener(PrevSlide);

        ShowSlide(currentIndex);
    }

    public void NextSlide()
    {
        currentIndex = (currentIndex + 1) % slidesA.Length;
        ShowSlide(currentIndex);
    }

    public void PrevSlide()
    {
        currentIndex = (currentIndex - 1 + slidesA.Length) % slidesA.Length;
        ShowSlide(currentIndex);
    }

    void ShowSlide(int index)
    {
        for (int i = 0; i < slidesA.Length; i++)
        {
            slidesA[i].SetActive(i == index);
        }

        for (int i = 0; i < slidesB.Length; i++)
        {
            slidesB[i].SetActive(i == index);
        }
    }
}