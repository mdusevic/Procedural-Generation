using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteInEditMode]
public class MapGenerator : MonoBehaviour
{
    #region Settings

    [Header("Base Settings")]

    [SerializeField]
    [Tooltip("The ID to connect the manager to the custom editor.")]
    public int ID;

    [SerializeField]
    [Tooltip("The grid in which all tilemaps will be attached to.")]
    public Grid dungeonGrid = null;

    // The size of the map
    [SerializeField]
    [HideInInspector]
    public BoundsInt mapSize;

    #endregion

    #region VoidMap

    [Space(4)]
    [Header("Void Map")]

    [SerializeField]
    [Tooltip("Void Tilemap used to create space around the dungeon")]
    public Tilemap voidTilemap = null;

    [Tooltip("Textures used to fill the map's void spaces")]
    [SerializeField]
    public Tile[] voidTiles = null;

    #endregion

    #region Rooms

    [Space(4)]
    [Header("Rooms")]

    [SerializeField]
    [Tooltip("All room prefabs to be spawned")]
    private Room[] rooms = null;

    [SerializeField]
    [Tooltip("Maximum number of rooms that can be spawned")]
    private int totalRoomLimit = 10;

    [SerializeField]
    [Tooltip("Minimum distance allowed between rooms. NOTE: Rooms are alreading spawned with a one tile gap")]
    public int roomMinDistance = 1;

    // Holds an array of rotations the room can be set to
    private int[] roomRotAngles = { 0, 90, 180, 270 };

    #endregion

    public void SetMapSize(Vector2 newMapSize)
    {
        mapSize.x = (int)newMapSize.x;
        mapSize.y = (int)newMapSize.y;
    }

    public void CreateVoidTiles()
    {
        // If void tiles are provided
        if (voidTiles != null)
        {
            // Resets the map entirely
            ResetMap();

            // Loops through all the positions of the void tile map
            for (int x = 0; x < mapSize.x; x++)
            {
                for (int y = 0; y < mapSize.y; y++)
                {
                    // Gets a random tile from the given void tiles
                    int tileID = Random.Range(0, voidTiles.Length);

                    // Sets the tile at the current position to the randomly selected tile
                    voidTilemap.SetTile(new Vector3Int(-x + mapSize.x / 2, -y + mapSize.y / 2, 0), voidTiles[tileID]);
                }
            }
        }
    }

    public void GenerateRooms()
    {
        // If rooms are provided
        if (rooms != null)
        {
            // Resets rooms each time
            ResetRooms();

            int xSpawnPos;
            int ySpawnPos;
            int fails = 0;
            int roomFails = 0;
            int totalSpawnedRooms = 0;

            // Loops until total room limit has been reached and fail safe has been triggered
            while (totalSpawnedRooms < totalRoomLimit && roomFails < 30)
            {
                // If the fail safe in creating the room's position has been breached
                if (fails > 50)
                {
                    // Resets the fail safe
                    fails = 0;

                    // Increases the secondary fail safe
                    roomFails++;
                }
                
                // Randomly picks a room from the given prefabs
                int roomID = Random.Range(0, rooms.Length);
                Room roomToSpawn = rooms[roomID];

                // Find the rooms size within the tilemap
                Vector3Int roomSize = roomToSpawn.gameObject.GetComponent<Tilemap>().size;

                Vector2 roomPos;
                int roomRot = 0;

                // If the specfic room type's limit has not been reached 
                if (roomToSpawn.m_spawnedRooms != roomToSpawn.m_roomSpawnLimit)
                {
                    bool isValidLocation = false;

                    // While the location of the room is false and no fail safe has been triggered, a new position will be created on each loop
                    do
                    {
                        // Gets a random position within the maps boundaries using the rooms size. No room can be touching.
                        xSpawnPos = Random.Range(((-mapSize.x / 2) + roomSize.x / 2) + 1, ((mapSize.x / 2) - roomSize.x / 2) + 1);
                        ySpawnPos = Random.Range(((-mapSize.y / 2) + roomSize.y / 2) + 1, ((mapSize.y / 2) - roomSize.y / 2) + 1);

                        // If the object allows for rotation
                        if (roomToSpawn.m_enableRoomRot)
                        {
                            // Gets a random rotations to apply to the room. Only four rotations are allowed.
                            int rot = Random.Range(0, roomRotAngles.Length);
                            roomRot = roomRotAngles[rot];

                            // If the object has been rotated 90 degrees, the objects position limits needs to be changed
                            if (roomRot == 90 || roomRot == 270)
                            {
                                xSpawnPos = Random.Range(((-mapSize.x / 2) + roomSize.y / 2) + 1, ((mapSize.x / 2) - roomSize.y / 2) + 1);
                                ySpawnPos = Random.Range(((-mapSize.y / 2) + roomSize.x / 2) + 1, ((mapSize.y / 2) - roomSize.x / 2) + 1);
                            }
                        }

                        // Creates a point with the x and y position values
                        roomPos = new Vector2Int(xSpawnPos, ySpawnPos);

                        // Checks for overlap with other objects within the room layer to determine if location is valid
                        int layerMask = 1 << 8;
                        Collider2D[] overlapObj = Physics2D.OverlapBoxAll(roomPos, new Vector2Int(roomSize.x + roomMinDistance, roomSize.y + roomMinDistance), roomRot, layerMask);

                        // If no overlaps have been detected in the layermask
                        if (overlapObj.Length == 0)
                        {
                            // Location is deemed valid and we are no longer in do/while loop
                            isValidLocation = true;

                            // Resets the fail safe
                            fails = 0;
                        }
                        // Otherwise, if overlaps have been detected in the layermask
                        else if (overlapObj.Length > 0)
                        {
                            // Fail safe is increased
                            fails++;
                        }

                    } while (!isValidLocation && fails < 50);

                    if (isValidLocation)
                    {
                        // Creates instance of room using data generated above
                        GameObject room = Instantiate(roomToSpawn.gameObject, roomPos, Quaternion.Euler(0, 0, roomRot));
                        room.transform.parent = dungeonGrid.transform;
                        room.GetComponent<Room>().CreateColliders();
                        roomToSpawn.m_spawnedRooms++;
                        totalSpawnedRooms++;
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

    private void ResetRooms()
    {
        foreach (var room in dungeonGrid.GetComponentsInChildren<Room>())
        {
            foreach (Room prefabRooms in rooms)
            {
                prefabRooms.m_spawnedRooms = 0;
            }

            DestroyImmediate(room.gameObject);
        }
    }

    private void ResetVoidMap()
    {
        if (voidTilemap != null)
        {
            voidTilemap.ClearAllTiles();
        }
    }

    public void ResetMap()
    {
        ResetVoidMap();
        ResetRooms();
    }
}
