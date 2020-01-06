using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visualiser : MonoBehaviour
{
    [SerializeField] float spacingBetweenSamples;
    [SerializeField] float heightMultiplier;

    [SerializeField] Transform lowSpectralParent;
    [SerializeField] Transform highSpectralParent;

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
        for (int b = 0; b < frequencyBands.Length; b++)
        {
            FrequencyBand band = frequencyBands[b];

            for (int i = 0; i < band.spectralFluxSamples.Count; i++)
            {
                SpectralFluxData sample = band.spectralFluxSamples[i];
                PlotPoint spectralPoint = Instantiate(plotPoint, new Vector2(i * spacingBetweenSamples, sample.spectralFlux * heightMultiplier), Quaternion.identity, b == 0 ? lowSpectralParent : highSpectralParent);

                spectralPoint.spriteRenderer.color = sample.isPeak ? peakColor : plotColor;

                if(previousPlotTransform != null)
                {
                    spectralPoint.connectingPlotTransform = previousPlotTransform;
                }
                
                previousPlotTransform = spectralPoint.transform;
            }
        }

        lowSpectralParent.position = new Vector3(0, -5, 0);
        highSpectralParent.position = new Vector3(0, 2, 0);

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
