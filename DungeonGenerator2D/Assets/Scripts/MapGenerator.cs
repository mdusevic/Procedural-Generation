using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct Node
{
    public Tile m_tile;
    public Vector3Int m_position;
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

    private readonly int[] tileDir = { 1, 1, -1, -1 };

    // Used to determine whether a node has been traversed
    private List<Node> visited = new List<Node>();

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
        if (corridorTile == null && corridorTilemap == null)
        {
            return;
        }

        ResetCorridors();

        // Creates a stack for tiles to be processed
        Stack<Node> stack = new Stack<Node>();

        // Randomly find a tile to use as the starting tile
        Vector2Int newTilePos;
        bool isValidTile = false;
        bool isFinished = false;
        int fails = 0;

        while (!isFinished)
        {
            do
            {
                int tilePosX = Random.Range(-mapSize.x / 2, mapSize.x / 2);
                int tilePosY = Random.Range(-mapSize.y / 2, mapSize.y / 2);

                newTilePos = new Vector2Int(tilePosX, tilePosY);

                int layerMask = 1 << 8;
                Collider2D[] overlapObj = Physics2D.OverlapBoxAll(newTilePos, new Vector2(1, 1), 0, layerMask);

                // If no overlaps have been detected in the layermask
                if (overlapObj.Length == 0)
                {
                    // Location is deemed valid and we are no longer in do/while loop
                    isValidTile = true;

                    // Resets the fail safe
                    fails = 0;
                }
                else if (overlapObj.Length > 0)
                {
                    // Fail safe is increased
                    fails++;
                }

            } while (!isValidTile && fails < 50);

            if (fails == 50)
            {
                isFinished = true;
                break;
            }

            // Assign starting tile
            corridorTilemap.SetTile(new Vector3Int(newTilePos.x, newTilePos.y, 0), corridorTile);
            Tile startTile = (Tile)corridorTilemap.GetTile(new Vector3Int(newTilePos.x, newTilePos.y, 0));

            Node start = new Node();
            start.m_tile = startTile;
            start.m_position = new Vector3Int(newTilePos.x, newTilePos.y, 0);

            stack.Push(start);
            visited.Add(start);

            Node current = start;

            while (stack.Count > 0)
            {
                Debug.Log(visited);

                stack.Pop();
                List<Node> neighbourTiles;

                neighbourTiles = GetUnvisitedNeighbours(corridorTilemap, current.m_position);

                if (neighbourTiles == null)
                {
                    Node prev = visited[visited.Count - 1];
                    current = prev;

                    neighbourTiles = GetUnvisitedNeighbours(corridorTilemap, current.m_position);

                    if (current.m_position == start.m_position && neighbourTiles == null)
                    {
                        foreach (Node corridor in visited)
                        {
                            corridorTilemap.SetTile(corridor.m_position, corridorTile);
                        }

                        isValidTile = false;
                        break;
                    }
                }
                else
                {
                    Node newTile = GetRandomNeighbourTile(neighbourTiles);

                    stack.Push(newTile);
                    visited.Add(newTile);

                    current = newTile;
                }
            }
        }

        foreach (Node corridor in visited)
        {
            corridorTilemap.SetTile(corridor.m_position, corridorTile);
        }



        // SET TILES 
        // corridorTilemap.SetTile(new Vector3Int(newTilePos.x, newTilePos.y, 0), corridorTile);

        // CREATE A GET TILE POS FUNCTION



        // Depth First Search Method

        // Pick a random point on tilemap
        // - check for collisions with rooms before finding spot
        // - must be within the map's size

        // Check surrounding pieces for unvisited and valid tiles and push into list
        // - tile must be unvisited and not be surrounded 
        // Pick a random direction from that list 
        // Store prev tile
        // Repeat with new tile until dead end found

        // When a deadend is found, backtrack to previous tile
        // Pick a new direction if any unvisited tiles surround it.
        // If no piece is found backtrack

        // Eventually the tile will return to the initial starting tile

        // Repeat process at a new starting location 
    }

    private List<Node> GetAllTiles(Tilemap tilemap)
    {
        List<Node> allTiles = new List<Node>();

        // Loops through all the positions of the tile map
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                Node node = new Node
                {
                    m_tile = (Tile)tilemap.GetTile(new Vector3Int(x, y, 0)),
                    m_position = new Vector3Int(x, y, 0)
                };
            }
        }

        return allTiles;
    }

    private Node GetRandomNeighbourTile(List<Node> tiles)
    {
        if (tiles.Count == 0)
        {
            return new Node { };
        }

        Node randNeighbour;

        int tileID = Random.Range(0, tiles.Count);
        randNeighbour = tiles[tileID];

        return randNeighbour;
    }

    private List<Node> GetUnvisitedNeighbours(Tilemap tilemap, Vector3Int originalPos)
    {
        List<Node> neighbourTiles = new List<Node>();

        Vector3Int above = new Vector3Int(originalPos.x, originalPos.y + 1, 0);
        Tile aboveTile = (Tile)tilemap.GetTile(above);
        neighbourTiles.Add(new Node { m_tile = aboveTile, m_position = above });

        Vector3Int below = new Vector3Int(originalPos.x, originalPos.y - 1, 0);
        Tile belowTile = (Tile)tilemap.GetTile(below);
        neighbourTiles.Add(new Node { m_tile = belowTile, m_position = below });

        Vector3Int right = new Vector3Int(originalPos.x + 1, originalPos.y, 0);
        Tile rightTile = (Tile)tilemap.GetTile(right);
        neighbourTiles.Add(new Node { m_tile = rightTile, m_position = right });

        Vector3Int left = new Vector3Int(originalPos.x - 1, originalPos.y, 0);
        Tile leftTile = (Tile)tilemap.GetTile(left);
        neighbourTiles.Add(new Node { m_tile = leftTile, m_position = left });

        // Checks if tile is overlapping into a room or is visited. If true, it is removed.
        foreach (Node tile in neighbourTiles)
        {
            foreach (Node visitedTile in visited)
            {
                int layerMask = 1 << 8;
                Collider2D overlapObj = Physics2D.OverlapBox(new Vector2Int(tile.m_position.x, tile.m_position.y), new Vector2(1, 1), 0, layerMask);
                if (visitedTile.m_position == tile.m_position || overlapObj != null)
                {
                    neighbourTiles.Remove(tile);
                }
            }
        }

        return neighbourTiles;
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
