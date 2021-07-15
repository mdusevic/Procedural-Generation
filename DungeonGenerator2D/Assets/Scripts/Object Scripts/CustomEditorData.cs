/*
 * File:	CustomEditorData.cs
 *
 * Author: Mara Dusevic (s200494@students.aie.edu.au)
 * Date Created: Thursday 29 April 2021
 * Date Last Modified: Thursday 15 July 2021
 * 
 * Used to store custom editor window data within an
 * scriptable object. Holds values that can be edited
 * and read by the editor window.
 *
 */

using UnityEngine;
using UnityEditor.Callbacks;

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