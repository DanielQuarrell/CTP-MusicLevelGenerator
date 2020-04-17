using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectrumAnalyzer 
{
    public FrequencyBand[] frequencyBands;
    public List<SpectrumData> spectrumData;

    //Number of samples per fft in the song (To the power of 2)
    int numberOfSamples = 1024;

    //Number of samples to average in the window
    int thresholdWindowSize = 50;

    //Number of frequency bars in the level scene
    int numberOfBars;

    //The frequency amount each index in the array represents
    float frequencyPerIndex = 0;
    float FFTmaxFrequency = 0;

    float[] currentSpectrum;
	float[] previousSpectrum;


    public SpectrumAnalyzer(int _sampleSize, float _sampleRate, int _thresholdWindowSize, FrequencyBand[] _frequencyBandBoundaries, int _numberOfBars)
    {
        numberOfSamples = (_sampleSize / 2) + 1;
        FFTmaxFrequency = _sampleRate / 2;

        frequencyPerIndex = FFTmaxFrequency / _sampleSize;

        thresholdWindowSize = _thresholdWindowSize;

        frequencyBands = new FrequencyBand[_frequencyBandBoundaries.Length];
        Array.Copy(_frequencyBandBoundaries, frequencyBands, _frequencyBandBoundaries.Length);

        //Start process halfway through the first window for each band
        foreach (FrequencyBand band in frequencyBands)
        {
            band.spectralFluxIndex = thresholdWindowSize / 2;
        }

        currentSpectrum = new float[numberOfSamples];
        previousSpectrum = new float[numberOfSamples];

        spectrumData = new List<SpectrumData>();

        numberOfBars = _numberOfBars;
    }
		
	public void AnalyzeSpectrum(float[] spectrum, float time) 
    {
        //Set new spectrum and save previous 
        SetCurrentSpectrum(spectrum);
        spectrumData.Add(new SpectrumData(ComputeAverages(spectrum)));
        AnalyseFrequencyBands(time);
	}

    //Sets the spectrum to be analysed and previous for comparison
    public void SetCurrentSpectrum(float[] newSpectrum) 
    {
        currentSpectrum.CopyTo(previousSpectrum, 0);
		newSpectrum.CopyTo(currentSpectrum, 0);
    }

    //Calculate Spectrum data averages for frequency bars
    public float[] ComputeAverages(float[] spectrum)
    {
        int spectrumSize = spectrum.Length;
        int incrementAmount = spectrumSize / numberOfBars;

        int lowBoundary = 0;
        int highBoundary = incrementAmount;

        float[] averages = new float[numberOfBars];

        for (int i = 0; i < numberOfBars; i++)
        {
            float average = 0;

            for (int j = lowBoundary; j <= highBoundary; j++)
            {
                average += spectrum[j];
            }

            average /= (highBoundary - lowBoundary + 1);
            averages[i] = average;

            lowBoundary += incrementAmount;
            highBoundary += incrementAmount;
        }

        return averages;
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
                band.spectralFluxSamples[band.spectralFluxIndex].thresholdedSpectralFlux = band.GetPrunedSpectralFlux();

                //Access the previous index that has now has neighbors to determine peak
                int indexToDetectPeak = band.spectralFluxIndex - 1;

                //If current sample is peak
                if (IsPeak(ref band.spectralFluxSamples, indexToDetectPeak))
                {
                    band.spectralFluxSamples[indexToDetectPeak].isOnset = true;
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
            //Current frequency of index < percentage of the max frequency provided by the FFT

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
        return (spectralFluxSamples[spectralFluxIndex].thresholdedSpectralFlux > spectralFluxSamples[spectralFluxIndex + 1].thresholdedSpectralFlux &&
                spectralFluxSamples[spectralFluxIndex].thresholdedSpectralFlux > spectralFluxSamples[spectralFluxIndex - 1].thresholdedSpectralFlux);
	}
}