using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor;

[CreateAssetMenu(
    fileName = "NewCustomGeneratorData",
    menuName = "Dungeon Generator Data",
    order = 0
    )]
public class CustomEditorData : ScriptableObject
{
    #region Fields

    [Header("Generation Data")]

    [HideInInspector]
    [SerializeField]
    public bool m_optionalEnabled;

    [HideInInspector]
    [SerializeField]
    public bool m_voidGenTriggered = false;

    [HideInInspector]
    [SerializeField]
    public bool m_roomGenTriggered = false;

    [HideInInspector]
    [SerializeField]
    public bool m_setupComplete = false;

    [HideInInspector]
    [SerializeField]
    public Vector2 m_mapLimits;

    [HideInInspector]
    [SerializeField]
    public GameObject m_SceneMapGenObject;

    [HideInInspector]
    [SerializeField]
    public MapGenerator m_MapGenScript;

    #endregion

    [OnOpenAssetAttribute(1)]
    public static bool CreateData(int instanceID, int line)
    {
        Object obj = UnityEditor.EditorUtility.InstanceIDToObject(instanceID);
        System.Type type = obj.GetType();

        if (typeof(CustomEditorData) == type)
        {
            CustomEditor.Init();
            CustomEditor.LoadEditorData((CustomEditorData)obj);

            return true;
        }

        return false;
    }
}