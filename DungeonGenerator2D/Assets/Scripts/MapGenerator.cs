﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct TileCoord
{
    public int xPos;
    public int yPos;

    public TileCoord(int x, int y)
    {
        xPos = x;
        yPos = y;
    }
}

public struct Node
{
    public Tile m_tile;
    public Vector3Int m_position;
}

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
    private readonly int[] roomRotAngles = { 0, 90, 180, 270 };

    #endregion

    #region Corridors

    [Space(4)]
    [Header("Corridors")]

    [SerializeField]
    [Tooltip("Corridor tilemap used to create the corridors connecting the rooms")]
    public Tilemap corridorTilemap = null;

    [Tooltip("Textures used to fill the map's corridors")]
    [SerializeField]
    public Tile corridorTile = null;

    private readonly Vector2Int[] directions = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

    public int borderWidth = 0;
    public int borderHeight = 0;

    public string seed;
    public bool enableRandomSeed = false;

    [Range(0, 80)]
    public float randomFillPercent;
    public int smoothTimes = 5;

    int[,] map;

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
            // Resets rooms and corridors each time
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

    public void GenerateCorridors2()
    {
        ResetCorridors();
        GenerateMap();
    }

    public void UpdateTilemap()
    {
        corridorTilemap.transform.position = new Vector3(-mapSize.x / 2, -mapSize.y / 2, 0);

        if (map != null)
        {
            for (int x = 0; x < mapSize.x; x++)
            {
                for (int y = 0; y < mapSize.y; y++)
                {
                    corridorTilemap.SetTile(new Vector3Int(x, y, 0), (map[x, y] == 1) ? null : corridorTile);
                }
            }
        }
    }

    void GenerateMap()
    {
        map = new int[mapSize.x, mapSize.y];
        RandomFillMap();

        for (int i = 0; i < smoothTimes; i++)
        {
            SmoothMap();
        }

        ProcessMap();

        UpdateTilemap();
    }

    void ProcessMap()
    {
        int regionCount = 0;

        List<List<TileCoord>> corridorRegions = GetRegions(0);
        int corridorThresholdSize = 50;

        foreach (List<TileCoord> corridorRegion in corridorRegions)
        {
            if (corridorRegion.Count < corridorThresholdSize)
            {
                foreach (TileCoord tile in corridorRegion)
                {
                    map[tile.xPos, tile.yPos] = 1;
                }
            }
            else
            {
                regionCount++;
            }
        }

        Debug.Log("Region Count: " + regionCount);
    }

    List<List<TileCoord>> GetRegions(int tileType)
    {
        List<List<TileCoord>> regions = new List<List<TileCoord>>();
        int[,] mapFlags = new int[mapSize.x, mapSize.y];

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                {
                    List<TileCoord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach (TileCoord tile in newRegion)
                    {
                        mapFlags[tile.xPos, tile.yPos] = 1;
                    }
                }
            }
        }

        return regions;
    }

    List<TileCoord> GetRegionTiles(int startX, int startY)
    {
        List<TileCoord> tiles = new List<TileCoord>();
        int[,] mapFlags = new int[mapSize.x, mapSize.y];
        int tileType = map[startX, startY];

        Queue<TileCoord> queue = new Queue<TileCoord>();
        queue.Enqueue(new TileCoord(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0)
        {
            TileCoord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.xPos - 1; x <= tile.xPos + 1; x++)
            {
                for (int y = tile.yPos - 1; y <= tile.yPos + 1; y++)
                {
                    if (IsInMapRange(x, y) && (y == tile.yPos || x == tile.xPos))
                    {
                        if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                        {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new TileCoord(x, y));
                        }
                    }
                }
            }
        }

        return tiles;
    }

    bool IsInMapRange(int x, int y)
    {
        return x >= 0 && x < mapSize.x && y >= 0 && y < mapSize.y;
    }

    void RandomFillMap()
    {
        if (enableRandomSeed)
        {
            seed = Time.time.ToString();
        }
        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                if (x < borderWidth || x >= mapSize.x - borderWidth || y < borderHeight || y >= mapSize.y - borderHeight)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? 1 : 0;
                }
            }
        }
    }

    void SmoothMap()
    {
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                int neighbourWallTiles = GetSurroundingWallCount(x, y);

                if (neighbourWallTiles > 4)
                {
                    map[x, y] = 1;
                }
                else if (neighbourWallTiles < 4)
                {
                    map[x, y] = 0;
                }
            }
        }
    }

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                if (neighbourX >= 0 && neighbourX < mapSize.x && neighbourY >= 0 && neighbourY < mapSize.y)
                {
                    if (neighbourX != gridX || neighbourY != gridY)
                    {
                        wallCount += map[neighbourX, neighbourY];
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }

        return wallCount;
    }

    public void GenerateCorridors()
    {
        // Returns if no tile or tilemap is given
        if (corridorTile == null && corridorTilemap == null)
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
            Vector3 midPnt = startRoom.gameObject.GetComponent<Tilemap>().CellToWorld(new Vector3Int((int)startCenter.x, (int)startCenter.y, 0));
            Vector3Int startMidPnt = new Vector3Int((int)midPnt.x, (int)midPnt.y, (int)midPnt.z);

            // Draw lines to all midpoints of rooms that are not connected
            Room closestRoom = null;
            Vector3Int closestRoomMidPnt = Vector3Int.zero;
            float minDistance = float.MaxValue;

            foreach (Room room in rooms)
            {
                if (!connectedRooms.Contains(room))
                {
                    Vector3 center = room.gameObject.GetComponent<Tilemap>().cellBounds.center;
                    Vector3 midPntInWorld = room.gameObject.GetComponent<Tilemap>().CellToWorld(new Vector3Int((int)center.x, (int)center.y, 0));

                    // Calculate distances between rooms and get the closest room
                    float distance = Vector2.Distance(new Vector2(startMidPnt.x, startMidPnt.y), new Vector2(midPntInWorld.x, midPntInWorld.y));
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestRoom = room;
                        closestRoomMidPnt = new Vector3Int((int)midPntInWorld.x, (int)midPntInWorld.y, (int)midPntInWorld.z);
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
            Line closestTopLine = new Line
            {
                pointA = new Vector2((closestRoomMidPnt.x - closestRoomWidth / 2) + 1, (closestRoomMidPnt.y + closestRoomHeight / 2) - 1),
                pointB = new Vector2((closestRoomMidPnt.x + closestRoomWidth / 2) - 1, (closestRoomMidPnt.y + closestRoomHeight / 2) - 1)
            };
            Line closestLeftLine = new Line
            {
                pointA = new Vector2((closestRoomMidPnt.x - closestRoomWidth / 2) + 1, (closestRoomMidPnt.y + closestRoomHeight / 2) - 1),
                pointB = new Vector2((closestRoomMidPnt.x - closestRoomWidth / 2) + 1, (closestRoomMidPnt.y - closestRoomHeight / 2) + 1)
            };
            Line closestRightLine = new Line
            {
                pointA = new Vector2((closestRoomMidPnt.x + closestRoomWidth / 2) - 1, (closestRoomMidPnt.y + closestRoomHeight / 2) - 1),
                pointB = new Vector2((closestRoomMidPnt.x + closestRoomWidth / 2) - 1, (closestRoomMidPnt.y - closestRoomHeight / 2) + 1)
            };

            Debug.DrawLine(closestBotLine.pointA, closestBotLine.pointB, Color.green, 3.0f);
            Debug.DrawLine(closestTopLine.pointA, closestTopLine.pointB, Color.green, 3.0f);
            Debug.DrawLine(closestLeftLine.pointA, closestLeftLine.pointB, Color.green, 3.0f);
            Debug.DrawLine(closestRightLine.pointA, closestRightLine.pointB, Color.green, 3.0f);

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

            Line startBotLine = new Line
            {
                pointA = new Vector2((startMidPnt.x - startRoomWidth / 2) + 1, (startMidPnt.y - startRoomHeight / 2) + 1),
                pointB = new Vector2((startMidPnt.x + startRoomWidth / 2) - 1, (startMidPnt.y - startRoomHeight / 2) + 1)
            };
            Line startTopLine = new Line
            {
                pointA = new Vector2((startMidPnt.x - startRoomWidth / 2) + 1, (startMidPnt.y + startRoomHeight / 2) - 1),
                pointB = new Vector2((startMidPnt.x + startRoomWidth / 2) - 1, (startMidPnt.y + startRoomHeight / 2) - 1)
            };
            Line startLeftLine = new Line
            {
                pointA = new Vector2((startMidPnt.x - startRoomWidth / 2) + 1, (startMidPnt.y + startRoomHeight / 2) - 1),
                pointB = new Vector2((startMidPnt.x - startRoomWidth / 2) + 1, (startMidPnt.y - startRoomHeight / 2) + 1)
            };
            Line startRightLine = new Line
            {
                pointA = new Vector2((startMidPnt.x + startRoomWidth / 2) - 1, (startMidPnt.y + startRoomHeight / 2) - 1),
                pointB = new Vector2((startMidPnt.x + startRoomWidth / 2) - 1, (startMidPnt.y - startRoomHeight / 2) + 1)
            };

            Debug.DrawLine(startBotLine.pointA, startBotLine.pointB, Color.yellow, 3.0f);
            Debug.DrawLine(startTopLine.pointA, startTopLine.pointB, Color.yellow, 3.0f);
            Debug.DrawLine(startLeftLine.pointA, startLeftLine.pointB, Color.yellow, 3.0f);
            Debug.DrawLine(startRightLine.pointA, startRightLine.pointB, Color.yellow, 3.0f);

            // DETERMINE ROUTE

            // Calculate midpoint between each room's midpoints
            Vector2Int roomsMidPnt = new Vector2Int((startMidPnt.x + closestRoomMidPnt.x) / 2, (startMidPnt.y + closestRoomMidPnt.y) / 2);

            Debug.DrawLine(closestRoomMidPnt, new Vector3(roomsMidPnt.x, roomsMidPnt.y, 0), Color.red, 3.0f);

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

            BuildCorridorLShape(new Vector2Int(startMidPnt.x, startMidPnt.y), new Vector2Int(closestRoomMidPnt.x, closestRoomMidPnt.y));

            rooms.Remove(closestRoom);
            connectedRooms.Add(closestRoom);
            startRoom = closestRoom;
        }
    }

    public static bool IsOnLine(Line line, Vector2 point)
    {
        if (line.pointA == point || line.pointB == point)
        {
            return false;
        }

        return Vector2.Distance(line.pointA, point) + Vector2.Distance(line.pointB, point) == Vector2.Distance(line.pointA, line.pointB);
    }

    public Vector3Int GetRandomOverlapPoint(Line startRoomLine, Line nextRoomLine)
    {
        Vector3Int newPoint = Vector3Int.zero;
        bool isValidPoint = false;
        bool isEdgeVertical = false;

        if (startRoomLine.pointA.x == startRoomLine.pointB.x && nextRoomLine.pointA.x == nextRoomLine.pointB.x)
        {
            isEdgeVertical = true;
        }

        while (!isValidPoint)
        {
            // Pick a random point along the starting room's given edge
            int xPos = (int)Random.Range(startRoomLine.pointA.x, startRoomLine.pointB.x);
            int yPos = (int)Random.Range(startRoomLine.pointA.y, startRoomLine.pointB.y);
            newPoint = new Vector3Int(xPos, yPos, 0);

            if (isEdgeVertical)
            {
                // Once the point is picked check if in overlap range
                if (IsOnLine(startRoomLine, new Vector2(startRoomLine.pointA.x, newPoint.y)))
                {
                    if (IsOnLine(nextRoomLine, new Vector2(nextRoomLine.pointA.x, newPoint.y)))
                    {
                        isValidPoint = true;
                        break;
                    }
                }
            }
            else
            {
                // Once the point is picked check if in overlap range
                if (IsOnLine(startRoomLine, new Vector2(newPoint.x, startRoomLine.pointA.y)))
                {
                    if (IsOnLine(nextRoomLine, new Vector2(newPoint.x, nextRoomLine.pointA.y)))
                    {
                        isValidPoint = true;
                        break;
                    }
                }
            }
        }

        return newPoint;
    }

    public void BuildCorridorHorizontal(Vector2Int startPoint, Vector2Int endPoint)
    {
        // Left
        if (startPoint.x > endPoint.x)
        {
            for (int i = startPoint.x; i > endPoint.x - 1; i--)
            {
                corridorTilemap.SetTile(new Vector3Int(i, startPoint.y - 1, 0), corridorTile);
            }
        }
        // Right
        else if (endPoint.x > startPoint.x)
        {
            for (int i = startPoint.x; i < endPoint.x + 1; i++)
            {
                corridorTilemap.SetTile(new Vector3Int(i, startPoint.y, 0), corridorTile);
            }
        }
    }

    public void BuildCorridorVertical(Vector2Int startPoint, Vector2Int endPoint)
    {
        // Down
        if (startPoint.y > endPoint.y)
        {
            for (int i = startPoint.y; i > endPoint.y - 1; i--)
            {
                corridorTilemap.SetTile(new Vector3Int(startPoint.x - 1, i, 0), corridorTile);
            }
        }
        // Up
        else if (endPoint.y > startPoint.y)
        {
            for (int i = startPoint.y; i < endPoint.y + 1; i++)
            {
                corridorTilemap.SetTile(new Vector3Int(startPoint.x, i, 0), corridorTile);
            }
        }
    }

    public void BuildCorridorLShape(Vector2Int startMidPnt, Vector2Int endMidPnt)
    {
        Vector2Int newStart = Vector2Int.zero;

        // Left
        if (startMidPnt.x > endMidPnt.x)
        {
            for (int i = startMidPnt.x; i > endMidPnt.x - 1; i--)
            {
                corridorTilemap.SetTile(new Vector3Int(i, startMidPnt.y, 0), corridorTile);
                if (i == endMidPnt.x)
                {
                    newStart = new Vector2Int(endMidPnt.x, startMidPnt.y);
                }
            }
        }
        // Right
        else if (endMidPnt.x > startMidPnt.x)
        {
            for (int i = startMidPnt.x; i < endMidPnt.x + 1; i++)
            {
                corridorTilemap.SetTile(new Vector3Int(i, startMidPnt.y, 0), corridorTile);
                if (i == endMidPnt.x)
                {
                    newStart = new Vector2Int(endMidPnt.x, startMidPnt.y);
                }
            }
        }

        // Down
        if (startMidPnt.y > endMidPnt.y)
        {
            for (int i = newStart.y; i > endMidPnt.y - 1; i--)
            {
                corridorTilemap.SetTile(new Vector3Int(newStart.x, i, 0), corridorTile);
            }
        }
        // Up
        else if (endMidPnt.y > startMidPnt.y)
        {
            for (int i = newStart.y; i < endMidPnt.y + 1; i++)
            {
                corridorTilemap.SetTile(new Vector3Int(newStart.x, i, 0), corridorTile);
            }
        }
    }

    private void CleanUpCorridors()
    {
        // If void tiles are provided
        if (corridorTilemap != null)
        {
            // Loops through all the positions of the void tile map
            for (int x = 0; x < mapSize.x; x++)
            {
                for (int y = 0; y < mapSize.y; y++)
                {
                    Vector3Int tilePos = new Vector3Int(-x + mapSize.x / 2, -y + mapSize.y / 2, 0);
                    if (corridorTilemap.GetTile(tilePos) != null)
                    {
                        int wallCount = 0;
                        foreach (Vector2Int dir in directions)
                        {
                            bool hitWall = false;

                            if (hitWall)
                            {
                                wallCount++;

                                if (wallCount == 2)
                                {
                                    corridorTilemap.SetTile(tilePos, null);
                                }
                            }
                        }

                        //Debug.DrawLine(new Vector3(tilePos.x + 0.5f, tilePos.y + 0.5f, 0), new Vector3(0, 0, 0), Color.magenta, 10);
                    }

                    //int layerMask = 1 << 8;
                    //Collider2D[] overlapObj = Physics2D.OverlapBoxAll(roomPos, new Vector2Int(roomSize.x + roomMinDistance, roomSize.y + roomMinDistance), roomRot, layerMask);

                    //// If no overlaps have been detected in the layermask
                    //if (overlapObj.Length == 0)
                    //{
                    //    // Location is deemed valid and we are no longer in do/while loop
                    //    isValidLocation = true;

                    //    // Resets the fail safe
                    //    fails = 0;
                    //}
                    //// Otherwise, if overlaps have been detected in the layermask
                    //else if (overlapObj.Length > 0)
                    //{
                    //    // Fail safe is increased
                    //    fails++;
                    //}

                    // Sets the tile at the current position to the randomly selected tile
                    //voidTilemap.SetTile(new Vector3Int(-x + mapSize.x / 2, -y + mapSize.y / 2, 0), voidTiles[tileID]);
                }
            }
        }
    }

    private int GetUnvisitedNeighbourCount(Tilemap tilemap, Vector3Int originalPos)
    {
        int wallCount = 0;
        foreach (Vector2Int dir in directions)
        {
            Vector3Int next = new Vector3Int(originalPos.x + dir.x, originalPos.y + dir.y, 0);
            Tile nextTile = (Tile)tilemap.GetTile(next);

            Node neighbour = new Node { m_tile = nextTile, m_position = next };

            if (neighbour.m_position.x >= -mapSize.x / 2 + (dir.x < 0 ? 1f : 0) && neighbour.m_position.x <= mapSize.x / 2 - (dir.x < 0 ? 1f : 0))
            {
                if (neighbour.m_position.y <= mapSize.y / 2 - (dir.y < 0 ? 1f : 0) && neighbour.m_position.y >= -mapSize.y / 2 + (dir.y < 0 ? 1f : 0))
                {
                    int layerMask = 1 << 8;

                    RaycastHit2D hit = Physics2D.Linecast(new Vector2(originalPos.x + 0.5f, originalPos.y + 0.5f), new Vector2(neighbour.m_position.x + 0.5f, neighbour.m_position.y + 0.5f), layerMask);

                    //Debug.DrawLine(new Vector2(originalPos.x + 0.5f, originalPos.y + 0.5f), new Vector2(neighbour.m_position.x + 0.5f, neighbour.m_position.y + 0.5f), Color.blue, 3.0f);

                    if (hit.collider == null)
                    {
                        wallCount++;
                    }
                }
            }
        }

        return wallCount;
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

    private void ResetCorridors()
    {
        if (corridorTilemap != null)
        {
            corridorTilemap.ClearAllTiles();
        }
    }

    public void ResetMap()
    {
        ResetVoidMap();
        ResetRooms();
        ResetCorridors();
    }
}
