using UnityEditor;
using UnityEngine;

public class CustomEditor : EditorWindow
{
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

        GUILayout.Label("Base Settings", EditorStyles.boldLabel);

        m_data.m_SceneMapGenObject = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Map Generation Manager Object", "The object in scene that will contain the MapGeneration script."), m_data.m_SceneMapGenObject, typeof(GameObject), true);

        m_data.m_MapGenScript = (MapGenerator)EditorGUILayout.ObjectField(new GUIContent("Map Generation Script", "MapGeneration script."), m_data.m_MapGenScript, typeof(MapGenerator), true);

        if (m_data.m_SceneMapGenObject != null && !m_data.m_setupComplete)
        {
            // Button to setup map generation script
            if (GUILayout.Button(new GUIContent("Setup", "Adds scripts to in scene object to run generation.")))
            {
                foreach (var component in m_data.m_SceneMapGenObject.GetComponents<Component>())
                {
                    if (!(component is Transform) && !(component is MapGenerator))
                    {
                        Destroy(component);

                        //m_data.m_SceneMapGenObject.AddComponent<MapGenerator>();
                        //m_data.m_MapGenScript = m_data.m_SceneMapGenObject.GetComponent<MapGenerator>();
                    }
                }

                m_data.m_setupComplete = true;
            }
        }

        if (m_data.m_setupComplete && m_data.m_MapGenScript != null)
        {
            GUILayout.Label("Generation", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            // Button to trigger room generation
            if (GUILayout.Button(new GUIContent("Generate Void Space", "Randomly places tiles to create void space.")))
            {
                Debug.Log("GENERATING VOID SPACE");

                m_data.m_voidGenTriggered = true;
                m_data.m_MapGenScript.CreateVoidTiles();
            }

            if (m_data.m_voidGenTriggered)
            {
                // Button to trigger room generation
                if (GUILayout.Button(new GUIContent("Generate Rooms", "Randomly places rooms on the map.")))
                {
                    Debug.Log("GENERATING ROOMS");

                    m_data.m_roomGenTriggered = true;
                    m_data.m_MapGenScript.GenerateRooms();
                }
            }

            if (m_data.m_roomGenTriggered)
            {
                // Button to trigger corridor generation
                if (GUILayout.Button(new GUIContent("Generate Corridors", "Randomly places corridor on the map. NOTE: Generate after rooms have been created.")))
                {
                    Debug.Log("GENERATING CORRIDORS");

                    m_data.m_MapGenScript.GenerateCorridors();
                }
            }

            GUILayout.EndHorizontal();

            m_data.m_mapLimits = EditorGUILayout.Vector2Field(new GUIContent("Map Limits", "Changes the size of the generated map"), m_data.m_mapLimits);
            m_data.m_MapGenScript.SetMapSize(m_data.m_mapLimits);

            // Button to reset map
            if (GUILayout.Button(new GUIContent("Reset", "Removes all components of map")))
            {
                Debug.Log("RESET MAP");

                m_data.m_voidGenTriggered = false;
                m_data.m_roomGenTriggered = false;
                m_data.m_MapGenScript.ResetMap();
            }

            m_data.m_optionalEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", m_data.m_optionalEnabled);
            EditorGUILayout.EndToggleGroup();
        }
    }
}