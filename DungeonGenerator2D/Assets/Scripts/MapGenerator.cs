using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    #region Settings

    [Header("Base Settings")]

    public int ID;

    [SerializeField]
    [Tooltip("Tilemap used to spawn tiles and map")]
    public Tilemap tilemap;

    [SerializeField]
    [HideInInspector]
    public BoundsInt mapSize;

    #endregion

    #region Tiles

    [Space(4)]
    [Header("Tiles")]

    [Tooltip("Textures used to fill the map's void spaces")]
    [SerializeField]
    public Tile[] voidTiles;

    #endregion

    #region Rooms

    [Space(4)]
    [Header("Rooms")]

    [SerializeField]
    [Tooltip("All room prefabs to be spawned")]
    private Room[] rooms;

    [SerializeField]
    [Tooltip("Maximum number of rooms that can be spawned")]
    private int totalRoomLimit = 10;

    private int[] roomRotAngles = { 0, 90, 180, 270 };

    #endregion

    public void SetMapSize(Vector2 newMapSize)
    {
        mapSize.x = (int)newMapSize.x;
        mapSize.y = (int)newMapSize.y;
    }

    public void CreateVoidTiles()
    {
        if (voidTiles != null)
        {
            ResetMap();

            for (int x = 0; x < mapSize.x; x++)
            {
                for (int y = 0; y < mapSize.y; y++)
                {
                    int tileID = Random.Range(0, voidTiles.Length);

                    tilemap.SetTile(new Vector3Int(-x + mapSize.x / 2, -y + mapSize.y / 2, 0), voidTiles[tileID]);
                }
            }
        }
    }

    public void GenerateRooms()
    {
        if (rooms != null)
        {
            int xSpawnSize;
            int ySpawnSize;

            for (int i = 0; i < totalRoomLimit; i++)
            {
                int roomID = Random.Range(0, rooms.Length);
                Room roomToSpawn = rooms[roomID];

                Vector3Int roomSize = roomToSpawn.gameObject.GetComponent<Tilemap>().size;

                if (roomToSpawn.m_spawnedRooms != roomToSpawn.m_roomSpawnLimit)
                {
                    xSpawnSize = Random.Range(((-mapSize.x / 2) + roomSize.x / 2) + 1, ((mapSize.x / 2) - roomSize.x / 2) + 1);
                    ySpawnSize = Random.Range(((-mapSize.y / 2) + roomSize.y / 2) + 1, ((mapSize.y / 2) - roomSize.y / 2) + 1);

                    int roomRot = 0;

                    if (roomToSpawn.m_enableRoomRot)
                    {
                        int rot = Random.Range(0, roomRotAngles.Length);
                        roomRot = roomRotAngles[rot];

                        if (roomRot == 90 || roomRot == 270)
                        {
                            xSpawnSize = Random.Range(((-mapSize.x / 2) + roomSize.y / 2) + 1, ((mapSize.x / 2) - roomSize.y / 2) + 1);
                            ySpawnSize = Random.Range(((-mapSize.y / 2) + roomSize.x / 2) + 1, ((mapSize.y / 2) - roomSize.x / 2) + 1);
                        }
                    }

                    Vector2 roomPos = new Vector2Int(xSpawnSize, ySpawnSize);
                    GameObject room = Instantiate(roomToSpawn.gameObject, roomPos, Quaternion.Euler(0, 0, roomRot));
                    room.transform.parent = tilemap.transform.parent;
                    room.GetComponent<Room>().CreateColliders();

                    int layerMask = 1 << 8;
                    Collider2D[] colliders = Physics2D.OverlapBoxAll(roomPos, new Vector2Int(roomSize.x, roomSize.y), roomRot, layerMask);
                    bool isValidLocation = colliders.Length == 0;

                    foreach (TilemapCollider2D col in colliders)
                    {
                        Debug.Log(col.gameObject);
                    }

                    Debug.Log(isValidLocation + "  " + colliders.Length);

                    if (!isValidLocation)
                    {
                        DestroyImmediate(room);
                    }
                    else
                    {
                        roomToSpawn.m_spawnedRooms++;
                    }
                }
            }
        }
    }

    public void GenerateCorridors()
    {

    }

    private void GenerateDoors()
    {

    }

    public void ResetMap()
    {
        if (tilemap != null)
        {
            tilemap.ClearAllTiles();

            foreach (var room in FindObjectsOfType<Room>())
            {
                foreach (Room prefabRooms in rooms)
                {
                    prefabRooms.m_spawnedRooms = 0;
                }

                DestroyImmediate(room.gameObject);
            }
        }
    }
}
