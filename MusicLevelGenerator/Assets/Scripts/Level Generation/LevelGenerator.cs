using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[System.Serializable]
public class LevelFeature
{
    public enum features
    {
        Spikes,
        DestructableWalls,
        LevelHeight,
        DuckBlock,
        Lighting
    }

    [Header("Usage")]
    public int bandIndex;
    public int priority;

    [Header("Feature")]
    public features type;

    [Header("Spacing")]
    public bool placeAdjacent;
    public int offset;
    public int preSpace;
    public int postSpace;
}

public class LevelObject
{
    public LevelFeature feature;
    public GameObject gameObject;
}

public class LevelGenerator : MonoBehaviour
{
    static public LevelGenerator instance;

    [Header("Level Song")]
    [SerializeField] TextAsset songJsonFile;

    [Header("Level Features")]
    [SerializeField] LevelFeature[] levelFeatures;
    [SerializeField] Image levelBackground;
    [SerializeField] Transform platform;

    [Header("Prefabs")]
    [SerializeField] Transform level;
    [SerializeField] GameObject spikePrefab;
    [SerializeField] GameObject duckBlockPrefab;

    [Header("Level options")]
    [SerializeField] float spacingBetweenSamples = 0.25f;
    [SerializeField] float playerOffset = 0f;
    [SerializeField] float loadingDistance = 0f;

    [Header("Player objects")]
    [SerializeField] Transform playerTransform;

    [SerializeField] Rigidbody2D player;
    [SerializeField] FollowPlayer currentTimeMarker;

    AudioSource audioSource;

    LevelObject[] levelObjects;
    bool[] ligthingEvents;

    float levelLength = 0;
    float songTime = 0;
    float playerVelocityX = 0;

    bool onFirstUpdate = false;
    bool paused = false;
    bool songStarted = false;

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

        audioSource = GetComponent<AudioSource>();
    }

    public void GenerateLevel()
    {
        RemoveLevel();

        string data = songJsonFile.text;
        SongData songData = JsonUtility.FromJson<SongData>(data);

        GenerateLevelFromSamples(songData.frequencyBands.ToArray(), songData.clipLength);

        audioSource.clip = Resources.Load<AudioClip>("Audio/" + songData.songName);
    }

    public void RemoveLevel()
    {
        //Scale level to the length of the song
        platform.localScale = new Vector3(15, 1, 1);

        if (levelObjects != null)
        {
            foreach (LevelObject levelObject in levelObjects)
            {
                if(levelObject != null)
                {
                    if (levelObject.gameObject != null)
                    {
                        GameObject.DestroyImmediate(levelObject.gameObject);
                    }
                }
            }

            Array.Clear(levelObjects, 0, levelObjects.Length);  
        }

        foreach (Transform child in level.transform)
        {
            GameObject.DestroyImmediate(child.gameObject);
        }
    }

    public void LoadLevel()
    {

    }

    public void SaveLevel()
    {

    }

    private void Start()
    {
        StartCoroutine(StartSong());
    }

    public void GenerateLevelFromSamples(FrequencyBand[] frequencyBands, float _songTime)
    {
        levelObjects = new LevelObject[frequencyBands[0].spectralFluxSamples.Count];
        levelLength = (frequencyBands[0].spectralFluxSamples.Count * spacingBetweenSamples);

        //Scale level to the length of the song
        platform.localScale = new Vector3 (levelLength + 7, 1, 1);

        Array.Sort(levelFeatures, delegate (LevelFeature feature1, LevelFeature feature2) { return feature1.priority.CompareTo(feature2.priority); });

        foreach (LevelFeature levelFeature in levelFeatures)
        {
            switch(levelFeature.type)
            {
                case LevelFeature.features.Spikes:
                    CreateLevelObjects(spikePrefab, frequencyBands[levelFeature.bandIndex], levelFeature);
                    break;
                case LevelFeature.features.DuckBlock:
                    CreateLevelObjects(duckBlockPrefab, frequencyBands[levelFeature.bandIndex], levelFeature);
                    break;
                case LevelFeature.features.DestructableWalls:
                    break;
                case LevelFeature.features.LevelHeight:
                    break;
                case LevelFeature.features.Lighting:
                    CreateLightingEvents(frequencyBands[levelFeature.bandIndex]);
                    break;
            }
        }

        CleanUpLevel();

        songTime = _songTime;

        //Old marker
        //Vector2 currentTimePosition = new Vector2(-playerOffset, currentTime.transform.localPosition.y);
        //currentTime.transform.localPosition = currentTimePosition;
    }

    private void CreateLevelObjects(GameObject levelObjectPrefab, FrequencyBand band, LevelFeature feature)
    {
        int iterationsSinceLast = 0;

        for (int i = 0; i < band.spectralFluxSamples.Count; i++)
        {
            SpectralFluxData sample = band.spectralFluxSamples[i];

            if (sample.isPeak && (iterationsSinceLast >= feature.preSpace || feature.placeAdjacent))
            {
                if (levelObjects[i] == null)
                {
                    levelObjects[i] = new LevelObject();
                    levelObjects[i].gameObject = Instantiate(levelObjectPrefab, new Vector2(i * spacingBetweenSamples + feature.offset, level.position.y), Quaternion.identity, level);
                    levelObjects[i].feature = feature;
                }

                iterationsSinceLast = 0;
            }
            else
            {
                iterationsSinceLast++;
            }
        }
    }

    private void CreateLightingEvents(FrequencyBand band)
    {
        ligthingEvents = new bool[band.spectralFluxSamples.Count];

        for (int i = 0; i < band.spectralFluxSamples.Count; i++)
        {
            SpectralFluxData sample = band.spectralFluxSamples[i];

            if (sample.isPeak)
            {
                ligthingEvents[i] = sample.isPeak;
            }
        }
    }

    private void CleanUpLevel()
    {
        //Check level features in order of priority
        foreach (LevelFeature currentFeature in levelFeatures)
        {
            //Loop through level objects
            for (int l = 0; l < levelObjects.Length; l++)
            {
                LevelObject currentLevelObject = levelObjects[l];
                if (currentLevelObject != null)
                {
                    if (currentLevelObject.feature == currentFeature)
                    {
                        //Check space in front and behind the object 
                        for (int i = l - currentFeature.preSpace; i < l + currentFeature.postSpace; i++)
                        {
                            if (0 <= i && i < levelObjects.Length)
                            {
                                if (levelObjects[i] != null)
                                {
                                    if (levelObjects[i].feature != currentFeature)
                                    {
                                        DestroyImmediate(levelObjects[i].gameObject);
                                        levelObjects[i].feature = null;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (songStarted)
        {
            UpdatePlayerVelocity();
            UpdateSongPosition(audioSource.time);

            /*
            for (int i = 0; i < levelObjects.Length; i++)
            {
                if(levelObjects[i] != null)
                {
                    if (levelObjects[i].gameObject != null)
                    {
                        Vector2 distance = player.position - (Vector2)levelObjects[i].gameObject.transform.position;
                        levelObjects[i].gameObject.SetActive(distance.magnitude < loadingDistance);
                    }
                }
            }
            */
        }
    }

    private void UpdatePlayerVelocity()
    {
        if(!paused)
        {
            player.velocity = new Vector2(playerVelocityX, player.velocity.y);
        }
        else
        {
            player.velocity = Vector2.zero;
        }
    }

    public void UpdateSongPosition(float currentTime)
    {
        float percentageThroughLevel = Mathf.InverseLerp(0, songTime, currentTime);

        //Can be changed to move the player through the level
        float playerXPosition = Mathf.Lerp(0.0f, levelLength, percentageThroughLevel) + playerOffset;

        playerTransform.position = new Vector2(playerXPosition, 0);

        int currentIndex = (int)(levelObjects.Length * (currentTime / songTime));

        if (ligthingEvents != null)
        {
            if(ligthingEvents[currentIndex])
            {
                levelBackground.DOKill();
                levelBackground.DOFade(1f, 0.1f).OnComplete(FadeBack);
            }
        }

        if(onFirstUpdate && currentTime != 0)
        {
            player.MovePosition(new Vector2(playerXPosition + 0.175f, 0));
            onFirstUpdate = false;
        }
    }

    void FadeBack()
    {
        levelBackground.DOFade(0f, 1f);
    }

    public void ResetLevel()
    {
        audioSource.Stop();

        onFirstUpdate = true;
        player.position = new Vector2(0, 0);

        //New marker
        currentTimeMarker.offset = new Vector2(-playerOffset, currentTimeMarker.transform.localPosition.y);

        playerVelocityX = levelLength / songTime;
        player.velocity = new Vector2(playerVelocityX, player.velocity.y);

        audioSource.Play();
        songStarted = true;
    }

    //TestLevelGeneration(_spectralFluxSamples.Count);
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

    IEnumerator StartSong()
    {
        yield return new WaitForSeconds(0.1f);

        ResetLevel();
    }
}
