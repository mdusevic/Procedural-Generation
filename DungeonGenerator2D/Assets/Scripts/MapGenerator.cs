using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

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

    public Tile buildTile = null;

    // Used to determine whether a node has been traversed
    private List<Node> visited = new List<Node>();

    private bool hasFoundNeighbour = false;
    private bool noValidTiles = false;

    public int totalCorridorLimit = 10;

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

        // VARIABLES
        List<Room> connectedRooms = new List<Room>();

        // Resets the tilemap on each generation
        ResetCorridors();

        // Find all rooms in scene
        Room[] rooms = (Room[])FindObjectsOfType(typeof(Room));

        while (rooms.Length != 0)
        {
            // Pick one from random
            int roomID = Random.Range(0, rooms.Length);
            Room startRoom = rooms[roomID];

            // Add to connected rooms list
            connectedRooms.Add(startRoom);
            rooms = rooms.Where((source, index) => index != roomID).ToArray();

            if (rooms.Length <= 0)
            {
                return;
            }

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

                    //Debug.DrawLine(new Vector2(startMidPnt.x, startMidPnt.y), new Vector2(midPntInWorld.x, midPntInWorld.y), Color.blue, 3.0f);

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

            Vector3Int startRoomSize = startRoom.GetComponent<Tilemap>().size;
            int startRoomWidth = startRoomSize.x;
            int startRoomHeight = startRoomSize.y;

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

            //Debug.DrawLine(new Vector2(startMidPnt.x, startMidPnt.y), closestRoomMidPnt, Color.green, 3.0f);

            // DETERMINE ROUTE

            // If true, build vertical corridor upwards
            if (IsOnLine(closestBotLine, new Vector2(startTopLine.pointA.x, closestBotLine.pointA.y)) ||
                IsOnLine(closestBotLine, new Vector2(startTopLine.pointB.x, closestBotLine.pointA.y)))
            {
                Debug.Log("build up");

                Vector3Int randStartPnt = GetRandomOverlapPoint(startTopLine, closestBotLine);
                BuildCorridorVertical(randStartPnt, new Vector3Int(randStartPnt.x, closestRoomMidPnt.y, 0));

                //BuildCorridorVertical(new Vector3Int(closestRoomMidPnt.x, startMidPnt.y, 0), closestRoomMidPnt);
            }
            // If true, build vertical corridor downwards
            else if (IsOnLine(closestTopLine, new Vector2(startBotLine.pointA.x, closestTopLine.pointA.y)) ||
                     IsOnLine(closestTopLine, new Vector2(startBotLine.pointB.x, closestTopLine.pointA.y)))
            {
                Debug.Log("build down");

                Vector3Int randStartPnt = GetRandomOverlapPoint(startBotLine, closestTopLine);
                BuildCorridorVertical(randStartPnt, new Vector3Int(randStartPnt.x, closestRoomMidPnt.y, 0));

                //BuildCorridorVertical(new Vector3Int(closestRoomMidPnt.x, startMidPnt.y, 0), closestRoomMidPnt);
            }
            // If true, build horizontal corridor to the right
            else if (IsOnLine(closestLeftLine, new Vector2(closestLeftLine.pointA.x, startRightLine.pointA.y)) ||
                     IsOnLine(closestLeftLine, new Vector2(closestLeftLine.pointA.x, startRightLine.pointB.y)))
            {
                Debug.Log("build right");

                Vector3Int randStartPnt = GetRandomOverlapPoint(startRightLine, closestLeftLine);
                BuildCorridorVertical(new Vector3Int(closestRoomMidPnt.x, randStartPnt.y, 0), randStartPnt);

                //BuildCorridorHorizontal(startMidPnt, new Vector3Int(closestRoomMidPnt.x, startMidPnt.y, 0));
            }
            // If true, build horizontal corridor to the left
            else if (IsOnLine(closestRightLine, new Vector2(closestRightLine.pointA.x, startLeftLine.pointA.y)) ||
                     IsOnLine(closestRightLine, new Vector2(closestRightLine.pointA.x, startLeftLine.pointB.y)))
            {
                Debug.Log("build left");

                Vector3Int randStartPnt = GetRandomOverlapPoint(startLeftLine, closestRightLine);
                BuildCorridorVertical(new Vector3Int(closestRoomMidPnt.x, randStartPnt.y, 0), randStartPnt);

                //BuildCorridorHorizontal(startMidPnt, new Vector3Int(closestRoomMidPnt.x, startMidPnt.y, 0));
            }
            // Otherwise, build L-shaped corridor
            else
            {
                Debug.Log("L-shape corridor");

                BuildCorridorHorizontal(startMidPnt, new Vector3Int(closestRoomMidPnt.x, startMidPnt.y, 0));
                BuildCorridorVertical(new Vector3Int(closestRoomMidPnt.x, startMidPnt.y, 0), closestRoomMidPnt);
            }
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
        bool isVerticalLine = false;

        if (startRoomLine.pointA.x == startRoomLine.pointB.x && nextRoomLine.pointA.x == nextRoomLine.pointB.x)
        {
            isVerticalLine = true;
        }
        
        while (!isValidPoint)
        {
            // Pick a random point along the starting room's given edge
            int xPos = (int)Random.Range(startRoomLine.pointA.x, startRoomLine.pointB.x);
            int yPos = (int)Random.Range(startRoomLine.pointA.y, startRoomLine.pointB.y);
            newPoint = new Vector3Int(xPos, yPos, 0);

            if (isVerticalLine)
            {
                // Once the point is picked check if in overlap range
                if (IsOnLine(startRoomLine, new Vector2(startRoomLine.pointA.x, newPoint.y)) && IsOnLine(nextRoomLine, new Vector2(nextRoomLine.pointA.x, newPoint.y)))
                {
                    isValidPoint = true;
                }
            }
            else
            {
                // Once the point is picked check if in overlap range
                if (IsOnLine(startRoomLine, new Vector2(newPoint.x, startRoomLine.pointA.y)) && IsOnLine(nextRoomLine, new Vector2(newPoint.x, nextRoomLine.pointA.y)))
                {
                    isValidPoint = true;
                }
            }
        }

        return newPoint;
    }

    public void BuildCorridorHorizontal(Vector3Int startPoint, Vector3Int endPoint)
    {
        if (startPoint.x > endPoint.x)
        {
            for (int i = startPoint.x; i > endPoint.x - 1; i--)
            {
                corridorTilemap.SetTile(new Vector3Int(i, startPoint.y, 0), corridorTile);
            }
        }
        else if (endPoint.x > startPoint.x)
        {
            for (int i = startPoint.x; i < endPoint.x + 1; i++)
            {
                corridorTilemap.SetTile(new Vector3Int(i, startPoint.y, 0), corridorTile);
            }
        }
    }

    public void BuildCorridorVertical(Vector3Int startPoint, Vector3Int endPoint)
    {
        if (startPoint.y > endPoint.y)
        {
            for (int i = startPoint.y; i > endPoint.y - 1; i--)
            {
                corridorTilemap.SetTile(new Vector3Int(startPoint.x, i, 0), corridorTile);
            }
        }
        else if (endPoint.y > startPoint.y)
        {
            for (int i = startPoint.y; i < endPoint.y + 1; i++)
            {
                corridorTilemap.SetTile(new Vector3Int(startPoint.x, i, 0), corridorTile);
            }
        }
    }

    public void GenerateCorridors2()
    {
        if (corridorTile == null && corridorTilemap == null)
        {
            return;
        }

        ResetCorridors();

        bool isFinished = false;
        int corridorFails = 0;
        int corridorCount = 0;

        Stack<Node> stack = new Stack<Node>();

        Node start = new Node();
        Node current = new Node();
        Vector2Int corridorDir = new Vector2Int();

        SetupCorridor(ref start, ref current, ref corridorDir);
        visited.Add(start);
        stack.Push(start);

        do
        {
            if (corridorFails > 60 || corridorCount == totalCorridorLimit)
            {
                isFinished = true;
            }

            Node next = GetUnvisitedNeighbour(corridorTilemap, current.m_position, corridorDir);

            if (!hasFoundNeighbour)
            {
                while (stack.Count == 0)
                {
                    SetupCorridor(ref start, ref current, ref corridorDir);

                    if (noValidTiles)
                    {
                        corridorFails++;
                    }
                    else
                    {
                        visited.Add(start);
                        stack.Push(start);
                        break;
                    }
                }

                if (stack.Count > 0)
                {
                    Node prev = stack.Pop();
                    next = prev;

                    if (next.m_position == start.m_position)
                    {
                        current = next;
                        corridorDir = GetRandomCorridorDirection();
                        corridorCount++;
                    }
                }
            }
            else if (hasFoundNeighbour)
            {
                current = next;
                visited.Add(current);
                stack.Push(current);

                corridorTilemap.SetTile(current.m_position, corridorTile);
            }

            //Debug.Log(current.m_position + " " + corridorFails);

        } while (!isFinished && corridorFails < 60);
    }

    private Node GetCorridorStartTile()
    {
        noValidTiles = false;
        Node start = new Node();
        Vector2Int newTilePos;
        bool isValidTile = false;
        int fails = 0;

        do
        {
            int tilePosX = Random.Range(-mapSize.x / 2 + 1, mapSize.x / 2 - 1);
            int tilePosY = Random.Range(-mapSize.y / 2 + 1, mapSize.y / 2 - 1);

            newTilePos = new Vector2Int(tilePosX, tilePosY);

            int layerMask = 1 << 8;
            Collider2D[] overlapObj = Physics2D.OverlapBoxAll(newTilePos, new Vector2(1, 1), 0, layerMask);

            // If no overlaps have been detected in the layermask
            if (overlapObj.Length == 0)
            {
                // Location is deemed valid and we are no longer in do/while loop
                isValidTile = true;

                fails = 0;
            }
            else if (overlapObj.Length > 0)
            {
                // Fail safe is increased
                fails++;
            }

        } while (!isValidTile && fails < 50 && !noValidTiles);

        if (fails == 50)
        {
            noValidTiles = true;
            return start;
        }

        // Assign starting tile
        Tile startTile = (Tile)corridorTilemap.GetTile(new Vector3Int(newTilePos.x, newTilePos.y, 0));
        start.m_tile = startTile;
        start.m_position = new Vector3Int(newTilePos.x, newTilePos.y, 0);

        if (visited.Contains(start))
        {
            noValidTiles = true;
            return start;
        }

        return start;
    }

    private void SetupCorridor(ref Node start, ref Node current, ref Vector2Int direction)
    {
        start = GetCorridorStartTile();

        if (noValidTiles)
        {
            return;
        }

        corridorTilemap.SetTile(start.m_position, corridorTile);
        current = start;
        direction = GetRandomCorridorDirection();
    }

    private Vector2Int GetRandomCorridorDirection()
    {
        int index = Random.Range(0, directions.Length);
        Vector2Int newDir = directions[index];

        return newDir;
    }

    private Node GetUnvisitedNeighbour(Tilemap tilemap, Vector3Int originalPos, Vector2Int direction)
    {
        hasFoundNeighbour = false;
        Vector3Int next = new Vector3Int(originalPos.x + direction.x, originalPos.y + direction.y, 0);
        Tile nextTile = (Tile)tilemap.GetTile(next);

        Node neighbour = new Node { m_tile = nextTile, m_position = next };

        if (neighbour.m_position.x >= -mapSize.x / 2 + (direction.x < 0 ? 1f : 0) && neighbour.m_position.x <= mapSize.x / 2 - (direction.x < 0 ? 1f : 0))
        {
            if (neighbour.m_position.y <= mapSize.y / 2 - (direction.y < 0 ? 1f : 0) && neighbour.m_position.y >= -mapSize.y / 2 + (direction.y < 0 ? 1f : 0))
            {
                int layerMask = 1 << 8;
                RaycastHit2D hit = Physics2D.Linecast(new Vector2(originalPos.x + 0.5f, originalPos.y + 0.5f), new Vector2(neighbour.m_position.x + 0.5f, neighbour.m_position.y + 0.5f), layerMask);

                //Debug.DrawLine(new Vector2(originalPos.x + 0.5f, originalPos.y + 0.5f), new Vector2(neighbour.m_position.x + 0.5f, neighbour.m_position.y + 0.5f), Color.blue, 3.0f);

                if (hit.collider == null)
                {
                    hasFoundNeighbour = true;
                }
            }
        }

        return neighbour;
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
            visited.Clear();
            noValidTiles = false;
            hasFoundNeighbour = false;
        }
    }

    public void ResetMap()
    {
        ResetVoidMap();
        ResetRooms();
        ResetCorridors();
    }
}
