using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VisualiserUI : MonoBehaviour
{
    SongController songController;

    [Header("Song Controller")]
    [SerializeField] InputField sampleSize;
    [SerializeField] InputField windowSize;
    [SerializeField] Text time;

    [Header("Frequency Bands")]
    [SerializeField] Dropdown bandSelector;
    [SerializeField] InputField upperBand;
    [SerializeField] InputField lowerBand;
    [SerializeField] InputField thresholdInput;

    private void Awake()
    {
        songController = SongController.instance;

        sampleSize.text = songController.spectrumSampleSize.ToString();
        windowSize.text = songController.thresholdWindowSize.ToString();

        bandSelector.ClearOptions();

        List<string> options = new List<string>();

        options.Add("Select Band");

        for (int i = 0; i < songController.frequencyBandBoundaries.Count; i++)
        {
            options.Add("Frequency Band " + (i+1));
        }

        bandSelector.AddOptions(options);
    }

    private void Update()
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(songController.GetSongTime());
        time.text = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
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

            thresholdInput.text = songController.frequencyBandBoundaries[frequencyBandIndex].thresholdMultiplier.ToString();
            thresholdInput.interactable = true;
        }
        else
        {
            upperBand.text = string.Empty;
            upperBand.interactable = false;
            
            lowerBand.text = string.Empty;
            lowerBand.interactable = false;

            thresholdInput.text = string.Empty;
            thresholdInput.interactable = false;
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

    public void EditThresholdValue()
    {
        int frequencyBandIndex = bandSelector.value;

        if (frequencyBandIndex != 0)
        {
            frequencyBandIndex--;
            songController.frequencyBandBoundaries[frequencyBandIndex].thresholdMultiplier = float.Parse(thresholdInput.text);
        }
    }

    public void UpdateVisualiser()
    {
        songController.ReprocessSong();
    }

    public void SaveOnsetsToFile()
    {
        songController.SaveToFile();
    }
}
