using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectralFluxInfo 
{
	public float time;
	public float spectralFlux;
	public float threshold;
	public float prunedSpectralFlux;
    public float peakPercentage;
	public bool isPeak;
}

public class SpectralFluxAnalyzer 
{
    int numSamples = 1024;

	// Sensitivity multiplier to scale the average threshold.
	// In this case, if a rectified spectral flux sample is > 1.5 times the average, it is a peak
	float thresholdMultiplier = 1.5f;

	// Number of samples to average in our window
	int thresholdWindowSize = 50;

    int lowIndexToProcess = 0;
    int highIndexToProcess = 0;

    float frequencyPerIndex = 0;
    float FFTmaxFrequency = 0;

	public List<SpectralFluxInfo> spectralFluxHighSamples;
    public List<SpectralFluxInfo> spectralFluxLowSamples;

    float[] curSpectrum;
	float[] prevSpectrum;


    public SpectralFluxAnalyzer(int _sampleSize, float _thresholdMultiplier, int _thresholdWindowSize, float _maxFrequency)
    {
        numSamples = (_sampleSize / 2) + 1;
        FFTmaxFrequency = _maxFrequency / 2;

        frequencyPerIndex = (_maxFrequency / 2) / _sampleSize;

        thresholdMultiplier = _thresholdMultiplier;
        thresholdWindowSize = _thresholdWindowSize;

        spectralFluxHighSamples = new List<SpectralFluxInfo>();
        spectralFluxLowSamples = new List<SpectralFluxInfo>();

        // Start processing from middle of first window and increment by 1 from there
        lowIndexToProcess = thresholdWindowSize / 2;
        highIndexToProcess = thresholdWindowSize / 2;

        curSpectrum = new float[numSamples];
        prevSpectrum = new float[numSamples];

    }

	public void SetCurrentSpectrum(float[] spectrum) 
    {
        curSpectrum.CopyTo(prevSpectrum, 0);
		spectrum.CopyTo(curSpectrum, 0);
	}
		
	public void AnalyzeSpectrum(float[] spectrum, float time) 
    {
		// Set spectrum
		SetCurrentSpectrum(spectrum);
        AnalyseLowBand(time);
        AnalyseHighBand(time);
	}
    private void AnalyseLowBand(float time)
    {
        // Get current spectral flux from spectrum
        SpectralFluxInfo curInfo = new SpectralFluxInfo();
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
        SpectralFluxInfo curInfo = new SpectralFluxInfo();
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

    //Calculate total positive changes in a low frequency band in the spectrum data
    float CalculateHighRectifiedSpectralFlux()
    {
        float sum = 0f;

        for (int i = 0; i < numSamples; i++)
        {
            //Seperate the spectral flux into High and Low bands
            //Current frequency of index < Half the max frequency provided by the FFT

            if ((i * frequencyPerIndex) >= FFTmaxFrequency / 4)
            {
                sum += Mathf.Max(0f, curSpectrum[i] - prevSpectrum[i]);
            }
        }
        return sum;
    }

    //Calculate total positive changes in a high freqeuncy band in the spectrum data
    float CalculateLowRectifiedSpectralFlux() 
    {
		float sum = 0f;

		for (int i = 0; i < numSamples; i++) 
        {
            //Seperate the spectral flux into High and Low bands
            //Current frequency of index < Half the max frequency provided by the FFT

            if((i * frequencyPerIndex) < FFTmaxFrequency / 4)
            {
                sum += Mathf.Max(0f, curSpectrum[i] - prevSpectrum[i]);
            }
		}
		return sum;
	}

	float GetFluxThreshold(ref List<SpectralFluxInfo> spectralFluxSamples, int spectralFluxIndex) 
    {
		// How many samples in the past and future we include in our average
		int windowStartIndex = Mathf.Max (0, spectralFluxIndex - thresholdWindowSize / 2);
		int windowEndIndex = Mathf.Min (spectralFluxSamples.Count - 1, spectralFluxIndex + thresholdWindowSize / 2);
		
	    //Add up our spectral flux over the window
		float sum = 0f;
		for (int i = windowStartIndex; i < windowEndIndex; i++) 
        {
			sum += spectralFluxSamples [i].spectralFlux;
		}

		// Return the average multiplied by our sensitivity multiplier
		float avg = sum / (windowEndIndex - windowStartIndex);
		return avg * thresholdMultiplier;
	}

	float GetPrunedSpectralFlux(ref List<SpectralFluxInfo> spectralFluxSamples, int spectralFluxIndex) 
    {
		return Mathf.Max (0f, spectralFluxSamples[spectralFluxIndex].spectralFlux - spectralFluxSamples [spectralFluxIndex].threshold);
	}

	bool IsPeak(ref List<SpectralFluxInfo> spectralFluxSamples, int spectralFluxIndex) 
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

	void LogSample(ref List<SpectralFluxInfo> spectralFluxSamples, int indexToLog) 
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