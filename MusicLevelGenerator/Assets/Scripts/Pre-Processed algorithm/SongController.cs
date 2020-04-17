using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DSPLib;
using System.IO;

public class SongController : MonoBehaviour 
{
    static public SongController instance;

	[SerializeField] AudioSource audioSource;
    [SerializeField] Visualiser visualiser;

    [SerializeField] TextAsset songJsonFile;

    [Header("Visual Modifiers")]
    public int numberOfBars = 12;
    
    [Header("Onset Algorithm Modifiers")]
    //Number of samples to average in the moving window
    public int spectrumSampleSize = 1024;

    //Number of samples to average in the threshold window
    public int thresholdWindowSize = 50;

    public List<FrequencyBand> frequencyBandBoundaries;

    SpectrumAnalyzer analyzer;

    //Number of channels in the clip (Mono = 1, Stereo = 2)
    int numberOfChannels;
    //Number of samples in the clip
	int totalSamples;
    //Samples per second
	int sampleRate;
    //Length of song in seconds
	float songTime;
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
		//Get audio samples from all channels
		multiChannelSamples = new float[audioSource.clip.samples * audioSource.clip.channels];
		numberOfChannels = audioSource.clip.channels;
		totalSamples = audioSource.clip.samples;
		songTime = audioSource.clip.length;

		//Store the clip's sampling rate
		sampleRate = audioSource.clip.frequency;

        //Preprocess entire audio clip
        analyzer = new SpectrumAnalyzer(spectrumSampleSize, sampleRate, thresholdWindowSize, frequencyBandBoundaries.ToArray(), numberOfBars);

        //Retrieve sample data from clip
        audioSource.clip.GetData(multiChannelSamples, 0);

        ProcessFullSpectrum();

        //Visuallise processed audio
        visualiser.GenerateVisualiserFromSamples(analyzer.frequencyBands, audioSource.clip.length);
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
            band.Reset();
        }

        analyzer = new SpectrumAnalyzer(spectrumSampleSize, sampleRate, thresholdWindowSize, frequencyBandBoundaries.ToArray(), numberOfBars);
        audioSource.clip.GetData(multiChannelSamples, 0);
        ProcessFullSpectrum();

        visualiser.GenerateVisualiserFromSamples(analyzer.frequencyBands, audioSource.clip.length);

        audioSource.Play();
    }

    public float GetSongTime()
    {
        return audioSource.time;
    }

    public void LoadSongData()
    {
        string data = songJsonFile.text;
        SongData songData = JsonUtility.FromJson<SongData>(data);

        audioSource.clip = Resources.Load<AudioClip>("Audio/" + songData.songName);
        songTime = audioSource.clip.length;

        spectrumSampleSize = songData.spectralSampleSize;
        thresholdWindowSize = songData.thresholdWindowSize;

        songData.frequencyBands = new List<FrequencyBand>(frequencyBandBoundaries);
    }

    public void SaveToFile()
    {
        SongData songData = new SongData();

        songData.songName = audioSource.clip.name;
        songData.songTime = songTime;

        songData.spectralSampleSize = spectrumSampleSize;
        songData.thresholdWindowSize = thresholdWindowSize;

        songData.frequencyBands = new List<FrequencyBand>(analyzer.frequencyBands);
        songData.spectrumData = new List<SpectrumData>(analyzer.spectrumData);

        string data = string.Empty;
        data = JsonUtility.ToJson(songData, true);

        UnityEditor.AssetDatabase.Refresh();

        File.WriteAllText("Assets/Resources/SongDataFiles/" + songData.songName + "_Data.json", data);
        Debug.Log("Saved song data to: " + "Assets/Resources/SongDataFiles/" + songData.songName + "_Data.json");

        UnityEditor.AssetDatabase.Refresh();

        songJsonFile = Resources.Load<TextAsset>("SongDataFiles/" + songData.songName + "_Data.json");
    }

    private void ProcessFullSpectrum()
    {
        //Prepare audio samples
        float[] preProcessedSamples = new float[totalSamples];

        int numProcessed = 0;
        float combinedChannelAverage = 0f;

        //Combine channels if the audio is stereo
        //Add to pre-processed array to be passed through the fft
        for (int i = 0; i < multiChannelSamples.Length; i++)
        {
            combinedChannelAverage += multiChannelSamples[i];

            //Store the average of the channels combined for a single time index
            if ((i + 1) % this.numberOfChannels == 0)
            {
                preProcessedSamples[numProcessed] = combinedChannelAverage / numberOfChannels;
                numProcessed++;
                combinedChannelAverage = 0f;
            }
        }

        //Execute an FFT to return the spectrum data over the time domain
        int totalIterations = preProcessedSamples.Length / spectrumSampleSize;

        //Instantiate and initialise a new FFT
        FFT fft = new FFT();
        fft.Initialize((uint)spectrumSampleSize);

        double[] sampleChunk = new double[spectrumSampleSize];
        for (int i = 0; i < totalIterations; i++)
        {
            //Get the current chunk of audio sample data
            Array.Copy(preProcessedSamples, i * spectrumSampleSize, sampleChunk, 0, spectrumSampleSize);

            //Apply an FFT Window type to the input data and calculate a scale factor
            double[] windowCoefficients = DSP.Window.Coefficients(DSP.Window.Type.Hanning, (uint)spectrumSampleSize);
            double[] scaledSpectrumChunk = DSP.Math.Multiply(sampleChunk, windowCoefficients);
            double scaleFactor = DSP.Window.ScaleFactor.Signal(windowCoefficients);

            //Execute the FFT and get the scaled spectrum back
            Complex[] fftSpectrum = fft.Execute(scaledSpectrumChunk);
            //Convert the complex spectrum into a usable format of doubles
            double[] scaledFFTSpectrum = DSP.ConvertComplex.ToMagnitude(fftSpectrum);
            //Apply the window scale to the spectrum
            scaledFFTSpectrum = DSP.Math.Multiply(scaledFFTSpectrum, scaleFactor);


            //The magnitude values correspond to a single point in time
            float currentSongTime = GetTimeFromIndex(i) * spectrumSampleSize;

            //Send magnitude spectrum data to Spectral Flux Analyzer to be analyzed for peaks
            analyzer.AnalyzeSpectrum(Array.ConvertAll(scaledFFTSpectrum, x => (float)x), currentSongTime);
        }

        Debug.Log("Spectrum Analysis done");
    }

    private float GetTimeFromIndex(int index)
    {
        return ((1f / (float)this.sampleRate) * index);
    }
}