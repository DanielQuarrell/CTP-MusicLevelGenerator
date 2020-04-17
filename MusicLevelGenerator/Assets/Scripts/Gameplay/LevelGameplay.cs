using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.IO;

public class LevelGameplay : MonoBehaviour
{
    static public LevelGameplay instance;

    [Header("Level Song")]
    [SerializeField] TextAsset levelJsonFile;

    [Header("Level Features")]
    [SerializeField] Image levelBackground;
    [SerializeField] Transform platform;
    [SerializeField] AudioSource audioSource;

    [Header("Frequncy Bars")]
    [SerializeField] GameObject barPrefab;
    [SerializeField] RectTransform barHolder;

    [Header("Prefabs")]
    [SerializeField] Transform levelTransform;
    [SerializeField] GameObject spikePrefab;
    [SerializeField] GameObject slideBlockPrefab;

    [Header("Level options")]
    float spacingBetweenSamples = 0.25f;
    float playerOffset = 0f;
    float loadingTime = 0f;

    [Header("Player objects")]
    [SerializeField] Player player;
    [SerializeField] Transform playerTransform;
    [SerializeField] FollowObject currentTimeMarker;

    LevelData loadedLevel;  //Loaded level from file
    
    List<LevelObject> levelObjects;
    List<LightingEventData> lightingEvents;
    List<SpectrumData> spectrumData;
    
    float playerOffsetDistance; //Distance offset from all level objects
    float numberOfBars;         //Number of frequency bars
    float songTime = 0;        //Length of song in seconds
    float currentTime;
    float levelLength = 0;     //Length of level in units
    bool songStarted = false;
    int songIndexLength = 0;   //Number of recorded samples in the song
    
    List<FrequencyBar> bars; //Bars to visually display FFT

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
    }

    private void Start()
    {
        LoadLevel();
        ResetLevel();
    }

    // Level/Song loading ----------------------------

    public void LoadLevel()
    {
        if (levelJsonFile != null)
        {
            levelObjects = new List<LevelObject>();

            string data = levelJsonFile.text;
            LevelData levelData = JsonUtility.FromJson<LevelData>(data);

            audioSource.clip = Resources.Load<AudioClip>("Audio/" + levelData.songName);

            songIndexLength = levelData.songIndexLength;
            spacingBetweenSamples = levelData.spacingBetweenSamples;
            playerOffset = levelData.playerOffset;

            songTime = levelData.songTime;
            levelLength = levelData.levelLength;
            platform.localScale = new Vector3(levelData.platformScale, 1, 1);

            for (int i = 0; i < levelData.levelObjectData.Count; i++)
            {
                LevelObject levelObject = new LevelObject();

                GameObject levelObjectPrefab = null;
                switch (levelData.levelObjectData[i].feature.type)
                {
                    case LevelFeature.features.Spikes:
                        levelObjectPrefab = spikePrefab;
                        break;
                    case LevelFeature.features.SlideBlock:
                        levelObjectPrefab = slideBlockPrefab;
                        break;
                    case LevelFeature.features.DestructableWalls:
                        break;
                }

                if (levelObject != null)
                {
                    levelObject.prefab = Instantiate(levelObjectPrefab, new Vector2(levelData.levelObjectData[i].songPositionIndex * levelData.spacingBetweenSamples + levelData.levelObjectData[i].feature.offset, levelTransform.position.y), Quaternion.identity, levelTransform);
                    levelObject.feature = levelData.levelObjectData[i].feature;
                    levelObject.songPositionIndex = levelData.levelObjectData[i].songPositionIndex;
                    levelObjects.Add(levelObject);
                }
            }

            lightingEvents = levelData.lightingEventData;

            spectrumData = levelData.spectrumData;
            numberOfBars = levelData.spectrumData[0].spectrum.Length;

            loadedLevel = levelData;
        }
    }

    //Gameplay----------------------------

    private void FixedUpdate()
    {
        if (songStarted)
        {
            UpdateSongPosition(audioSource.time);
            UpdatePlayerVelocity();
        }
    }

    private void UpdatePlayerVelocity()
    {
        currentTime += Time.fixedDeltaTime;
        float playerPosition = Mathf.Lerp(0.0f, levelLength, Mathf.Clamp(currentTime / songTime, 0f, 1f)) + playerOffsetDistance;
        playerTransform.position = new Vector2(playerPosition, 0);
    }

    public void UpdateSongPosition(float currentTime)
    {
        int currentIndex = (int)(songIndexLength * (currentTime / songTime));

        if (currentIndex >= songIndexLength)
        {
            ResetLevel();
            return;
        }

        if (lightingEvents != null)
        {
            LightingEventData lightingEvent = LightingEventAtPosition(currentIndex);

            if (lightingEvent != null)
            {
                levelBackground.DOKill();
                levelBackground.DOFade(1f, 0.1f).OnComplete(FadeBack);
            }
        }

        if (spectrumData != null)
        {
            for (int i = 0; i < bars.Count; i++)
            {
                Vector2 fillAmount = Vector2.one;
                fillAmount.y = Mathf.Clamp(spectrumData[currentIndex].spectrum[i] * 80, 0.0f, 1.0f);
                if (fillAmount.y < bars[i].fill.anchorMax.y)
                {
                    fillAmount.y = Mathf.Clamp(bars[i].fill.anchorMax.y - Time.deltaTime, 0.0f, 1.0f);
                    bars[i].fill.anchorMax = fillAmount;
                }
                else
                {
                    bars[i].fill.DOAnchorMax(fillAmount, duration: 0);
                }
            }
        }
    }

    private void FadeBack()
    {
        levelBackground.DOFade(0f, 1f);
    }

    public void StartLevelAtPosition(int timeIndex)
    {
        audioSource.Stop();

        CalculatePlayerOffset(timeIndex);

        //Set marker position
        MoveTimeMarker(timeIndex);
        currentTimeMarker.offset = new Vector2(-playerOffsetDistance + (timeIndex * spacingBetweenSamples),
                                                currentTimeMarker.transform.localPosition.y);
        currentTimeMarker.SetTransformWithOffset(playerTransform);

        CreateFrequencyBars();

        //Reset song time and player position
        currentTime = songTime * ((float)timeIndex / (float)songIndexLength);
        audioSource.time = currentTime;

        audioSource.Play();

        if (loadedLevel != null)
        {
            songStarted = true;
        }
        else
        {
            Debug.LogError("Level file not loaded");
            songStarted = false;
        }
    }

    private void CalculatePlayerOffset(int timeIndex)
    {
        float distancePerSecond = levelLength / songTime;
        playerOffsetDistance = playerOffset * distancePerSecond;

        Vector2 playerPos = player.transform.position;
        playerPos.y = -2.5f;
        player.transform.position = playerPos;

        playerTransform.position = new Vector2(playerOffsetDistance + (timeIndex * spacingBetweenSamples), 0);
    }

    private void CreateFrequencyBars()
    {
        if (bars != null)
        {
            for (int i = 0; i < bars.Count; i++)
            {
                bars[i].fill.DOKill();
                Destroy(bars[i].gameObject);
            }

            bars.Clear();
        }

        bars = new List<FrequencyBar>();

        float increment = barHolder.rect.width / (numberOfBars + 2);

        for (int i = 0; i < numberOfBars; i++)
        {
            FrequencyBar bar = Instantiate(barPrefab, barHolder.transform).GetComponent<FrequencyBar>();

            Vector3 position = bar.rectTransform.localPosition;
            position.x = increment * (i + 1);
            position.y = 0;
            bar.rectTransform.localPosition = position;

            bars.Add(bar);
        }
    }

    LightingEventData LightingEventAtPosition(int index)
    {
        return lightingEvents.FirstOrDefault(lightEvent => lightEvent.songPositionIndex == index);
    }

    private void MoveTimeMarker(int timeIndex)
    {
        float levelPosition = (float)timeIndex;
        levelPosition *= spacingBetweenSamples;

        Vector3 timeMarkerPosition = currentTimeMarker.transform.position;
        timeMarkerPosition.x = levelPosition;
        currentTimeMarker.transform.position = timeMarkerPosition;
    }

    public void ResetLevel()
    {
        StartLevelAtPosition(0);
    }
}

