using UnityEditor;
using UnityEngine;

public class CustomEditor : EditorWindow
{
    #region Fields

    private GameObject m_MapGenPrefab;
    private GameObject m_SceneMapGenObj;

    private MapGenerator m_MapGenScript;

    private static CustomEditorData m_data;
    public static CustomEditor m_window;

    #endregion

    // Adds menu item to the window
    [MenuItem("Window/Dungeon Generator")]
    // Shows existing window instance. If one doesn't exist, make one.
    public static void Init()
    {
        // Get existing window or if none, make one
        m_window = (CustomEditor)EditorWindow.GetWindow(typeof(CustomEditor));
        m_window.Show();
    }

    // Loads the information from the opened data object
    public static void LoadEditorData(CustomEditorData a_data)
    {
        m_data = a_data;
    }

    // Creates the manager in scene if none exists 
    private void CreateManager()
    {
        // Creates instance of prefab in scene
        GameObject GenManagerInstance = Instantiate(m_MapGenPrefab);
        
        // Links object to editor
        m_SceneMapGenObj = GenManagerInstance;
        m_MapGenScript = m_SceneMapGenObj.GetComponent<MapGenerator>();

        // Creates a unique id to link the object again when loaded
        int id = 0;

        // Loops through all objects in scene to create the highest ID available
        foreach (var obj in FindObjectsOfType<MapGenerator>())
        {
            if (obj.ID > id)
            {
                id = obj.ID;
            }
        }

        id++;

        // Assigns ID to object and saves it
        m_MapGenScript.ID = id;
        m_data.m_MapGenObjID = id;

        // Sets setup as completed
        m_data.m_setupComplete = true;
    }

    // Finds and connects the in scene manager to the editor
    private void LoadManager()
    {
        // Loops though all objects in scene with assigned script
        foreach (var obj in FindObjectsOfType<MapGenerator>())
        {
            // If the object in scene has the same ID as the saved data
            if (obj.ID == m_data.m_MapGenObjID)
            {
                // Sets editor variables to connect it with the object
                m_MapGenScript = obj;
                m_SceneMapGenObj = obj.gameObject;
            }
        }
    }

    // Deletes the scene manager
    private void DeleteManager()
    {
        // Sets all variables back to their defaults, resets the map and destroys manager object in scene
        m_MapGenScript.ResetMap();
        m_data.m_setupComplete = false;
        m_data.m_voidGenTriggered = false;
        m_data.m_roomGenTriggered = false;
        m_data.m_mapLimits = new Vector2(0, 0);
        DestroyImmediate(m_SceneMapGenObj);
        m_data.m_MapGenObjID = -1;
        m_MapGenScript = null;
    }

    private void OnGUI()
    {
        // If no data exists
        if (m_data == null)
        {
            return;
        }

        // If setup hasn't been completed
        if (!m_data.m_setupComplete)
        {
            // UI to create a new manager in scene
            GUILayout.Label("Map Generation Manager Prefab", EditorStyles.boldLabel);
            m_MapGenPrefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Map Generation Manager Prefab", "Prefab that contains MapGenerator script"), m_MapGenPrefab, typeof(GameObject), true);

            // When a prefab to instantiate has been given
            if (m_MapGenPrefab != null)
            {
                // Button to create generation manager in scene
                if (GUILayout.Button(new GUIContent("Setup", "Creates manager in scene")))
                {
                    // Calls function to create manager in scene
                    CreateManager();
                }
            }
        }

        // If setup has been completed but no object has been connected
        if (m_data.m_setupComplete && m_MapGenScript == null)
        {
            // Calls the function to find and load the manager in scene
            LoadManager();
        }

        // If setup has been completed and object has been linked to editor
        if (m_data.m_setupComplete && m_MapGenScript != null)
        {
            GUILayout.Label("Generation", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            // Button to trigger room generation
            if (GUILayout.Button(new GUIContent("Generate Void Space", "Randomly places tiles to create void space.")))
            {
                m_data.m_voidGenTriggered = true;
                m_MapGenScript.CreateVoidTiles();
            }

            if (m_data.m_voidGenTriggered)
            {
                // Button to trigger corridor generation
                if (GUILayout.Button(new GUIContent("Generate Corridors", "Randomly places corridor on the map. NOTE: Generate after rooms have been created.")))
                {
                    m_data.m_roomGenTriggered = true;
                    m_MapGenScript.GenerateCorridors();
                }
            }

            if (m_data.m_roomGenTriggered)
            {
                // Button to trigger room generation
                if (GUILayout.Button(new GUIContent("Generate Rooms", "Randomly places rooms on the map.")))
                {
                    m_MapGenScript.GenerateRooms();
                }
            }

            GUILayout.EndHorizontal();

            // UI to edit the size of the map
            m_data.m_mapLimits = EditorGUILayout.Vector2Field(new GUIContent("Map Limits", "Changes the size of the generated map"), m_data.m_mapLimits);
            m_MapGenScript.SetMapSize(m_data.m_mapLimits);

            // Button to reset map
            if (GUILayout.Button(new GUIContent("Reset", "Removes all components of map")))
            {
                m_data.m_voidGenTriggered = false;
                m_data.m_roomGenTriggered = false;
                m_MapGenScript.ResetMap();
            }

            // Button to delete manager in scene
            if (GUILayout.Button(new GUIContent("Delete Manager", "Deletes the map generator manager in scene.")))
            {
                DeleteManager();
            }

            m_data.m_optionalEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", m_data.m_optionalEnabled);
            EditorGUILayout.EndToggleGroup();
        }

        // Saves data on UI changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(m_data);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}