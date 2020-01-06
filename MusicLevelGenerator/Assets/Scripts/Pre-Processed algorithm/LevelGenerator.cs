using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] GameObject spikePrefab;
    [SerializeField] Transform level;

    [SerializeField] float spacingBetweenSamples = 0.25f;
    [SerializeField] float playerOffset = 0f;

    public Transform playerTransform;

    float levelLength = 0;
    float songTime = 0;

    public void GenerateLevelFromSamples(List<SpectralFluxData> _spectralFluxSamples, float _songTime)
    {
        int iterationsSinceLast = 0;

        for (int i = 0; i < _spectralFluxSamples.Count; i++)
        {
            SpectralFluxData sample = _spectralFluxSamples[i];
            
            if(sample.isPeak && iterationsSinceLast >= 12)
            {
                Instantiate(spikePrefab, new Vector2(i * spacingBetweenSamples, level.position.y), Quaternion.identity ,level);
                iterationsSinceLast = 0;
            }
            else
            {
                iterationsSinceLast++;
            }
        }

        //TestLevelGeneration(_spectralFluxSamples.Count);

        levelLength = (_spectralFluxSamples.Count * spacingBetweenSamples);
        songTime = _songTime;

        GameObject currentTime = playerTransform.Find("CurrentTime").gameObject;
        Vector2 currentTimePosition = new Vector2(-playerOffset, currentTime.transform.localPosition.y);
        currentTime.transform.localPosition = currentTimePosition;
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
