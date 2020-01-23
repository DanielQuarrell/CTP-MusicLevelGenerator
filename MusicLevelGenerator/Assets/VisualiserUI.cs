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

    [SerializeField] Button updateSampleSize;
    [SerializeField] Button updateWindowSize;

    [Header("Frequency Bands")]
    [SerializeField] Dropdown BandSelector;
    [SerializeField] InputField upperBand;
    [SerializeField] InputField lowerBand;
    [SerializeField] Slider threshold;
    [SerializeField] Text thresholdValue;

    public void UpdateSampleSizePressed()
    {
        
    }

    public void UpdateThresholdWindowPressed()
    {

    }

    public void FrequencyBandDropdownChanged()
    {

    }

    public void EditUpperBoundary()
    {

    }

    public void EditLowerBoundary()
    {

    }

    public void UpdateVisualiser()
    {

    }
}
