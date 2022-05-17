using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Options : MonoBehaviour
{
    [SerializeField] private Text viewDistanceText;
    [SerializeField] private Slider viewDistanceSlider;

    private void Start()
    {
        if (PlayerPrefs.HasKey("viewDistance"))
        {
            int value = PlayerPrefs.GetInt("viewDistance");
            viewDistanceText.text = value.ToString();
            viewDistanceSlider.value = value;
        }
    }
    
    public void SliderViewDistanceChanged(float value)
    {
        viewDistanceText.text = ((int)value).ToString();
        PlayerPrefs.SetInt("viewDistance", (int)value);
        PlayerPrefs.Save();
    }
}
