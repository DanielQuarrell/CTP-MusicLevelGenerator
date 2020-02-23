using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        Array.Copy(_frequencyBandBoundaries, frequencyBands, _frequencyBandBoundaries.Length);

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

    private void AnalyseFrequencyBands(float time)
    {
        //Loop through the frequency bands
        foreach(FrequencyBand band in frequencyBands)
        {
            SpectralFluxData currentFluxData = new SpectralFluxData();
            currentFluxData.time = time;
            currentFluxData.spectralFlux = CalculateRectifiedSpectralFlux(band);

            band.spectralFluxSamples.Add(currentFluxData);

            if (band.spectralFluxSamples.Count >= thresholdWindowSize)
            {
                //Get Flux threshold of time window surrounding index to process
                band.spectralFluxSamples[band.spectralFluxIndex].threshold = band.GetFluxThreshold(thresholdWindowSize);

                //Keep samples that are above the threshold to use for peak filtering
                band.spectralFluxSamples[band.spectralFluxIndex].prunedSpectralFlux = band.GetPrunedSpectralFlux();

                //Access the previous index that has now has neighbors to determine peak
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
}