using UnityEditor;
using UnityEngine;

public class CustomEditor : EditorWindow
{
    public GameObject managerPrefab;

    private static CustomEditorData m_data;
    public static CustomEditor m_window;

    // Adds menu item to the window
    [MenuItem ("Window/Dungeon Generator")]

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
                    m_data.m_SceneMapGenObject = GenManagerInstance;

                    m_data.m_MapGenScript = m_data.m_SceneMapGenObject.GetComponent<MapGenerator>();

                    m_data.m_setupComplete = true;
                }
            }
        }

        if (m_data.m_setupComplete && m_data.m_MapGenScript != null)
        {
            GUILayout.Label("Generation", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            // Button to trigger room generation
            if (GUILayout.Button(new GUIContent("Generate Void Space", "Randomly places tiles to create void space.")))
            {
                m_data.m_voidGenTriggered = true;
                m_data.m_MapGenScript.CreateVoidTiles();
            }

            if (m_data.m_voidGenTriggered)
            {
                // Button to trigger room generation
                if (GUILayout.Button(new GUIContent("Generate Rooms", "Randomly places rooms on the map.")))
                {
                    m_data.m_roomGenTriggered = true;
                    m_data.m_MapGenScript.GenerateRooms();
                }
            }

            if (m_data.m_roomGenTriggered)
            {
                // Button to trigger corridor generation
                if (GUILayout.Button(new GUIContent("Generate Corridors", "Randomly places corridor on the map. NOTE: Generate after rooms have been created.")))
                {
                    m_data.m_MapGenScript.GenerateCorridors();
                }
            }

            GUILayout.EndHorizontal();

            m_data.m_mapLimits = EditorGUILayout.Vector2Field(new GUIContent("Map Limits", "Changes the size of the generated map"), m_data.m_mapLimits);
            m_data.m_MapGenScript.SetMapSize(m_data.m_mapLimits);

            // Button to reset map
            if (GUILayout.Button(new GUIContent("Reset", "Removes all components of map")))
            {
                m_data.m_voidGenTriggered = false;
                m_data.m_roomGenTriggered = false;
                m_data.m_MapGenScript.ResetMap();
            }

            // Button to delete manager in scene
            if (GUILayout.Button(new GUIContent("Delete Manager", "Deletes the map generator manager in scene.")))
            {
                m_data.m_MapGenScript.ResetMap();
                m_data.m_setupComplete = false;
                m_data.m_voidGenTriggered = false;
                m_data.m_roomGenTriggered = false;
                m_data.m_mapLimits = new Vector2(0, 0);
                DestroyImmediate(m_data.m_SceneMapGenObject);
                m_data.m_SceneMapGenObject = null;
                m_data.m_MapGenScript = null;
            }

            m_data.m_optionalEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", m_data.m_optionalEnabled);
            EditorGUILayout.EndToggleGroup();
        }
    }
}