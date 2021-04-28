using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Editor : EditorWindow
{
    private bool optionalEnabled;
    private bool voidGenTriggered = false;
    private bool roomGenTriggered = false;
    private float testFloat;
    private Vector2 mapLimits;

    private MapGenerator GenManager;

    // Adds menu item to the window
    [MenuItem ("Window/Dungeon Generator")]

    // Shows existing window instance. If one doesn't exist, make one.
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(Editor));
    }

    private void OnGUI()
    {
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);

        GenManager = (MapGenerator)EditorGUILayout.ObjectField(new GUIContent("Map Generation Manager", "The object in scene that contains the MapGeneration script."), GenManager, typeof(MapGenerator), true);

        GUILayout.Label("Generation", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();

        // Button to trigger room generation
        if (GUILayout.Button(new GUIContent("Generate Void Space", "Randomly places tiles to create void space.")))
        {
            Debug.Log("GENERATING VOID SPACE");

            voidGenTriggered = true;
            GenManager.CreateVoidTiles();
        }

        if (voidGenTriggered)
        {
            // Button to trigger room generation
            if (GUILayout.Button(new GUIContent("Generate Rooms", "Randomly places rooms on the map.")) && voidGenTriggered)
            {
                Debug.Log("GENERATING ROOMS");

                roomGenTriggered = true;
                GenManager.GenerateRooms();
            }
        }

        if (roomGenTriggered)
        {
            // Button to trigger corridor generation
            if (GUILayout.Button(new GUIContent("Generate Corridors", "Randomly places corridor on the map. NOTE: Generate after rooms have been created.")))
            {
                Debug.Log("GENERATING CORRIDORS");

                GenManager.GenerateCorridors();
            }
        }

        GUILayout.EndHorizontal();

        mapLimits = EditorGUILayout.Vector2Field(new GUIContent("Map Limits", "Changes the size of the generated map"), mapLimits);
        GenManager.SetMapSize(mapLimits);

        // Button to reset map
        if (GUILayout.Button(new GUIContent("Reset", "Removes all components of map")))
        {
            Debug.Log("RESET MAP");

            voidGenTriggered = false;
            roomGenTriggered = false;
            GenManager.ResetMap();
        }

        optionalEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", optionalEnabled);
        testFloat = EditorGUILayout.Slider("Slider", testFloat, -3, 3);
        EditorGUILayout.EndToggleGroup();
    }
}