using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Node
{
    public Tile m_tile;
    public Vector2 m_position;
    public Node m_prevNode;

    public Node()
    {

    }

    public Node(Tile a_tile, Vector2 a_position)
    {
        m_tile = a_tile;
        m_position = a_position;
        m_prevNode = null;
    }

    public Node(Tile a_tile, Vector2 a_position, Node a_prevTile)
    {
        m_tile = a_tile;
        m_position = a_position;
        m_prevNode = a_prevTile;
    }

    public Vector3Int TilePos()
    {
        return new Vector3Int(Mathf.RoundToInt(m_position.x), Mathf.RoundToInt(m_position.y), 0);
    }
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
    public Tile roomWallTile = null;

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

    [SerializeField]
    public Tile buildTile = null;

    private readonly Vector2Int[] directions = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

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
            ResetCorridors();

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

            rooms.Remove(closestRoom);
            connectedRooms.Add(closestRoom);
            startRoom = closestRoom;
        }

        CleanUpCorridors();
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
            int xPos = Mathf.RoundToInt(Random.Range(startRoomLine.pointA.x, startRoomLine.pointB.x));
            int yPos = Mathf.RoundToInt(Random.Range(startRoomLine.pointA.y, startRoomLine.pointB.y));
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
                Vector3 cellPos = corridorTilemap.GetCellCenterWorld(new Vector3Int(i, startPoint.y, 0));
                corridorTilemap.SetTile(new Vector3Int(i, startPoint.y, 0), corridorTile);
            }
        }
        // Right
        else if (endPoint.x > startPoint.x)
        {
            for (int i = startPoint.x; i < endPoint.x + 1; i++)
            {
                corridorTilemap.SetTile(new Vector3Int(i, startPoint.y - 1, 0), corridorTile);
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
                corridorTilemap.SetTile(new Vector3Int(startPoint.x, i, 0), corridorTile);
            }
        }
        // Up
        else if (endPoint.y > startPoint.y)
        {
            for (int i = startPoint.y; i < endPoint.y + 1; i++)
            {
                corridorTilemap.SetTile(new Vector3Int(startPoint.x - 1, i, 0), corridorTile);
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

    public void CleanUpCorridors()
    {
        if (corridorTilemap != null)
        {
            for (int x = 0; x < mapSize.x; x++)
            {
                for (int y = 0; y < mapSize.y; y++)
                {
                    Vector3Int tilePos = new Vector3Int(-x + mapSize.x / 2, -y + mapSize.y / 2, 0);

                    if (corridorTilemap.GetTile(tilePos) != null)
                    {
                        int layerMask = 1 << 8;
                        RaycastHit2D hit = Physics2D.Linecast(new Vector2(tilePos.x + 0.5f, tilePos.y + 0.5f), new Vector2(tilePos.x + 0.5f, tilePos.y + 0.5f), layerMask);

                        if (hit.collider != null)
                        {
                            corridorTilemap.SetTile(tilePos, null);
                        }
                    }
                }
            }
        }
    }

    private List<Vector2> WallCollisionDirection(Tilemap tilemap, Vector2 tilePos)
    {
        List<Vector2> collisionDirections = new List<Vector2>();

        foreach (Vector2Int dir in directions)
        {
            Vector2 pos = new Vector2(tilePos.x + dir.x, tilePos.y + dir.y);
            Vector3Int next = new Vector3Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), 0);
            Tile nextTile = (Tile)tilemap.GetTile(next);

            Node neighbour = new Node(nextTile, pos);

            if (neighbour.m_position.x >= -mapSize.x / 2 + (dir.x < 0 ? 1f : 0) && neighbour.m_position.x <= mapSize.x / 2 - (dir.x < 0 ? 1f : 0))
            {
                if (neighbour.m_position.y <= mapSize.y / 2 - (dir.y < 0 ? 1f : 0) && neighbour.m_position.y >= -mapSize.y / 2 + (dir.y < 0 ? 1f : 0))
                {
                    int layerMask = 1 << 8;
                    RaycastHit2D hit = Physics2D.Linecast(new Vector2(tilePos.x + 0.5f, tilePos.y + 0.5f), new Vector2(neighbour.m_position.x + 0.5f, neighbour.m_position.y + 0.5f), layerMask);

                    Debug.DrawLine(new Vector2(tilePos.x + 0.5f, tilePos.y + 0.5f), new Vector2(neighbour.m_position.x + 0.5f, neighbour.m_position.y + 0.5f), Color.blue, 3.0f);

                    if (hit.collider != null)
                    {
                        collisionDirections.Add(dir);
                    }
                }
            }
        }

        return collisionDirections;
    }

    public void CheckForWall()
    {
        if (corridorTilemap == null)
        {
            return;
        }

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                Vector3Int tilePos = new Vector3Int(-x + mapSize.x / 2, -y + mapSize.y / 2, 0);

                if (corridorTilemap.GetTile(tilePos) == null)
                {
                    break;
                }

                CheckForWalls(tilePos);
            }
        }
    }

    private void CheckForWalls(Vector3Int tilePos)
    {
        foreach (Vector2Int dir in directions)
        {
            Vector2 pos = new Vector2(tilePos.x + dir.x, tilePos.y + dir.y);
            Vector3Int next = new Vector3Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), 0);
            Tile nextTile = (Tile)corridorTilemap.GetTile(next);

            Node neighbour = new Node(nextTile, pos);

            if (neighbour.m_position.x >= -mapSize.x / 2 + (dir.x < 0 ? 1f : 0) && neighbour.m_position.x <= mapSize.x / 2 - (dir.x < 0 ? 1f : 0))
            {
                if (neighbour.m_position.y <= mapSize.y / 2 - (dir.y < 0 ? 1f : 0) && neighbour.m_position.y >= -mapSize.y / 2 + (dir.y < 0 ? 1f : 0))
                {
                    int layerMask = 1 << 8;
                    RaycastHit2D hit = Physics2D.Linecast(new Vector2(tilePos.x + 0.5f, tilePos.y + 0.5f), new Vector2(neighbour.m_position.x + 0.5f, neighbour.m_position.y + 0.5f), layerMask);

                    if (hit.collider != null)
                    {
                        bool isCornerWall = CheckForCornerWall(neighbour.m_position, dir);

                        if (isCornerWall)
                        {
                            corridorTilemap.SetTile(tilePos, buildTile);
                        }
                    }
                }
            }
        }
    }

    private bool CheckForCornerWall(Vector2 tilePos, Vector2Int direction)
    {
        Vector2 pos = new Vector2(tilePos.x + direction.x, tilePos.y + direction.y);
        Vector3Int next = new Vector3Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), 0);
        Tile nextTile = (Tile)corridorTilemap.GetTile(next);

        Node neighbour = new Node(nextTile, pos);

        if (neighbour.m_position.x >= -mapSize.x / 2 + (direction.x < 0 ? 1f : 0) && neighbour.m_position.x <= mapSize.x / 2 - (direction.x < 0 ? 1f : 0))
        {
            if (neighbour.m_position.y <= mapSize.y / 2 - (direction.y < 0 ? 1f : 0) && neighbour.m_position.y >= -mapSize.y / 2 + (direction.y < 0 ? 1f : 0))
            {
                int layerMask = 1 << 8;
                RaycastHit2D hit = Physics2D.Linecast(new Vector2(tilePos.x + 0.5f, tilePos.y + 0.5f), new Vector2(neighbour.m_position.x + 0.5f, neighbour.m_position.y + 0.5f), layerMask);

                if (hit.collider != null)
                {
                    Room roomHit = hit.collider.gameObject.GetComponent<Room>();

                    Vector3 roomTilePos = roomHit.GetComponent<Tilemap>().WorldToCell(next);
                    Vector3Int roundedPos = new Vector3Int(Mathf.RoundToInt(roomTilePos.x), Mathf.RoundToInt(roomTilePos.y), 0);

                    Debug.DrawLine(new Vector2(0, 0), new Vector2(roomTilePos.x + 0.5f, roomTilePos.y + 0.5f), Color.blue, 10.0f, false);

                    if (roomHit.GetComponent<Tilemap>().GetTile(roundedPos) == roomWallTile)
                    {
                        roomHit.GetComponent<Tilemap>().SetTile(roundedPos, buildTile);
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private Node GetUnvisitedNeighbour(Tilemap tilemap, Vector2 originalPos, Vector2 direction)
    {
        Vector2 pos = new Vector2(originalPos.x + direction.x, originalPos.y + direction.y);
        Vector3Int next = new Vector3Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), 0);
        Tile nextTile = (Tile)tilemap.GetTile(next);

        Node neighbour = new Node(nextTile, pos);

        if (neighbour.m_position.x >= -mapSize.x / 2 + (direction.x < 0 ? 1f : 0) && neighbour.m_position.x <= mapSize.x / 2 - (direction.x < 0 ? 1f : 0))
        {
            if (neighbour.m_position.y <= mapSize.y / 2 - (direction.y < 0 ? 1f : 0) && neighbour.m_position.y >= -mapSize.y / 2 + (direction.y < 0 ? 1f : 0))
            {
                int layerMask = 1 << 8;
                RaycastHit2D hit = Physics2D.Linecast(new Vector2(originalPos.x + 0.5f, originalPos.y + 0.5f), new Vector2(neighbour.m_position.x + 0.5f, neighbour.m_position.y + 0.5f), layerMask);

                //Debug.DrawLine(new Vector2(originalPos.x + 0.5f, originalPos.y + 0.5f), new Vector2(neighbour.m_position.x + 0.5f, neighbour.m_position.y + 0.5f), Color.green, 6.0f);

                if (hit.collider == null)
                {
                    return neighbour;
                }
            }
        }

        return null;
    }

    private List<Vector2> GetValidDirections(Tilemap tilemap, Vector2 originalPos)
    {
        List<Vector2> validDirections = new List<Vector2>();

        foreach (Vector2Int dir in directions)
        {
            Vector2 pos = new Vector2(originalPos.x + dir.x, originalPos.y + dir.y);
            Vector3Int next = new Vector3Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), 0);
            Tile nextTile = (Tile)tilemap.GetTile(next);

            Node neighbour = new Node(nextTile, pos);

            if (neighbour.m_position.x >= -mapSize.x / 2 + (dir.x < 0 ? 1f : 0) && neighbour.m_position.x <= mapSize.x / 2 - (dir.x < 0 ? 1f : 0))
            {
                if (neighbour.m_position.y <= mapSize.y / 2 - (dir.y < 0 ? 1f : 0) && neighbour.m_position.y >= -mapSize.y / 2 + (dir.y < 0 ? 1f : 0))
                {
                    int layerMask = 1 << 8;
                    RaycastHit2D hit = Physics2D.Linecast(new Vector2(originalPos.x + 0.5f, originalPos.y + 0.5f), new Vector2(neighbour.m_position.x + 0.5f, neighbour.m_position.y + 0.5f), layerMask);

                    Debug.DrawLine(new Vector2(originalPos.x + 0.5f, originalPos.y + 0.5f), new Vector2(neighbour.m_position.x + 0.5f, neighbour.m_position.y + 0.5f), Color.blue, 3.0f);

                    if (hit.collider == null)
                    {
                        validDirections.Add(dir);
                    }
                }
            }
        }

        return validDirections;
    }

    public void GenerateDoors()
    {
        CheckForWall();
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
