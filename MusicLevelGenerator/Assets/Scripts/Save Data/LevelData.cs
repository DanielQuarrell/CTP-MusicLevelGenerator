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
        SlideBlock,
        Lighting
    }

    [Header("Usage")]
    public int bandIndex;
    public int priority;

    [Header("Feature")]
    public features type;

    [Header("Spacing")]
    public bool placeAdjacent;

    //In units
    public float offset;
    public float preSpace;
    public float postSpace;

    public void AdjustForJumpDistance(float jumpDistance)
    {
        if (preSpace < jumpDistance / 2)
        {
            preSpace = jumpDistance / 2;
        }
        if (postSpace < jumpDistance / 2)
        {
            postSpace = jumpDistance / 2;
        }
    }

    public void CalculateSpaceIndexes(float spacingPerIndex)
    {
        preSpaceIndex = (int)(preSpace / spacingPerIndex);
        postSpaceIndex = (int)(postSpace / spacingPerIndex);
    }

    [HideInInspector] public int preSpaceIndex;
    [HideInInspector] public int postSpaceIndex;
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

    public PhysicsModel physicsModel;

    public List<LevelObjectData> levelObjectData;
    public List<LightingEventData> lightingEventData;

    public List<SpectrumData> spectrumData;
}
