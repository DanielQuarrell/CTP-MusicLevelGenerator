using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visualiser : MonoBehaviour
{
    [SerializeField] float spacingBetweenSamples;
    [SerializeField] float heightMultiplier;

    [SerializeField] Transform spectralParent;
    [SerializeField] Transform prunedSpectralParent;

    [SerializeField] Transform currentTimeMarker;

    [Header("PlotPoint attributes")]
    [SerializeField] SpriteRenderer plotPoint;
    [SerializeField] Color plotColor;
    [SerializeField] Color peakColor;

    float levelLength;
    float songTime;

    public void GenerateVisualiserFromSamples(List<SpectralFluxInfo> _spectralFluxSamples, float _songTime)
    {
        for (int i = 0; i < _spectralFluxSamples.Count; i++)
        {
            SpectralFluxInfo sample = _spectralFluxSamples[i];

            SpriteRenderer spectralPoint = Instantiate(plotPoint, new Vector2(i * spacingBetweenSamples, sample.spectralFlux * heightMultiplier), Quaternion.identity, spectralParent);
            SpriteRenderer prunedSpectralPoint = Instantiate(plotPoint, new Vector2(i * spacingBetweenSamples, sample.prunedSpectralFlux * heightMultiplier), Quaternion.identity, prunedSpectralParent);

            if (sample.isPeak)
            {
                spectralPoint.color = peakColor;
                prunedSpectralPoint.color = peakColor;
            }
            else
            {
                spectralPoint.color = plotColor;
                prunedSpectralPoint.color = plotColor;
            }
        }

        spectralParent.position = new Vector3(0, -3, 0);
        prunedSpectralParent.position = new Vector3(0, 3, 0);

        levelLength = (_spectralFluxSamples.Count * spacingBetweenSamples);
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
