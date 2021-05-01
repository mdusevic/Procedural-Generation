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

    [Tooltip("ID assigned to manager object in scene")]
    [SerializeField]
    public int m_MapGenObjID;

    #endregion

    // When scriptable object is opened, data is created and sent to editor
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