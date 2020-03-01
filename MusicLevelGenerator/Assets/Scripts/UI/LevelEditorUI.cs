using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelGenerator))]
public class LevelEditorUI : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelGenerator levelGenerator = (LevelGenerator)target;

        if (GUILayout.Button("Generate Level"))
        {
            levelGenerator.GenerateLevel();
        }

        if (GUILayout.Button("Load Level"))
        {
            levelGenerator.LoadLevel();
        }

        if (GUILayout.Button("Remove Level"))
        {
            levelGenerator.RemoveLevel();
        }

        if (GUILayout.Button("Save Level"))
        {
            levelGenerator.SaveLevel();
        }
    }
}
