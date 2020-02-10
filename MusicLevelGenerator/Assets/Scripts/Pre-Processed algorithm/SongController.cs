using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEditor;
using DSPLib;

public class SongController : MonoBehaviour 
{
    static public SongController instance;

    [SerializeField] bool debug;

	[SerializeField] AudioSource audioSource;
    [SerializeField] LevelGenerator levelGenerator;
    [SerializeField] Visualiser visualiser;
    
    [Header("Onset Algorithm Modifiers")]
    public int spectrumSampleSize = 1024;

    // Number of samples to average in our window
    public int thresholdWindowSize = 50;

    public FrequencyBand[] frequencyBandBoundaries;

    SpectralFluxAnalyzer spectralFluxAnalyzer;

    int numOfChannels;
	int totalSamples;
	int sampleRate;
	float clipLength;
	float[] multiChannelSamples;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start() 
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
            audioSource.Play();
        }
        else
        {
            levelGenerator.GenerateLevelFromSamples(spectralFluxAnalyzer.frequencyBands, audioSource.clip.length);
            StartCoroutine(StartSong());
        }
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

    public void ReprocessSong()
    {
        audioSource.Pause();

        spectralFluxAnalyzer = new SpectralFluxAnalyzer(spectrumSampleSize, sampleRate, thresholdWindowSize, frequencyBandBoundaries);
        audioSource.clip.GetData(multiChannelSamples, 0);
        ProcessFullSpectrum();

        visualiser.GenerateVisualiserFromSamples(spectralFluxAnalyzer.frequencyBands, audioSource.clip.length);

        audioSource.Play();
    }

    public void RestartSong()
    {
        audioSource.Stop();
        levelGenerator.ResetLevel();
        audioSource.Play();
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
        //Prepare audio samples
        float[] preProcessedSamples = new float[totalSamples];

        int numProcessed = 0;
        float combinedChannelAverage = 0f;

        for (int i = 0; i < multiChannelSamples.Length; i++)
        {
            combinedChannelAverage += multiChannelSamples[i];

            // Each time we have processed all channels samples for a point in time, we will store the average of the channels combined
            if ((i + 1) % this.numOfChannels == 0)
            {
                preProcessedSamples[numProcessed] = combinedChannelAverage / numOfChannels;
                numProcessed++;
                combinedChannelAverage = 0f;
            }
        }

        Debug.Log("Combine Channels done");
        Debug.Log(preProcessedSamples.Length);
        //-----------------------------------------------------------------

        //Execute an FFT to return the spectrum data over the time domain
        int totalIterations = preProcessedSamples.Length / spectrumSampleSize;

        FFT fft = new FFT();
        fft.Initialize((UInt32)spectrumSampleSize);

        Debug.Log("Processing " + totalIterations + " time domain samples for FFT");
        double[] sampleChunk = new double[spectrumSampleSize];
        for (int i = 0; i < totalIterations; i++)
        {
            //Grab the current 1024 chunk of audio sample data
            Array.Copy(preProcessedSamples, i * spectrumSampleSize, sampleChunk, 0, spectrumSampleSize);

            //Apply an FFT Window type
            double[] windowCoefs = DSP.Window.Coefficients(DSP.Window.Type.Hanning, (uint)spectrumSampleSize);
            double[] scaledSpectrumChunk = DSP.Math.Multiply(sampleChunk, windowCoefs);
            double scaleFactor = DSP.Window.ScaleFactor.Signal(windowCoefs);

            //Perform the FFT and convert output (complex numbers) to Magnitude
            Complex[] fftSpectrum = fft.Execute(scaledSpectrumChunk);
            double[] scaledFFTSpectrum = DSPLib.DSP.ConvertComplex.ToMagnitude(fftSpectrum);
            scaledFFTSpectrum = DSP.Math.Multiply(scaledFFTSpectrum, scaleFactor);

            //These 1024 magnitude values correspond to a single point in the audio timeline
            float curSongTime = GetTimeFromIndex(i) * spectrumSampleSize;

            //Send our magnitude data off to our Spectral Flux Analyzer to be analyzed for peaks
            spectralFluxAnalyzer.AnalyzeSpectrum(Array.ConvertAll(scaledFFTSpectrum, x => (float)x), curSongTime);
        }

        Debug.Log("Spectrum Analysis done");
    }

    IEnumerator StartSong()
    {
        yield return new WaitForSeconds(0.1f);

        RestartSong();
    }
}