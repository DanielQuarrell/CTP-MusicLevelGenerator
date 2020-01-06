using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using DSPLib;

public class SongController : MonoBehaviour 
{
    [SerializeField] bool debug;

	[SerializeField] AudioSource audioSource;
    [SerializeField] LevelGenerator levelGenerator;
    [SerializeField] Visualiser visualiser;
    
    [Header("Onset Algorithm Modifiers")]
    [SerializeField] int spectrumSampleSize = 1024;

    // Number of samples to average in our window
    [SerializeField] int thresholdWindowSize = 50;

    [SerializeField] FrequencyBand[] frequencyBandBoundaries;

    SpectralFluxAnalyzer spectralFluxAnalyzer;

    int numOfChannels;
	int totalSamples;
	int sampleRate;
	float clipLength;
	float[] multiChannelSamples;

    void Start() 
    {
		// Need all audio samples.  If in stereo, samples will return with left and right channels interweaved
		// [L,R,L,R,L,R]
		multiChannelSamples = new float[audioSource.clip.samples * audioSource.clip.channels];
		numOfChannels = audioSource.clip.channels;
		totalSamples = audioSource.clip.samples;
		clipLength = audioSource.clip.length;

		//Store the clip's sampling rate
		sampleRate = audioSource.clip.frequency;

        //Preprocess entire audio clip
        spectralFluxAnalyzer = new SpectralFluxAnalyzer(spectrumSampleSize, sampleRate, thresholdWindowSize, frequencyBandBoundaries);

        audioSource.clip.GetData(multiChannelSamples, 0);

        ProcessFullSpectrum();

        //Visuallise processed audio
        if(debug)
        {
            visualiser.GenerateVisualiserFromSamples(spectralFluxAnalyzer.frequencyBands, audioSource.clip.length);
        }
        else
        {
            //levelGenerator.GenerateLevelFromSamples(spectralFluxAnalyzer.spectralFluxSamples, audioSource.clip.length);
        }

        
        audioSource.Play();
    }

	void Update() 
    {
        if (debug)
        {
            visualiser.UpdateTimePosition(audioSource.time);
        }
        else
        {
            levelGenerator.UpdatePlayerPosition(audioSource.time);
        }
        
    }

	public int GetIndexFromTime(float curTime) 
    {
		float lengthPerSample = this.clipLength / (float)this.totalSamples;

		return Mathf.FloorToInt (curTime / lengthPerSample);
	}

	public float GetTimeFromIndex(int index) 
    {
		return ((1f / (float)this.sampleRate) * index);
	}

    private void ProcessFullSpectrum()
    {
        float[] preProcessedSamples = new float[this.totalSamples];

        int numProcessed = 0;
        float combinedChannelAverage = 0f;

        for (int i = 0; i < multiChannelSamples.Length; i++)
        {
            combinedChannelAverage += multiChannelSamples[i];

            // Each time we have processed all channels samples for a point in time, we will store the average of the channels combined
            if ((i + 1) % this.numOfChannels == 0)
            {
                preProcessedSamples[numProcessed] = combinedChannelAverage / this.numOfChannels;
                numProcessed++;
                combinedChannelAverage = 0f;
            }
        }

        Debug.Log("Combine Channels done");
        Debug.Log(preProcessedSamples.Length);
        //------------------------------------------

        // Once we have our audio sample data prepared, we can execute an FFT to return the spectrum data over the time domain
        int iterations = preProcessedSamples.Length / spectrumSampleSize;

        FFT fft = new FFT();
        fft.Initialize((UInt32)spectrumSampleSize);

        Debug.Log("Processing " + iterations + " time domain samples for FFT");
        double[] sampleChunk = new double[spectrumSampleSize];
        for (int i = 0; i < iterations; i++)
        {
            //Grab the current 1024 chunk of audio sample data
            Array.Copy(preProcessedSamples, i * spectrumSampleSize, sampleChunk, 0, spectrumSampleSize);

            //Apply an FFT Window type
            double[] windowCoefs = DSP.Window.Coefficients(DSP.Window.Type.Hanning, (uint)spectrumSampleSize);
            double[] scaledSpectrumChunk = DSP.Math.Multiply(sampleChunk, windowCoefs);
            double scaleFactor = DSP.Window.ScaleFactor.Signal(windowCoefs);

            // Perform the FFT and convert output (complex numbers) to Magnitude
            Complex[] fftSpectrum = fft.Execute(scaledSpectrumChunk);
            double[] scaledFFTSpectrum = DSPLib.DSP.ConvertComplex.ToMagnitude(fftSpectrum);
            scaledFFTSpectrum = DSP.Math.Multiply(scaledFFTSpectrum, scaleFactor);

            // These 1024 magnitude values correspond (roughly) to a single point in the audio timeline
            float curSongTime = GetTimeFromIndex(i) * spectrumSampleSize;

            // Send our magnitude data off to our Spectral Flux Analyzer to be analyzed for peaks
            spectralFluxAnalyzer.AnalyzeSpectrum(Array.ConvertAll(scaledFFTSpectrum, x => (float)x), curSongTime);
        }

        Debug.Log("Spectrum Analysis done");
     }

    //Unused
    private void GetFullSpectrumThreaded() 
    {
		try 
        {
            //Combine channels
            //----------------------------------------
			// We only need to retain the samples for combined channels over the time domain
			float[] preProcessedSamples = new float[this.totalSamples];

			int numProcessed = 0;
			float combinedChannelAverage = 0f;

			for (int i = 0; i < multiChannelSamples.Length; i++) 
            {
				combinedChannelAverage += multiChannelSamples [i];

				// Each time we have processed all channels samples for a point in time, we will store the average of the channels combined
				if ((i + 1) % this.numOfChannels == 0) 
                {
					preProcessedSamples[numProcessed] = combinedChannelAverage / this.numOfChannels;
					numProcessed++;
					combinedChannelAverage = 0f;
				}
			}

			Debug.Log ("Combine Channels done");
			Debug.Log (preProcessedSamples.Length);
            //------------------------------------------

			// Once we have our audio sample data prepared, we can execute an FFT to return the spectrum data over the time domain
			int spectrumSampleSize = 1024;
			int iterations = preProcessedSamples.Length / spectrumSampleSize;

			FFT fft = new FFT ();
			fft.Initialize ((UInt32)spectrumSampleSize);

			Debug.Log (string.Format("Processing {0} time domain samples for FFT", iterations));
			double[] sampleChunk = new double[spectrumSampleSize];
			for (int i = 0; i < iterations; i++) 
            {
				// Grab the current 1024 chunk of audio sample data
				Array.Copy (preProcessedSamples, i * spectrumSampleSize, sampleChunk, 0, spectrumSampleSize);

				// Apply our chosen FFT Window
				double[] windowCoefs = DSP.Window.Coefficients (DSP.Window.Type.Hanning, (uint)spectrumSampleSize);
				double[] scaledSpectrumChunk = DSP.Math.Multiply (sampleChunk, windowCoefs);
				double scaleFactor = DSP.Window.ScaleFactor.Signal (windowCoefs);

				// Perform the FFT and convert output (complex numbers) to Magnitude
				Complex[] fftSpectrum = fft.Execute (scaledSpectrumChunk);
				double[] scaledFFTSpectrum = DSPLib.DSP.ConvertComplex.ToMagnitude (fftSpectrum);
				scaledFFTSpectrum = DSP.Math.Multiply (scaledFFTSpectrum, scaleFactor);

				// These 1024 magnitude values correspond (roughly) to a single point in the audio timeline
				float curSongTime = GetTimeFromIndex(i) * spectrumSampleSize;

                // Send our magnitude data off to our Spectral Flux Analyzer to be analyzed for peaks
                spectralFluxAnalyzer.AnalyzeSpectrum (Array.ConvertAll (scaledFFTSpectrum, x => (float)x), curSongTime);
			}

			Debug.Log ("Spectrum Analysis done");
			Debug.Log ("Background Thread Completed");
				
		} 
        catch (Exception e) 
        {
			// Catch exceptions here since the background thread won't always surface the exception to the main thread
			Debug.Log (e.ToString ());
		}
	}
}