/*
 * File:	MapGenerator.cs
 *
 * Author: Mara Dusevic (s200494@students.aie.edu.au)
 * Date Created: Wednesday 28 April 2021
 * Date Last Modified: Thursday 15 July 2021
 * 
 * Using the given values and objects, this script will
 * generate a procedural 2D dungeon.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct Line
{
    public Vector2 pointA;
    public Vector2 pointB;
}

[ExecuteInEditMode]
public class MapGenerator : MonoBehaviour
{
    #region Settings

    [Header("Base Settings")]

    [SerializeField]
    [Tooltip("The ID to connect the manager to the custom editor.")]
    public int m_ID;

    [SerializeField]
    [Tooltip("The grid in which all tilemaps will be attached to.")]
    public Grid m_dungeonGrid = null;

    // The size of the map
    [SerializeField]
    [HideInInspector]
    public BoundsInt m_mapSize;

    #endregion

    #region VoidMap

    [Space(4)]
    [Header("Void Map")]

    [SerializeField]
    [Tooltip("Void Tilemap used to create space around the dungeon")]
    public Tilemap m_voidTilemap = null;

    [Tooltip("Textures used to fill the map's void spaces")]
    [SerializeField]
    public Tile[] m_voidTiles = null;

    #endregion

    #region Rooms

    [Space(4)]
    [Header("Rooms")]

    [SerializeField]
    [Tooltip("All room prefabs to be spawned")]
    private Room[] m_rooms = null;

    [SerializeField]
    [Tooltip("Maximum number of rooms that can be spawned")]
    private int m_totalRoomLimit = 10;

    [SerializeField]
    [Tooltip("Minimum distance allowed between rooms. NOTE: Rooms are alreading spawned with a one tile gap")]
    public int m_roomMinDistance = 1;

    // Holds an array of rotations the room can be set to
    private readonly int[] m_roomRotAngles = { 0, 90, 180, 270 };

    #endregion

    #region Corridors

    [Space(4)]
    [Header("Corridors")]

    [SerializeField]
    [Tooltip("Corridor tilemap used to create the corridors connecting the rooms")]
    public Tilemap m_corridorTilemap = null;

    [Tooltip("Textures used to fill the map's corridors")]
    [SerializeField]
    public Tile m_corridorTile = null;

    #endregion

    // Sets the map's size
    public void SetMapSize(Vector2 a_newMapSize)
    {
        m_mapSize.x = (int)a_newMapSize.x;
        m_mapSize.y = (int)a_newMapSize.y;
    }

    // Creates the void tiles with a mixture of given tiles
    public void CreateVoidTiles()
    {
        // If void tiles are provided
        if (m_voidTiles != null)
        {
            // Resets the map entirely
            ResetMap();

            // Loops through all the positions of the void tile map
            for (int x = 0; x < m_mapSize.x; x++)
            {
                for (int y = 0; y < m_mapSize.y; y++)
                {
                    // Gets a random tile from the given void tiles
                    int tileID = Random.Range(0, m_voidTiles.Length);

                    // Sets the tile at the current position to the randomly selected tile
                    m_voidTilemap.SetTile(new Vector3Int(-x + m_mapSize.x / 2, -y + m_mapSize.y / 2, 0), m_voidTiles[tileID]);
                }
            }
        }
    }

    // Creates random rooms on the map
    public void GenerateRooms()
    {
        // If rooms are provided
        if (m_rooms != null)
        {
            // Resets rooms and corridors each time
            ResetRooms();
            ResetCorridors();

            int xSpawnPos;
            int ySpawnPos;
            int fails = 0;
            int roomFails = 0;
            int totalSpawnedRooms = 0;

            // Loops until total room limit has been reached and fail safe has been triggered
            while (totalSpawnedRooms < m_totalRoomLimit && roomFails < 30)
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
                int roomID = Random.Range(0, m_rooms.Length);
                Room roomToSpawn = m_rooms[roomID];

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
                        xSpawnPos = Random.Range(((-m_mapSize.x / 2) + roomSize.x / 2) + 2, ((m_mapSize.x / 2) - roomSize.x / 2) - 2);
                        ySpawnPos = Random.Range(((-m_mapSize.y / 2) + roomSize.y / 2) + 2, ((m_mapSize.y / 2) - roomSize.y / 2) - 2);

                        // If the object allows for rotation
                        if (roomToSpawn.m_enableRoomRot)
                        {
                            // Gets a random rotations to apply to the room. Only four rotations are allowed.
                            int rot = Random.Range(0, m_roomRotAngles.Length);
                            roomRot = m_roomRotAngles[rot];

                            // If the object has been rotated 90 degrees, the objects position limits needs to be changed
                            if (roomRot == 90 || roomRot == 270)
                            {
                                xSpawnPos = Random.Range(((-m_mapSize.x / 2) + roomSize.y / 2) + 2, ((m_mapSize.x / 2) - roomSize.y / 2) - 2);
                                ySpawnPos = Random.Range(((-m_mapSize.y / 2) + roomSize.x / 2) + 2, ((m_mapSize.y / 2) - roomSize.x / 2) - 2);
                            }
                        }

                        // Creates a point with the x and y position values
                        roomPos = new Vector2Int(xSpawnPos, ySpawnPos);

                        // Checks for overlap with other objects within the room layer to determine if location is valid
                        int layerMask = 1 << 8;
                        Collider2D[] overlapObj = Physics2D.OverlapBoxAll(roomPos, new Vector2Int(roomSize.x + m_roomMinDistance, roomSize.y + m_roomMinDistance), roomRot, layerMask);

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
                        room.transform.parent = m_dungeonGrid.transform;
                        room.GetComponent<Room>().CreateColliders();
                        roomToSpawn.m_spawnedRooms++;
                        totalSpawnedRooms++;
                    }
                }
            }
        }
    }

    // ---- Corridor Functions ----

    // Creates corridors between each room
    public void GenerateCorridors()
    {
        // Returns if no tile or tilemap is given
        if (m_corridorTile == null && m_corridorTilemap == null)
        {
            return;
        }

        // Lists to store rooms depending on their state
        List<Room> rooms = new List<Room>();
        List<Room> connectedRooms = new List<Room>();

        // Resets the tilemap on each generation
        ResetCorridors();

        // Find all rooms in scene
        foreach (Room room in FindObjectsOfType(typeof(Room)))
        {
            rooms.Add(room);
        }

        // Pick one from random
        int roomID = Random.Range(0, rooms.Count);
        Room startRoom = rooms[roomID];

        // Add to connected rooms list
        connectedRooms.Add(startRoom);
        rooms.Remove(startRoom);

        while (rooms.Count != 0)
        {
            // Get the starting room's midpoint
            Vector3 startCenter = startRoom.gameObject.GetComponent<Tilemap>().cellBounds.center;
            Vector3 midPnt = startRoom.gameObject.GetComponent<Tilemap>().CellToWorld(new Vector3Int(Mathf.RoundToInt(startCenter.x), Mathf.RoundToInt(startCenter.y), 0));
            Vector3Int startMidPnt = new Vector3Int(Mathf.RoundToInt(midPnt.x), Mathf.RoundToInt(midPnt.y), Mathf.RoundToInt(midPnt.z));

            // Draw lines to all midpoints of rooms that are not connected
            Room closestRoom = null;
            Vector3Int closestRoomMidPnt = Vector3Int.zero;
            float minDistance = float.MaxValue;

            foreach (Room room in rooms)
            {
                if (!connectedRooms.Contains(room))
                {
                    Vector3 center = room.gameObject.GetComponent<Tilemap>().cellBounds.center;
                    Vector3 midPntInWorld = room.gameObject.GetComponent<Tilemap>().CellToWorld(new Vector3Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y), 0));

                    // Calculate distances between rooms and get the closest room
                    float distance = Vector2.Distance(new Vector2(startMidPnt.x, startMidPnt.y), new Vector2(midPntInWorld.x, midPntInWorld.y));
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestRoom = room;
                        closestRoomMidPnt = new Vector3Int(Mathf.RoundToInt(midPntInWorld.x), Mathf.RoundToInt(midPntInWorld.y), Mathf.RoundToInt(midPntInWorld.z));
                    }
                }
            }

            // Get each room's edges
            if (closestRoom == null)
            {
                return;
            }

            Vector3Int roomSize = closestRoom.GetComponent<Tilemap>().size;
            int closestRoomWidth = roomSize.x;
            int closestRoomHeight = roomSize.y;

            // If the room is rotated we need to change its sizing 
            if (closestRoom.m_enableRoomRot == true)
            {
                if (closestRoom.transform.rotation == Quaternion.Euler(0, 0, 90) ||
                    closestRoom.transform.rotation == Quaternion.Euler(0, 0, 270))
                {
                    closestRoomWidth = roomSize.y;
                    closestRoomHeight = roomSize.x;
                }
            }

            Line closestBotLine = new Line
            {
                pointA = new Vector2((closestRoomMidPnt.x - closestRoomWidth / 2) + 1, (closestRoomMidPnt.y - closestRoomHeight / 2) + 1),
                pointB = new Vector2((closestRoomMidPnt.x + closestRoomWidth / 2) - 1, (closestRoomMidPnt.y - closestRoomHeight / 2) + 1)
            };
            Line closestLeftLine = new Line
            {
                pointA = new Vector2((closestRoomMidPnt.x - closestRoomWidth / 2) + 1, (closestRoomMidPnt.y + closestRoomHeight / 2) - 1),
                pointB = new Vector2((closestRoomMidPnt.x - closestRoomWidth / 2) + 1, (closestRoomMidPnt.y - closestRoomHeight / 2) + 1)
            };

            Vector3Int startRoomSize = startRoom.GetComponent<Tilemap>().size;
            int startRoomWidth = startRoomSize.x;
            int startRoomHeight = startRoomSize.y;

            // If the room is rotated we need to change its sizing 
            if (startRoom.m_enableRoomRot == true)
            {
                if (startRoom.transform.rotation == Quaternion.Euler(0, 0, 90) ||
                    startRoom.transform.rotation == Quaternion.Euler(0, 0, 270))
                {
                    startRoomWidth = startRoomSize.y;
                    startRoomHeight = startRoomSize.x;
                }
            }

            Line startTopLine = new Line
            {
                pointA = new Vector2((startMidPnt.x - startRoomWidth / 2) + 1, (startMidPnt.y + startRoomHeight / 2) - 1),
                pointB = new Vector2((startMidPnt.x + startRoomWidth / 2) - 1, (startMidPnt.y + startRoomHeight / 2) - 1)
            };
            Line startRightLine = new Line
            {
                pointA = new Vector2((startMidPnt.x + startRoomWidth / 2) - 1, (startMidPnt.y + startRoomHeight / 2) - 1),
                pointB = new Vector2((startMidPnt.x + startRoomWidth / 2) - 1, (startMidPnt.y - startRoomHeight / 2) + 1)
            };

            // DETERMINE ROUTE

            // Calculate midpoint between each room's midpoints
            Vector2Int roomsMidPnt = new Vector2Int((startMidPnt.x + closestRoomMidPnt.x) / 2, (startMidPnt.y + closestRoomMidPnt.y) / 2);

            // Midpoint between rooms are closer vertically
            if (IsOnLine(startTopLine, new Vector2(roomsMidPnt.x, startTopLine.pointA.y)) &&
                IsOnLine(closestBotLine, new Vector2(roomsMidPnt.x, closestBotLine.pointA.y)))
            {
                BuildCorridorVertical(new Vector2Int(roomsMidPnt.x, startMidPnt.y), new Vector2Int(roomsMidPnt.x, closestRoomMidPnt.y));
            }
            // Midpoint between rooms are closer horizontally
            else if (IsOnLine(startRightLine, new Vector2(startRightLine.pointA.x, roomsMidPnt.y)) &&
                     IsOnLine(closestLeftLine, new Vector2(closestLeftLine.pointA.x, roomsMidPnt.y)))
            {
                BuildCorridorHorizontal(new Vector2Int(startMidPnt.x, roomsMidPnt.y), new Vector2Int(closestRoomMidPnt.x, roomsMidPnt.y));
            }
            // Otherwise, build L-shaped corridor
            else
            {
                BuildCorridorLShape(new Vector2Int(startMidPnt.x, startMidPnt.y), new Vector2Int(closestRoomMidPnt.x, closestRoomMidPnt.y));
            }

            rooms.Remove(closestRoom);
            connectedRooms.Add(closestRoom);
            startRoom = closestRoom;
        }
    }

    // Checks whether a given point is on a line
    private static bool IsOnLine(Line a_line, Vector2 a_point)
    {
        // If the point is either the start or end point of the line, return false
        if (a_line.pointA == a_point || a_line.pointB == a_point)
        {
            return false;
        }

        return Vector2.Distance(a_line.pointA, a_point) + Vector2.Distance(a_line.pointB, a_point) == Vector2.Distance(a_line.pointA, a_line.pointB);
    }

    // ---- Corridor Building Functions ----

    // Creates a horizontal corridor going left or right
    private void BuildCorridorHorizontal(Vector2Int a_startPoint, Vector2Int a_endPoint)
    {
        // Left
        if (a_startPoint.x > a_endPoint.x)
        {
            for (int i = a_startPoint.x; i > a_endPoint.x - 1; i--)
            {
                m_corridorTilemap.SetTile(new Vector3Int(i, a_startPoint.y, 0), m_corridorTile);
            }
        }
        // Right
        else if (a_endPoint.x > a_startPoint.x)
        {
            for (int i = a_startPoint.x; i < a_endPoint.x + 1; i++)
            {
                m_corridorTilemap.SetTile(new Vector3Int(i, a_startPoint.y, 0), m_corridorTile);
            }
        }
    }

    // Creates a vertical corridor going up or down
    private void BuildCorridorVertical(Vector2Int a_startPoint, Vector2Int a_endPoint)
    {
        // Down
        if (a_startPoint.y > a_endPoint.y)
        {
            for (int i = a_startPoint.y; i > a_endPoint.y - 1; i--)
            {
                m_corridorTilemap.SetTile(new Vector3Int(a_startPoint.x, i, 0), m_corridorTile);
            }
        }
        // Up
        else if (a_endPoint.y > a_startPoint.y)
        {
            for (int i = a_startPoint.y; i < a_endPoint.y + 1; i++)
            {
                m_corridorTilemap.SetTile(new Vector3Int(a_startPoint.x, i, 0), m_corridorTile);
            }
        }
    }

    // Creates an L-shaped corridor
    private void BuildCorridorLShape(Vector2Int a_startMidPnt, Vector2Int a_endMidPnt)
    {
        Vector2Int newStart = Vector2Int.zero;

        // Left
        if (a_startMidPnt.x > a_endMidPnt.x)
        {
            for (int i = a_startMidPnt.x; i > a_endMidPnt.x - 1; i--)
            {
                m_corridorTilemap.SetTile(new Vector3Int(i, a_startMidPnt.y, 0), m_corridorTile);
                if (i == a_endMidPnt.x)
                {
                    newStart = new Vector2Int(a_endMidPnt.x, a_startMidPnt.y);
                }
            }
        }
        // Right
        else if (a_endMidPnt.x > a_startMidPnt.x)
        {
            for (int i = a_startMidPnt.x; i < a_endMidPnt.x + 1; i++)
            {
                m_corridorTilemap.SetTile(new Vector3Int(i, a_startMidPnt.y, 0), m_corridorTile);
                if (i == a_endMidPnt.x)
                {
                    newStart = new Vector2Int(a_endMidPnt.x, a_startMidPnt.y);
                }
            }
        }

        // Down
        if (a_startMidPnt.y > a_endMidPnt.y)
        {
            for (int i = newStart.y; i > a_endMidPnt.y - 1; i--)
            {
                m_corridorTilemap.SetTile(new Vector3Int(newStart.x, i, 0), m_corridorTile);
            }
        }
        // Up
        else if (a_endMidPnt.y > a_startMidPnt.y)
        {
            for (int i = newStart.y; i < a_endMidPnt.y + 1; i++)
            {
                m_corridorTilemap.SetTile(new Vector3Int(newStart.x, i, 0), m_corridorTile);
            }
        }
    }

    // ---- Reset Functions ----

    // Removes all the rooms from the grid
    private void ResetRooms()
    {
        foreach (var room in m_dungeonGrid.GetComponentsInChildren<Room>())
        {
            foreach (Room prefabRooms in m_rooms)
            {
                prefabRooms.m_spawnedRooms = 0;
            }

            DestroyImmediate(room.gameObject);
        }
    }

    // Removes all tiles on the void map
    private void ResetVoidMap()
    {
        if (m_voidTilemap != null)
        {
            m_voidTilemap.ClearAllTiles();
        }
    }

    // Removes all corridors on the corridor map
    private void ResetCorridors()
    {
        if (m_corridorTilemap != null)
        {
            m_corridorTilemap.ClearAllTiles();
        }
    }

    // Resets the grid entirely 
    public void ResetMap()
    {
        ResetVoidMap();
        ResetRooms();
        ResetCorridors();
    }
}