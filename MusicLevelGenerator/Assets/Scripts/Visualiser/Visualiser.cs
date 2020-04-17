using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BandVisualiser
{
    public Transform parent;
    public RectTransform seperator;
    public float heightMultiplier;
}

public class Visualiser : MonoBehaviour
{
    [SerializeField] float spacingBetweenSamples;
    [SerializeField] float bandOffsetMultiplier = 0.15f;

    [SerializeField] BandVisualiser[] FrequencyBandVisualisers;

    [SerializeField] Transform currentTimeMarker;

    [Header("PlotPoint attributes")]
    [SerializeField] PlotPoint plotPoint;
    [SerializeField] Color plotColor;
    [SerializeField] Color peakColor;

    Transform previousPlotTransform;

    float levelLength;
    float songTime;

    public void GenerateVisualiserFromSamples(FrequencyBand[] frequencyBands, float _songTime)
    {
        for (int i = 0; i < 4; i++)
        {
            FrequencyBandVisualisers[i].seperator.gameObject.SetActive(i < frequencyBands.Length);
        }

        for (int b = 0; b < frequencyBands.Length; b++)
        {
            FrequencyBand band = frequencyBands[b];

            FrequencyBandVisualisers[b].parent.position = new Vector3(0f, 0f, 0f);

            previousPlotTransform = null;

            foreach (Transform child in FrequencyBandVisualisers[b].parent)
            {
                GameObject.Destroy(child.gameObject);
            }

            for (int i = 0; i < band.spectralFluxSamples.Count; i++)
            {
                SpectralFluxData sample = band.spectralFluxSamples[i];
                PlotPoint spectralPoint = Instantiate(plotPoint, new Vector2(i * spacingBetweenSamples, sample.spectralFlux * FrequencyBandVisualisers[b].heightMultiplier), Quaternion.identity, FrequencyBandVisualisers[b].parent);

                spectralPoint.spriteRenderer.color = sample.isOnset ? peakColor : plotColor;

                if(previousPlotTransform != null)
                {
                    spectralPoint.connectingPlotTransform = previousPlotTransform;
                }
                
                previousPlotTransform = spectralPoint.transform;
            }

            float screenHeight = 12.6f;
            float distanceBetweenBands = screenHeight / frequencyBands.Length;
            float offsetSpace = distanceBetweenBands * bandOffsetMultiplier;
            float initialOffset = (screenHeight / 2) - distanceBetweenBands + offsetSpace;

            FrequencyBandVisualisers[b].parent.position = new Vector3(0f, initialOffset - (distanceBetweenBands * b), 0f);

            Vector2 anchorMin = FrequencyBandVisualisers[b].seperator.anchorMin;
            Vector2 anchorMax = FrequencyBandVisualisers[b].seperator.anchorMax;

            anchorMin.Set(anchorMin.x, (b) * (1f / frequencyBands.Length));
            anchorMax.Set(anchorMax.x, (b + 1f) * (1f / frequencyBands.Length));

            FrequencyBandVisualisers[b].seperator.anchorMin = anchorMin;
            FrequencyBandVisualisers[b].seperator.anchorMax = anchorMax;
        }

        levelLength = (frequencyBands[0].spectralFluxSamples.Count * spacingBetweenSamples);
        songTime = _songTime;
    }

    public void UpdateTimePosition(float currentTime)
    {
        float percentageThroughLevel = Mathf.InverseLerp(0, songTime, currentTime);

        //Can be changed to move the player through the level
        float timePosition = Mathf.Lerp(0.0f, levelLength, percentageThroughLevel);

        currentTimeMarker.position = new Vector2(timePosition, currentTimeMarker.position.y);
    }
}
