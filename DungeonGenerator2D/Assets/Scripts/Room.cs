using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Room : MonoBehaviour
{
    #region Fields

    [Tooltip("Spawn limit of the room")]
    [SerializeField]
    public float m_roomSpawnLimit = 1;

    [Tooltip("Enable to allow room rotation")]
    [SerializeField]
    public bool m_enableRoomRot = false;

    public int m_spawnedRooms = 0;

    #endregion

    // When object is created in scene, colliders are attached
    public void OnEnable()
    {
        TilemapCollider2D roomCollider = this.gameObject.AddComponent<TilemapCollider2D>();
        roomCollider.usedByComposite = true;
        this.transform.parent.gameObject.AddComponent<CompositeCollider2D>();
    }
}
