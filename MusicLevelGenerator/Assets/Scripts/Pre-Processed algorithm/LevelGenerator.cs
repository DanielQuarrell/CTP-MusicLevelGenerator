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

    [SerializeField] Transform playerTransform;
    [SerializeField] GameObject currentTime;

    [SerializeField] Rigidbody2D player;
    [SerializeField] FollowPlayer currentTimeMarker;

    float levelLength = 0;
    float songTime = 0;
    float playerVelocityX = 0;

    bool onFirstUpdate = true;

    public void GenerateLevelFromSamples(FrequencyBand[] frequencyBands, float _songTime)
    {
        foreach(LevelFeature levelFeature in levelFeatures)
        {
            switch(levelFeature.type)
            {
                case LevelFeature.features.Spikes:
                    CreateLevelSpikes(frequencyBands[levelFeature.bandIndex]);
                    levelLength = (frequencyBands[levelFeature.bandIndex].spectralFluxSamples.Count * spacingBetweenSamples);
                    break;
                case LevelFeature.features.DestructableWalls:
                    break;
                case LevelFeature.features.LevelHeight:
                    break;
            }
        }
        
        songTime = _songTime;

        //Old marker
        Vector2 currentTimePosition = new Vector2(-playerOffset, currentTime.transform.localPosition.y);
        currentTime.transform.localPosition = currentTimePosition;

        //New marker
        currentTimeMarker.offset = new Vector2(-playerOffset, currentTimeMarker.transform.localPosition.y);

        playerVelocityX = levelLength / songTime;
        player.velocity = new Vector2(playerVelocityX, player.velocity.y);
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
    }

    private void FixedUpdate()
    {
        player.velocity = new Vector2(playerVelocityX, player.velocity.y);
    }

    public void UpdatePlayerPosition(float currentTime)
    {
        float percentageThroughLevel = Mathf.InverseLerp(0, songTime, currentTime);

        //Can be changed to move the player through the level
        float playerXPosition = Mathf.Lerp(0.0f, levelLength, percentageThroughLevel) + playerOffset;

        playerTransform.position = new Vector2(playerXPosition, 0);

        if(onFirstUpdate && currentTime != 0)
        {
            player.MovePosition(new Vector2(playerXPosition + 0.175f, 0));
            onFirstUpdate = false;
        }
    }

    public void ResetLevel()
    {
        onFirstUpdate = true;
        player.position = new Vector2(0, 0);
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
