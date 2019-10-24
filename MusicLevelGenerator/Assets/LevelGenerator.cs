using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] GameObject spikePrefab;
    [SerializeField] Transform level;

    [SerializeField] float spacingBetweenSamples;
    [SerializeField] float levelOffset;

    Rigidbody2D levelRigidbody;

    float levelLength = 0;
    float songTime = 0;

    private void Start()
    {
        levelRigidbody = level.GetComponent<Rigidbody2D>();
    }

    public void GenerateLevelFromSamples(List<SpectralFluxInfo> _spectralFluxSamples, float _songTime)
    {
        for (int i = 0; i < _spectralFluxSamples.Count; i++)
        {
            SpectralFluxInfo sample = _spectralFluxSamples[i];
            
            if(sample.isPeak)
            {
                Instantiate(spikePrefab, new Vector2(i * spacingBetweenSamples + levelOffset, level.position.y), Quaternion.identity ,level);
            }
        }

        levelLength = (_spectralFluxSamples.Count * spacingBetweenSamples) + levelOffset;
        songTime = _songTime;
    }

    public void UpdateLevelPosition(float currentTime)
    {
        float percentageThroughLevel = Mathf.InverseLerp(levelOffset, songTime, currentTime);

        //Can be changed to move the player through the level
        float levelXPosition = Mathf.Lerp(0.0f, -levelLength, percentageThroughLevel);

        levelRigidbody.MovePosition(new Vector2(levelXPosition, level.position.y));
    }
}
