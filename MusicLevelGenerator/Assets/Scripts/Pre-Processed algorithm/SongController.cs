using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEditor;
using DSPLib;
using System.IO;

public class SongController : MonoBehaviour 
{
    static public SongController instance;

    [SerializeField] bool debug;

	[SerializeField] AudioSource audioSource;
    [SerializeField] Visualiser visualiser;

    [SerializeField] TextAsset songJsonFile;
    
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
        visualiser.GenerateVisualiserFromSamples(spectralFluxAnalyzer.frequencyBands, audioSource.clip.length);
        audioSource.Play();
    }

    void Update() 
    {
        visualiser.UpdateTimePosition(audioSource.time);
    }

    public void ReprocessSong()
    {
        audioSource.Pause();

        foreach(FrequencyBand band in frequencyBandBoundaries)
        {
            band.spectralFluxSamples.Clear();
            band.spectralFluxIndex = 0;
        }

        spectralFluxAnalyzer = new SpectralFluxAnalyzer(spectrumSampleSize, sampleRate, thresholdWindowSize, frequencyBandBoundaries);
        audioSource.clip.GetData(multiChannelSamples, 0);
        ProcessFullSpectrum();

        visualiser.GenerateVisualiserFromSamples(spectralFluxAnalyzer.frequencyBands, audioSource.clip.length);

        audioSource.Play();
    }

    public float GetSongTime()
    {
        return audioSource.time;
    }

    void LoadSongData()
    {
        string data = songJsonFile.text;
        SongData songData = JsonUtility.FromJson<SongData>(data);
    }

    public void SaveToFile()
    {
        SongData songData = new SongData();

        songData.songName = audioSource.clip.name;
        songData.clipLength = clipLength;

        songData.spectralSampleSize = spectrumSampleSize;
        songData.thresholdWindowSize = thresholdWindowSize;

        songData.frequencyBands = new List<FrequencyBand>(frequencyBandBoundaries);

        string data = string.Empty;
        data = JsonUtility.ToJson(songData, true);

        File.WriteAllText("Assets/Resources/SongDataFiles/" + songData.songName + "_Data.json", data);
        Debug.Log("Saved world to: " + "Assets/Resources/SongDataFiles/" + songData.songName + "_Data.json");
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
            double[] windowCoefficients = DSP.Window.Coefficients(DSP.Window.Type.Hanning, (uint)spectrumSampleSize);
            double[] scaledSpectrumChunk = DSP.Math.Multiply(sampleChunk, windowCoefficients);
            double scaleFactor = DSP.Window.ScaleFactor.Signal(windowCoefficients);

            //Perform the FFT and convert output (complex numbers) to Magnitude
            Complex[] fftSpectrum = fft.Execute(scaledSpectrumChunk);
            double[] scaledFFTSpectrum = DSP.ConvertComplex.ToMagnitude(fftSpectrum);
            scaledFFTSpectrum = DSP.Math.Multiply(scaledFFTSpectrum, scaleFactor);

            //These 1024 magnitude values correspond to a single point in the audio timeline
            float curSongTime = GetTimeFromIndex(i) * spectrumSampleSize;

            //Send our magnitude data off to our Spectral Flux Analyzer to be analyzed for peaks
            spectralFluxAnalyzer.AnalyzeSpectrum(Array.ConvertAll(scaledFFTSpectrum, x => (float)x), curSongTime);
        }

        Debug.Log("Spectrum Analysis done");
    }
}