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
    public void CreateColliders()
    {
        CreateGridCollider();
        CreateRoomCollider();
    }

    private void CreateGridCollider()
    {
        GameObject grid = this.transform.parent.gameObject;

        if (grid.GetComponent<Rigidbody2D>())
        {
            return;
        }

        if (!grid.GetComponent<CompositeCollider2D>())
        {
            grid.AddComponent<CompositeCollider2D>();
            grid.GetComponent<Rigidbody2D>().isKinematic = true;
        }
    }

    private void CreateRoomCollider()
    {
        if (!this.gameObject.GetComponent<TilemapCollider2D>())
        {
            this.gameObject.AddComponent<TilemapCollider2D>();
            this.gameObject.GetComponent<TilemapCollider2D>().usedByComposite = true;
        }

        if (!this.gameObject.GetComponent<BoxCollider2D>())
        {
            this.gameObject.AddComponent<BoxCollider2D>();
            this.gameObject.GetComponent<BoxCollider2D>().isTrigger = true;
        }
    }
}
