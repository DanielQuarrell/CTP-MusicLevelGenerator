using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] GameObject spikePrefab;
    [SerializeField] Transform level;

    [SerializeField] float spacingBetweenSamples;
    [SerializeField] float levelOffset;

    public Rigidbody2D playerRigidbody;
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
                Instantiate(spikePrefab, new Vector2(i * spacingBetweenSamples, level.position.y), Quaternion.identity ,level);
            }
        }

        levelLength = (_spectralFluxSamples.Count * spacingBetweenSamples);
        songTime = _songTime;
    }

    public void UpdatePlayerPosition(float currentTime)
    {
        float percentageThroughLevel = Mathf.InverseLerp(0, songTime, currentTime);

        //Can be changed to move the player through the level
        float playerXPosition = Mathf.Lerp(0.0f, levelLength, percentageThroughLevel) + levelOffset;

        playerRigidbody.MovePosition(new Vector2(playerXPosition, playerRigidbody.position.y));
    }
}
