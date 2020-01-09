using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelFeature
{
    public enum features
    {
        Spikes,
        DestructableWalls,
        LevelHeight
    }

    public int bandIndex;
    public features type;
}

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] LevelFeature[] levelFeatures;

    [SerializeField] GameObject spikePrefab;
    [SerializeField] Transform level;

    [SerializeField] float spacingBetweenSamples = 0.25f;
    [SerializeField] float playerOffset = 0f;

    public Transform playerTransform;

    float levelLength = 0;
    float songTime = 0;

    public void GenerateLevelFromSamples(FrequencyBand[] frequencyBands, float _songTime)
    {
        foreach(LevelFeature levelFeature in levelFeatures)
        {
            switch(levelFeature.type)
            {
                case LevelFeature.features.Spikes:
                    CreateLevelSpikes(frequencyBands[levelFeature.bandIndex]);
                    break;
                case LevelFeature.features.DestructableWalls:
                    break;
                case LevelFeature.features.LevelHeight:
                    break;
            }
        }
        
        songTime = _songTime;

        GameObject currentTime = playerTransform.Find("CurrentTime").gameObject;
        Vector2 currentTimePosition = new Vector2(-playerOffset, currentTime.transform.localPosition.y);
        currentTime.transform.localPosition = currentTimePosition;
    }

    public void CreateLevelSpikes(FrequencyBand band)
    {
        int iterationsSinceLast = 0;

        for (int i = 0; i < band.spectralFluxSamples.Count; i++)
        {
            SpectralFluxData sample = band.spectralFluxSamples[i];

            if (sample.isPeak && iterationsSinceLast >= 8)
            {
                Instantiate(spikePrefab, new Vector2(i * spacingBetweenSamples, level.position.y), Quaternion.identity, level);
                iterationsSinceLast = 0;
            }
            else
            {
                iterationsSinceLast++;
            }
        }

        //TestLevelGeneration(_spectralFluxSamples.Count);
        levelLength = (band.spectralFluxSamples.Count * spacingBetweenSamples);
    }

    public void UpdatePlayerPosition(float currentTime)
    {
        float percentageThroughLevel = Mathf.InverseLerp(0, songTime, currentTime);

        //Can be changed to move the player through the level
        float playerXPosition = Mathf.Lerp(0.0f, levelLength, percentageThroughLevel) + playerOffset;

        playerTransform.position = new Vector2(playerXPosition, 0);
    }

    private void TestLevelGeneration(float _spectralfluxSampleCount)
    {
        //3 iterations equals one triangle length at 0.25f

        for (int i = 0; i < _spectralfluxSampleCount; i++)
        {
            if (i % 12 == 0)
            {
                Instantiate(spikePrefab, new Vector2(i * spacingBetweenSamples, level.position.y), Quaternion.identity, level);
            }
        }
    }
}
