using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(SongController))]
public class SongControllerUI : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SongController songController = (SongController)target;

        if (GUILayout.Button("Load Song File"))
        {
            songController.LoadSongData();
        }

        if (GUILayout.Button("Save Song File"))
        {
            songController.SaveToFile();
        }
    }
}