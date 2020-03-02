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

[System.Serializable]
public class LevelObjectData
{
    public LevelFeature feature;
    public int songPositionIndex;

    public LevelObjectData(LevelFeature _feature, int _index)
    {
        feature = _feature;
        songPositionIndex = _index;
    }
}

[System.Serializable]
public class LightingEventData
{
    public int songPositionIndex;
    public Color color;

    public LightingEventData(int _index)
    {
        songPositionIndex = _index;
    }

    public LightingEventData(int _index, Color _color)
    {
        songPositionIndex = _index;
        color = _color;
    }
}

[System.Serializable]
public class LevelData
{
    public string songName;

    public int songIndexLength;
    public float spacingBetweenSamples;
    public float playerOffset;

    public float songTime;
    public float levelLength;
    public float platformScale;

    public List<LevelObjectData> levelObjectData;
    public List<LightingEventData> lightingEventData;
}
