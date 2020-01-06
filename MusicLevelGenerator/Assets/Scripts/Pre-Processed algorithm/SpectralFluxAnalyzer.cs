using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectralFluxData 
{
	public float time;
	public float spectralFlux;
	public float threshold;
	public float prunedSpectralFlux;
    public float peakPercentage;
	public bool isPeak;
}

[System.Serializable]
public class FrequencyBand
{
    //Defined Frequencies in the band
    public int lowerBoundary = 0;
    public int upperBoundary = 0;

    //Sensitivity multiplier to scale the average threshold.
    //In this case, if a rectified spectral flux sample is > 1.5 times the average, it is a peak
    public float thresholdMultiplier = 1;

    //List of the spectral flux in the frequency band
    public List<SpectralFluxData> spectralFluxSamples = new List<SpectralFluxData>();
    
    //The current index to process
    [HideInInspector]
    public int spectralFluxIndex = 0;

    public float GetFluxThreshold(int thresholdWindowSize)
    {
        //Number of samples that create the window from the current time position
        int windowStartIndex = Mathf.Max(0, spectralFluxIndex - thresholdWindowSize / 2);
        int windowEndIndex = Mathf.Min(spectralFluxSamples.Count - 1, spectralFluxIndex + thresholdWindowSize / 2);

        //Add up our spectral flux over the window
        float sum = 0f;
        for (int i = windowStartIndex; i < windowEndIndex; i++)
        {
            sum += spectralFluxSamples[i].spectralFlux;
        }

        // Return the average multiplied by our sensitivity multiplier
        float avg = sum / (windowEndIndex - windowStartIndex);
        return avg * thresholdMultiplier;
    }

    public float GetPrunedSpectralFlux()
    {
        return Mathf.Max(0f, spectralFluxSamples[spectralFluxIndex].spectralFlux - spectralFluxSamples[spectralFluxIndex].threshold);
    }
}

public class SpectralFluxAnalyzer 
{
    public FrequencyBand[] frequencyBands;

    int numberOfSamples = 1024;

    // Number of samples to average in our window
    int thresholdWindowSize = 50;

    float frequencyPerIndex = 0;
    float FFTmaxFrequency = 0;

    float[] currentSpectrum;
	float[] previousSpectrum;

    public SpectralFluxAnalyzer(int _sampleSize, float _maxFrequency, int _thresholdWindowSize, FrequencyBand[] _frequencyBandBoundaries)
    {
        numberOfSamples = (_sampleSize / 2) + 1;
        FFTmaxFrequency = _maxFrequency / 2;

        frequencyPerIndex = (_maxFrequency / 2) / _sampleSize;

        thresholdWindowSize = _thresholdWindowSize;

        frequencyBands = new FrequencyBand[_frequencyBandBoundaries.Length];
        System.Array.Copy(_frequencyBandBoundaries, frequencyBands, _frequencyBandBoundaries.Length);

        //Begin processing from halfway through first window and continue to increment by 1
        foreach (FrequencyBand band in frequencyBands)
        {
            band.spectralFluxIndex = thresholdWindowSize / 2;
        }

        currentSpectrum = new float[numberOfSamples];
        previousSpectrum = new float[numberOfSamples];
    }

	public void SetCurrentSpectrum(float[] spectrum) 
    {
        currentSpectrum.CopyTo(previousSpectrum, 0);
		spectrum.CopyTo(currentSpectrum, 0);
	}
		
	public void AnalyzeSpectrum(float[] spectrum, float time) 
    {
		// Set spectrum
		SetCurrentSpectrum(spectrum);
        AnalyseFrequencyBands(time);
	}

    /*
    private void AnalyseLowBand(float time)
    {
        // Get current spectral flux from spectrum
        SpectralFluxData curInfo = new SpectralFluxData();
        curInfo.time = time;

        curInfo.spectralFlux = CalculateLowRectifiedSpectralFlux();

        spectralFluxLowSamples.Add(curInfo);

        // We have enough samples to detect a peak
        if (spectralFluxLowSamples.Count >= thresholdWindowSize)
        {
            // Get Flux threshold of time window surrounding index to process
            spectralFluxLowSamples[lowIndexToProcess].threshold = GetFluxThreshold(ref spectralFluxLowSamples, lowIndexToProcess);

            // Only keep amp amount above threshold to allow peak filtering
            spectralFluxLowSamples[lowIndexToProcess].prunedSpectralFlux = GetPrunedSpectralFlux(ref spectralFluxLowSamples, lowIndexToProcess);

            // Now that we are processed at n, n-1 has neighbors (n-2, n) to determine peak
            int indexToDetectPeak = lowIndexToProcess - 1;

            //If current sample is peak
            if (IsPeak(ref spectralFluxLowSamples, indexToDetectPeak))
            {
                spectralFluxLowSamples[indexToDetectPeak].isPeak = true;
            }
            lowIndexToProcess++;
        }
        else
        {
            Debug.Log(string.Format("Not ready yet.  At spectral flux sample size of {0} growing to {1}", spectralFluxLowSamples.Count, thresholdWindowSize));
        }
    }

    private void AnalyseHighBand(float time)
    {
        // Get current spectral flux from spectrum
        SpectralFluxData curInfo = new SpectralFluxData();
        curInfo.time = time;

        curInfo.spectralFlux = CalculateHighRectifiedSpectralFlux();

        spectralFluxHighSamples.Add(curInfo);

        // We have enough samples to detect a peak
        if (spectralFluxHighSamples.Count >= thresholdWindowSize)
        {
            // Get Flux threshold of time window surrounding index to process
            spectralFluxHighSamples[highIndexToProcess].threshold = GetFluxThreshold(ref spectralFluxHighSamples, highIndexToProcess);

            // Only keep amp amount above threshold to allow peak filtering
            spectralFluxHighSamples[highIndexToProcess].prunedSpectralFlux = GetPrunedSpectralFlux(ref spectralFluxHighSamples, highIndexToProcess);

            // Now that we are processed at n, n-1 has neighbors (n-2, n) to determine peak
            int indexToDetectPeak = highIndexToProcess - 1;

            //If current sample is peak
            if (IsPeak(ref spectralFluxHighSamples, indexToDetectPeak))
            {
                spectralFluxHighSamples[indexToDetectPeak].isPeak = true;
            }
            highIndexToProcess++;
        }
    }
    */

    private void AnalyseFrequencyBands(float time)
    {
        foreach(FrequencyBand band in frequencyBands)
        {
            SpectralFluxData currentFluxData = new SpectralFluxData();
            currentFluxData.time = time;
            currentFluxData.spectralFlux = CalculateRectifiedSpectralFlux(band);

            band.spectralFluxSamples.Add(currentFluxData);

            if (band.spectralFluxSamples.Count >= thresholdWindowSize)
            {
                // Get Flux threshold of time window surrounding index to process
                band.spectralFluxSamples[band.spectralFluxIndex].threshold = band.GetFluxThreshold(thresholdWindowSize);

                // Only keep amp amount above threshold to allow peak filtering
                band.spectralFluxSamples[band.spectralFluxIndex].prunedSpectralFlux = band.GetPrunedSpectralFlux();

                // Now that we are processed at n, n-1 has neighbors (n-2, n) to determine peak
                int indexToDetectPeak = band.spectralFluxIndex - 1;

                //If current sample is peak
                if (IsPeak(ref band.spectralFluxSamples, indexToDetectPeak))
                {
                    band.spectralFluxSamples[indexToDetectPeak].isPeak = true;
                }

                band.spectralFluxIndex++;
            }
        }
    }

    //Calculate total positive changes in a low frequency band in the spectrum data
    float CalculateHighRectifiedSpectralFlux()
    {
        float sum = 0f;

        for (int i = 0; i < numberOfSamples; i++)
        {
            //Seperate the spectral flux into High band
            //Current frequency of index > Half the max frequency provided by the FFT

            if ((i * frequencyPerIndex) >= FFTmaxFrequency / 4)
            {
                sum += Mathf.Max(0f, currentSpectrum[i] - previousSpectrum[i]);
            }
        }
        return sum;
    }

    //Calculate total positive changes in a high freqeuncy band in the spectrum data
    float CalculateLowRectifiedSpectralFlux() 
    {
		float sum = 0f;

		for (int i = 0; i < numberOfSamples; i++) 
        {
            //Seperate the spectral flux into Low band
            //Current frequency of index < Half the max frequency provided by the FFT

            if((i * frequencyPerIndex) < FFTmaxFrequency / 4)
            {
                sum += Mathf.Max(0f, currentSpectrum[i] - previousSpectrum[i]);
            }
		}
		return sum;
	}

    //Calculate total positive changes in a freqeuncy band in the spectrum data
    float CalculateRectifiedSpectralFlux(FrequencyBand band)
    {
        float sum = 0f;

        for (int i = 0; i < numberOfSamples; i++)
        {
            //Seperate the spectral flux into frequency band
            //Current frequency of index < percentage the max frequency provided by the FFT

            float indexFrequency = i * frequencyPerIndex;

            if (band.lowerBoundary < indexFrequency && indexFrequency < band.upperBoundary)
            {
                sum += Mathf.Max(0f, currentSpectrum[i] - previousSpectrum[i]);
            }
        }

        return sum;
    }

	bool IsPeak(ref List<SpectralFluxData> spectralFluxSamples, int spectralFluxIndex) 
    {
		if (spectralFluxSamples[spectralFluxIndex].prunedSpectralFlux > spectralFluxSamples[spectralFluxIndex + 1].prunedSpectralFlux &&
			spectralFluxSamples[spectralFluxIndex].prunedSpectralFlux > spectralFluxSamples[spectralFluxIndex - 1].prunedSpectralFlux) {
			return true;
		} 
        else 
        {
			return false;
		}
	}

	void LogSample(ref List<SpectralFluxData> spectralFluxSamples, int indexToLog) 
    {
		int windowStart = Mathf.Max (0, indexToLog - thresholdWindowSize / 2);
		int windowEnd = Mathf.Min (spectralFluxSamples.Count - 1, indexToLog + thresholdWindowSize / 2);
		Debug.Log (string.Format (
			"Peak detected at song time {0} with pruned flux of {1} ({2} over thresh of {3}).\n" +
			"Thresh calculated on time window of {4}-{5} ({6} seconds) containing {7} samples.",
			spectralFluxSamples [indexToLog].time,
			spectralFluxSamples [indexToLog].prunedSpectralFlux,
			spectralFluxSamples [indexToLog].spectralFlux,
			spectralFluxSamples [indexToLog].threshold,
			spectralFluxSamples [windowStart].time,
			spectralFluxSamples [windowEnd].time,
			spectralFluxSamples [windowEnd].time - spectralFluxSamples [windowStart].time,
			windowEnd - windowStart
		));
	}
}