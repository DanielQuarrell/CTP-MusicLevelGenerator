using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.IO;

public class LevelObject
{
    public GameObject gameObject;
    public LevelFeature feature;
    public int songPositionIndex;
}

public class LevelGenerator : MonoBehaviour
{
    static public LevelGenerator instance;

    [Header("Level Song")]
    [SerializeField] TextAsset songJsonFile;
    [SerializeField] TextAsset levelJsonFile;

    [Header("Level Features")]
    [SerializeField] LevelFeature[] levelFeatures;
    [SerializeField] Image levelBackground;
    [SerializeField] Transform platform;
    [SerializeField] AudioSource audioSource;

    [Header("Frequncy Bars")]
    [SerializeField] GameObject barPrefab;
    [SerializeField] RectTransform barHolder;

    [Header("Prefabs")]
    [SerializeField] Transform levelTransform;
    [SerializeField] GameObject spikePrefab;
    [SerializeField] GameObject duckBlockPrefab;

    [Header("Level options")]
    [SerializeField] float spacingBetweenSamples = 0.25f;
    [SerializeField] float playerOffset = 0f;
    [SerializeField] float loadingTime = 0f;

    [Header("Player objects")]
    [SerializeField] Player player;
    [SerializeField] Transform playerTransform;
    [SerializeField] FollowPlayer currentTimeMarker;    

    //Variables to keep loaded in editor
    [HideInInspector] public float songTime = 0;        //Length of song in seconds
    [HideInInspector] public float levelLength = 0;     //Length of level in units
    [HideInInspector] public int songIndexLength = 0;   //Number of recorded samples in the song

    [HideInInspector] public List<LevelObject> levelObjects;            
    [HideInInspector] public List<LightingEventData> lightingEvents;    

    [HideInInspector] public PhysicsModel physicsModel;

    [HideInInspector] public List<SpectrumData> spectrumData;

    [HideInInspector] public float numberOfBars;

    LevelData loadedLevel;  //Loaded level from file

    List<FrequencyBar> bars; //Bars to visually display FFT

    float playerOffsetDistance; //Distance offset from all level objects

    float currentTime;
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
    }

    public void GenerateLevel()
    {
        if(songJsonFile != null)
        {
            RemoveLevel();

            string data = songJsonFile.text;
            SongData songData = JsonUtility.FromJson<SongData>(data);

            GenerateLevelFromSamples(songData.frequencyBands.ToArray(), songData.clipLength);

            spectrumData = songData.spectrumData;

            numberOfBars = songData.spectrumData[0].spectrum.Length;

            audioSource.clip = Resources.Load<AudioClip>("Audio/" + songData.songName);
        }
        else
        {
            Debug.LogError("Song file needs to be added to generate a level");
        }
    }

    public void RemoveLevel()
    {
        //Scale level to the length of the song
        platform.localScale = new Vector3(22, 1, 1);

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

            levelObjects.Clear();
        }

        foreach (Transform child in levelTransform.transform)
        {
            GameObject.DestroyImmediate(child.gameObject);
        }
    }

    public void LoadLevel()
    {
        if (levelJsonFile != null)
        {
            RemoveLevel();

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

            physicsModel = levelData.physicsModel;

            for (int i = 0; i < levelData.levelObjectData.Count; i++)
            {
                LevelObject levelObject = new LevelObject();

                GameObject levelObjectPrefab = null;
                switch (levelData.levelObjectData[i].feature.type)
                {
                    case LevelFeature.features.Spikes:
                        levelObjectPrefab = spikePrefab;
                        break;
                    case LevelFeature.features.DuckBlock:
                        levelObjectPrefab = duckBlockPrefab;
                        break;
                    case LevelFeature.features.DestructableWalls:
                        break;
                }

                if(levelObject != null)
                {
                    levelObject.gameObject = Instantiate(levelObjectPrefab, new Vector2(levelData.levelObjectData[i].songPositionIndex * levelData.spacingBetweenSamples + levelData.levelObjectData[i].feature.offset, levelTransform.position.y), Quaternion.identity, levelTransform);
                    levelObject.feature = levelData.levelObjectData[i].feature;
                    levelObjects.Add(levelObject);
                }
            }

            lightingEvents = levelData.lightingEventData;
            spectrumData = levelData.spectrumData;

            numberOfBars = levelData.spectrumData[0].spectrum.Length;

            loadedLevel = levelData;
        }
    }

    public void SaveLevel()
    {
        LevelData levelData = new LevelData();

        levelData.songName = audioSource.clip.name;

        levelData.songIndexLength = songIndexLength;
        levelData.spacingBetweenSamples = spacingBetweenSamples;
        levelData.playerOffset = playerOffset;

        levelData.songTime = songTime;
        levelData.levelLength = levelLength;
        levelData.platformScale = platform.localScale.x;

        levelData.physicsModel = physicsModel;

        levelData.levelObjectData = new List<LevelObjectData>();

        for (int i = 0; i < levelObjects.Count; i++)
        {
            levelData.levelObjectData.Add(new LevelObjectData(levelObjects[i].feature, levelObjects[i].songPositionIndex));
        }

        levelData.lightingEventData = new List<LightingEventData>(lightingEvents);

        levelData.spectrumData = new List<SpectrumData>(spectrumData);

        string data = string.Empty;
        data = JsonUtility.ToJson(levelData, true);

        if (File.Exists("Assets/Resources/LevelDataFiles/" + levelData.songName + "_Level.json"))
        {
            File.Delete("Assets/Resources/LevelDataFiles/" + levelData.songName + "_Level.json");
        }

        File.WriteAllText("Assets/Resources/LevelDataFiles/" + levelData.songName + "_Level.json", data);
        Debug.Log("Saved level data to: " + "Assets/Resources/LevelDataFiles/" + levelData.songName + "_Level.json");
    }

    public void GenerateLevelFromSamples(FrequencyBand[] frequencyBands, float _songTime)
    {
        levelObjects = new List<LevelObject>();
        songIndexLength = frequencyBands[0].spectralFluxSamples.Count;
        levelLength = (songIndexLength * spacingBetweenSamples);

        songTime = _songTime;

        //Scale level to the length of the song
        platform.localScale = new Vector3 (levelLength + 15, 1, 1);

        physicsModel = new PhysicsModel();

        //Set physics model
        physicsModel.gravity = Mathf.Abs(Physics2D.gravity.y * player.rigidbody.gravityScale);
        physicsModel.velocity = levelLength / songTime;
        physicsModel.jumpAcceleration = player.GetJumpAcceleration();
        physicsModel.CalculatePhysicsModel();

        //Sort level based on priority
        Array.Sort(levelFeatures, delegate (LevelFeature feature1, LevelFeature feature2) { return feature1.priority.CompareTo(feature2.priority); });

        for (int i = 0; i < levelFeatures.Length; i++)
        {
            switch (levelFeatures[i].type)
            {
                case LevelFeature.features.Spikes:
                    CreateLevelObjects(spikePrefab, frequencyBands[levelFeatures[i].bandIndex], ref levelFeatures[i]);
                    break;
                case LevelFeature.features.DuckBlock:
                    CreateLevelObjects(duckBlockPrefab, frequencyBands[levelFeatures[i].bandIndex], ref levelFeatures[i]);
                    break;
                case LevelFeature.features.DestructableWalls:
                    break;
                case LevelFeature.features.LevelHeight:
                    break;
                case LevelFeature.features.Lighting:
                    CreateLightingEvents(frequencyBands[levelFeatures[i].bandIndex]);
                    break;
            }
        }

        CleanUpLevel();

        SaveLevel();

        //Old marker
        //Vector2 currentTimePosition = new Vector2(-playerOffset, currentTime.transform.localPosition.y);
        //currentTime.transform.localPosition = currentTimePosition;
    }

    private void CreateLevelObjects(GameObject levelObjectPrefab, FrequencyBand band, ref LevelFeature feature)
    {
        int iterationsSinceLast = 0;

        feature.AdjustForJumpDistance(physicsModel.jumpDistance);
        feature.CalculateSpaceIndexes(spacingBetweenSamples);

        for (int i = 0; i < band.spectralFluxSamples.Count; i++)
        {
            SpectralFluxData sample = band.spectralFluxSamples[i];

            //TODO: Change to use physics model
            if (sample.isPeak && (iterationsSinceLast >= feature.preSpaceIndex || feature.placeAdjacent))
            {
                LevelObject levelObject = LevelObjectAtPosition(i);

                if (levelObject == null)
                {
                    levelObject = new LevelObject();
                    levelObject.gameObject = Instantiate(levelObjectPrefab, new Vector2(i * spacingBetweenSamples + feature.offset, levelTransform.position.y), Quaternion.identity, levelTransform);
                    levelObject.feature = feature;
                    levelObject.songPositionIndex = i;

                    levelObjects.Add(levelObject);
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
        lightingEvents = new List<LightingEventData>();

        for (int i = 0; i < band.spectralFluxSamples.Count; i++)
        {
            SpectralFluxData sample = band.spectralFluxSamples[i];

            if (sample.isPeak)
            {
                lightingEvents.Add(new LightingEventData(i));
            }
        }
    }

    private void CleanUpLevel()
    {
        //Check level features in order of priority
        foreach (LevelFeature currentFeature in levelFeatures)
        {
            List<LevelObject> objectsToRemove = new List<LevelObject>();

            foreach (LevelObject levelObject in levelObjects)
            {
                if (levelObject.feature == currentFeature)
                {
                    //Check space in front and behind the object 
                    for (int i = levelObject.songPositionIndex - currentFeature.preSpaceIndex; i < levelObject.songPositionIndex + currentFeature.postSpaceIndex; i++)
                    {
                        LevelObject objectToCheck = LevelObjectAtPosition(i);
                         
                        if (objectToCheck != null)
                        {
                            if (objectToCheck.feature != currentFeature)
                            {
                                DestroyImmediate(objectToCheck.gameObject);
                                objectsToRemove.Add(objectToCheck);
                            }
                        }
                    }
                }
            }

            //Remove features with lower priority that are within spacing
            foreach (LevelObject levelObject in objectsToRemove)
            {
                levelObjects.Remove(levelObject);
            }
        }
    }

    private void Start()
    {
        LoadLevel();

        StartCoroutine(StartSong());
    }

    private void FixedUpdate()
    {
        if (songStarted)
        {
            UpdateSongPosition(audioSource.time);

            currentTime += Time.deltaTime;
            UpdatePlayerVelocity();

            /*
             * Enable / disable objects on whether or not they are in view
             * 
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
            float playerPosition = Mathf.Lerp(0.0f, levelLength, Mathf.Clamp(currentTime / songTime, 0f, 1f)) + playerOffsetDistance;
            playerTransform.position = new Vector2(playerPosition, 0);
        }
        else
        {
        }
    }

    public void UpdateSongPosition(float currentTime)
    {
        int currentIndex = (int)(songIndexLength * (currentTime / songTime));

        if (lightingEvents != null)
        {
            LightingEventData lightingEvent = LightingEventAtPosition(currentIndex);

            if (lightingEvent != null)
            {
                levelBackground.DOKill();
                levelBackground.DOFade(1f, 0.1f).OnComplete(FadeBack);
            }
        }

        if(spectrumData != null)
        {
            for (int i = 0; i < bars.Count; i++)
            {
                Vector2 fillAmount = Vector2.one;
                fillAmount.y = Mathf.Clamp(spectrumData[currentIndex].spectrum[i] * 100, 0.0f, 1.0f);
                if(fillAmount.y < bars[i].fill.anchorMax.y)
                {
                    fillAmount.y = bars[i].fill.anchorMax.y - Time.deltaTime;
                    bars[i].fill.anchorMax = fillAmount;
                }
                else
                {
                    bars[i].fill.DOAnchorMax(fillAmount, duration: 0);
                }
            }
        }
    }

    void FadeBack()
    {
        levelBackground.DOFade(0f, 1f);
    }

    public void ResetLevel()
    {
        audioSource.Stop();

        CalculatePlayerOffset();

        CreateFrequencyBars();

        //New marker
        currentTimeMarker.offset = new Vector2(-playerOffsetDistance, currentTimeMarker.transform.localPosition.y);

        //Reset song time and player position
        currentTime = 0;

        audioSource.Play();

        if(loadedLevel != null)
        {
            songStarted = true;
        }
        else
        {
            Debug.LogError("Level file not loaded");
            songStarted = false;
        }
    }

    private void CalculatePlayerOffset()
    {
        float distancePerSecond = levelLength / songTime;
        playerOffsetDistance = playerOffset * distancePerSecond;

        playerTransform.position = new Vector2(playerOffsetDistance, playerTransform.position.y);
    }

    private void CreateFrequencyBars()
    {
        if(bars != null)
        {
            for (int i = 0; i < bars.Count; i++)
            {
                bars[i].fill.DOKill();
                Destroy(bars[i].gameObject);
            }

            bars.Clear();
        }

        bars = new List<FrequencyBar>();

        float increment =  barHolder.rect.width / (numberOfBars + 2);

        for(int i = 0; i < numberOfBars; i++)
        {
            FrequencyBar bar = Instantiate(barPrefab, barHolder.transform).GetComponent<FrequencyBar>();

            Vector3 position = bar.rectTransform.localPosition;
            position.x = increment *(i + 1);
            position.y = 0;
            bar.rectTransform.localPosition = position;

            bars.Add(bar);
        }
    }

    LevelObject LevelObjectAtPosition(int index)
    {
        return levelObjects.FirstOrDefault(levelObject => levelObject.songPositionIndex == index);
    }

    LightingEventData LightingEventAtPosition(int index)
    {
        return lightingEvents.FirstOrDefault(lightEvent => lightEvent.songPositionIndex == index);
    }

    IEnumerator StartSong()
    {
        yield return new WaitForSeconds(0.1f);

        ResetLevel();
    }
}
