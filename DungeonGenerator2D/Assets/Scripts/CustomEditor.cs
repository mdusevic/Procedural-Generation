using UnityEditor;
using UnityEngine;
using System.IO;

public class CustomEditor : EditorWindow
{
    public GameObject managerPrefab;

    private GameObject SceneMapGenObj;

    private MapGenerator MapGenScript;

    private static CustomEditorData m_data;
    public static CustomEditor m_window;

    // Adds menu item to the window
    [MenuItem("Window/Dungeon Generator")]
    // Shows existing window instance. If one doesn't exist, make one.
    public static void Init()
    {
        // Get existing window or if none, make one
        m_window = (CustomEditor)EditorWindow.GetWindow(typeof(CustomEditor));
        m_window.Show();
    }

    public static void LoadEditorData(CustomEditorData a_data)
    {
        m_data = a_data;
    }

    private void OnGUI()
    {
        if (m_data == null)
        {
            return;
        }

        if (!m_data.m_setupComplete)
        {
            GUILayout.Label("Map Generation Manager Prefab", EditorStyles.boldLabel);
            managerPrefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Map Generation Manager Prefab", "Prefab that contains MapGenerator script"), managerPrefab, typeof(GameObject), true);

            if (managerPrefab != null)
            {
                // Button to create generation manager in scene
                if (GUILayout.Button(new GUIContent("Setup", "Adds scripts to in scene object to run generation.")))
                {
                    GameObject GenManagerInstance = Instantiate(managerPrefab);
                    SceneMapGenObj = GenManagerInstance;

                    MapGenScript = SceneMapGenObj.GetComponent<MapGenerator>();

                    int id = 0;

                    foreach (var obj in FindObjectsOfType<MapGenerator>())
                    {
                        if (obj.ID > id)
                        {
                            id = obj.ID;
                        }
                    }

                    id++;

                    MapGenScript.ID = id;
                    m_data.m_MapGenObjID = id;

                    m_data.m_setupComplete = true;
                }
            }
        }

        if (m_data.m_setupComplete && MapGenScript == null)
        {
            foreach (var obj in FindObjectsOfType<MapGenerator>())
            {
                if (obj.ID == m_data.m_MapGenObjID)
                {
                    MapGenScript = obj;
                    SceneMapGenObj = obj.gameObject;
                }
            }
        }

        if (m_data.m_setupComplete && MapGenScript != null)
        {
            GUILayout.Label("Generation", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            // Button to trigger room generation
            if (GUILayout.Button(new GUIContent("Generate Void Space", "Randomly places tiles to create void space.")))
            {
                m_data.m_voidGenTriggered = true;
                MapGenScript.CreateVoidTiles();
            }

            if (m_data.m_voidGenTriggered)
            {
                // Button to trigger room generation
                if (GUILayout.Button(new GUIContent("Generate Rooms", "Randomly places rooms on the map.")))
                {
                    m_data.m_roomGenTriggered = true;
                    MapGenScript.GenerateRooms();
                }
            }

            if (m_data.m_roomGenTriggered)
            {
                // Button to trigger corridor generation
                if (GUILayout.Button(new GUIContent("Generate Corridors", "Randomly places corridor on the map. NOTE: Generate after rooms have been created.")))
                {
                    MapGenScript.GenerateCorridors();
                }
            }

            GUILayout.EndHorizontal();

            m_data.m_mapLimits = EditorGUILayout.Vector2Field(new GUIContent("Map Limits", "Changes the size of the generated map"), m_data.m_mapLimits);
            MapGenScript.SetMapSize(m_data.m_mapLimits);

            // Button to reset map
            if (GUILayout.Button(new GUIContent("Reset", "Removes all components of map")))
            {
                m_data.m_voidGenTriggered = false;
                m_data.m_roomGenTriggered = false;
                MapGenScript.ResetMap();
            }

            // Button to delete manager in scene
            if (GUILayout.Button(new GUIContent("Delete Manager", "Deletes the map generator manager in scene.")))
            {
                MapGenScript.ResetMap();
                m_data.m_setupComplete = false;
                m_data.m_voidGenTriggered = false;
                m_data.m_roomGenTriggered = false;
                m_data.m_mapLimits = new Vector2(0, 0);
                DestroyImmediate(SceneMapGenObj);
                m_data.m_MapGenObjID = -1;
                MapGenScript = null;
            }

            m_data.m_optionalEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", m_data.m_optionalEnabled);
            EditorGUILayout.EndToggleGroup();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(m_data);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}