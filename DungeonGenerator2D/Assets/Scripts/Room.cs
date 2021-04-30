using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Room : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Spawn limit of the room")]
    public float m_roomSpawnLimit = 1;

    public void OnEnable()
    {
        TilemapCollider2D roomCollider = this.gameObject.AddComponent<TilemapCollider2D>();
        roomCollider.usedByComposite = true;
        this.transform.parent.gameObject.AddComponent<CompositeCollider2D>();
    }
}
