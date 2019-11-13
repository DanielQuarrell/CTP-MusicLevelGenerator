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
    [SerializeField] SpriteRenderer plotPoint;
    [SerializeField] Color plotColor;
    [SerializeField] Color peakColor;

    float levelLength;
    float songTime;

    public void GenerateVisualiserFromSamples(List<SpectralFluxInfo> _spectralFluxLowSamples, List<SpectralFluxInfo> _spectralFluxHighSamples, float _songTime)
    {
        for (int i = 0; i < _spectralFluxLowSamples.Count; i++)
        {
            SpectralFluxInfo lowSample = _spectralFluxLowSamples[i];

            SpriteRenderer lowSpectralPoint = Instantiate(plotPoint, new Vector2(i * spacingBetweenSamples, lowSample.spectralFlux * heightMultiplier), Quaternion.identity, lowSpectralParent);

            if (lowSample.isPeak)
            {
                lowSpectralPoint.color = peakColor;
            }
            else
            {
                lowSpectralPoint.color = plotColor;
            }
        }

        for (int i = 0; i < _spectralFluxHighSamples.Count; i++)
        {
            SpectralFluxInfo highSample = _spectralFluxHighSamples[i];

            SpriteRenderer highSpectralPoint = Instantiate(plotPoint, new Vector2(i * spacingBetweenSamples, highSample.spectralFlux * heightMultiplier), Quaternion.identity, highSpectralParent);

            if (highSample.isPeak)
            {
                highSpectralPoint.color = peakColor;
            }
            else
            {
                highSpectralPoint.color = plotColor;
            }
        }

        lowSpectralParent.position = new Vector3(0, -5, 0);
        highSpectralParent.position = new Vector3(0, 2, 0);

        levelLength = (_spectralFluxLowSamples.Count * spacingBetweenSamples);
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
