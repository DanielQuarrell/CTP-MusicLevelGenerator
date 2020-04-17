using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpectrumData
{
    public float[] spectrum;

    public SpectrumData(float [] _spectrum)
    {
        spectrum = _spectrum;
    }
}

[System.Serializable]
public class SpectralFluxData
{
    public float spectralFlux;
    public float threshold;
    public float thresholdedSpectralFlux;
    public float time;
    public bool isOnset;
}

[System.Serializable]
public class FrequencyBand
{
    public string name;

    //Defined Frequencies in the band
    public int lowerBoundary = 0;
    public int upperBoundary = 0;

    //Sensitivity multiplier to scale the average threshold.
    //In this case, if a rectified spectral flux sample is > 1.5 times the average, it is a peak
    public float thresholdMultiplier = 1;

    //List of the spectral flux in the frequency band
    [HideInInspector]
    public List<SpectralFluxData> spectralFluxSamples = new List<SpectralFluxData>();

    //The current index to process
    [HideInInspector]
    public int spectralFluxIndex = 0;

    public void Reset()
    {
        spectralFluxSamples.Clear();
        spectralFluxIndex = 0;
    }

    public float GetFluxThreshold(int thresholdWindowSize)
    {
        //Number of samples that create the window from the current time position
        int windowStartIndex = Mathf.Max(0, spectralFluxIndex - thresholdWindowSize / 2);
        int windowEndIndex = Mathf.Min(spectralFluxSamples.Count - 1, spectralFluxIndex + thresholdWindowSize / 2);

        //Sum the spectral flux over the window
        float sum = 0f;
        for (int i = windowStartIndex; i < windowEndIndex; i++)
        {
            sum += spectralFluxSamples[i].spectralFlux;
        }

        //Return the average multiplied by the threshold multiplier
        float average = sum / (windowEndIndex - windowStartIndex);
        return average * thresholdMultiplier;
    }

    //Remove spectrum data that falls below the threshold
    public float GetPrunedSpectralFlux()
    {
        return Mathf.Max(0f, spectralFluxSamples[spectralFluxIndex].spectralFlux - spectralFluxSamples[spectralFluxIndex].threshold);
    }
}

[System.Serializable]
public class SongData
{
    public string songName;

    public float songTime;

    public int spectralSampleSize;
    public int thresholdWindowSize;

    public List<FrequencyBand> frequencyBands;
    public List<SpectrumData> spectrumData;
}

