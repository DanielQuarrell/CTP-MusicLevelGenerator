using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VisualiserUI : MonoBehaviour
{
    [SerializeField] SongController songController;

    [Header("Song Controller")]
    [SerializeField] InputField sampleSize;
    [SerializeField] InputField windowSize;

    [Header("Frequency Bands")]
    [SerializeField] Dropdown bandSelector;
    [SerializeField] InputField upperBand;
    [SerializeField] InputField lowerBand;
    [SerializeField] Slider thresholdSlider;
    [SerializeField] Text thresholdValue;

    private void Awake()
    {
        sampleSize.text = songController.spectrumSampleSize.ToString();
        windowSize.text = songController.thresholdWindowSize.ToString();

        bandSelector.ClearOptions();

        List<string> options = new List<string>();

        options.Add("Select Band");

        for (int i = 0; i < songController.frequencyBandBoundaries.Length; i++)
        {
            options.Add("Frequency Band " + (i+1));
        }

        bandSelector.AddOptions(options);
    }

    public void UpdateSampleSizePressed()
    {
        songController.spectrumSampleSize = int.Parse(sampleSize.text);
    }

    public void UpdateThresholdWindowPressed()
    {
        songController.thresholdWindowSize = int.Parse(sampleSize.text);
    }

    public void FrequencyBandDropdownChanged()
    {
        int frequencyBandIndex = bandSelector.value;

        if (frequencyBandIndex != 0)
        {
            frequencyBandIndex--; 

            upperBand.text = songController.frequencyBandBoundaries[frequencyBandIndex].upperBoundary.ToString();
            upperBand.interactable = true;

            lowerBand.text = songController.frequencyBandBoundaries[frequencyBandIndex].lowerBoundary.ToString();
            lowerBand.interactable = true;

            thresholdSlider.value = songController.frequencyBandBoundaries[frequencyBandIndex].thresholdMultiplier;
            thresholdSlider.interactable = true;

            thresholdValue.text = songController.frequencyBandBoundaries[frequencyBandIndex].thresholdMultiplier.ToString();
        }
        else
        {
            upperBand.text = string.Empty;
            upperBand.interactable = false;
            
            lowerBand.text = string.Empty;
            lowerBand.interactable = false;

            thresholdSlider.value = 0;
            thresholdSlider.interactable = false;

            thresholdValue.text = "0";
        }
    }

    public void EditUpperBoundary()
    {
        int frequencyBandIndex = bandSelector.value;

        if (frequencyBandIndex != 0)
        {
            frequencyBandIndex--;
            songController.frequencyBandBoundaries[frequencyBandIndex].upperBoundary = int.Parse(upperBand.text);
        }
    }

    public void EditLowerBoundary()
    {
        int frequencyBandIndex = bandSelector.value;

        if(frequencyBandIndex != 0)
        {
            frequencyBandIndex--;
            songController.frequencyBandBoundaries[frequencyBandIndex].lowerBoundary = int.Parse(lowerBand.text);
        }
    }

    public void ThresholdSliderChanged()
    {
        float thresholdMultiplier = Mathf.Lerp(0, 3, thresholdSlider.value);
        thresholdValue.text = thresholdMultiplier.ToString();

        int frequencyBandIndex = bandSelector.value; 

        if (frequencyBandIndex != 0)
        {
            frequencyBandIndex--;
            songController.frequencyBandBoundaries[frequencyBandIndex].thresholdMultiplier = thresholdMultiplier;
        }
    }

    public void UpdateVisualiser()
    {
        songController.ReprocessSong();
    }
}
