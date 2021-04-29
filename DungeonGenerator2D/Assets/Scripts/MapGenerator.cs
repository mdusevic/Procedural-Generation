﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    #region Settings

    [Header("Base Settings")]

    [SerializeField]
    [Tooltip("Tilemap used to spawn tiles and map")]
    public Tilemap tilemap;

    [SerializeField]
    [Tooltip("Sets the size of the map")]
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
    private GameObject[] rooms;

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

    }

    public void GenerateCorridors()
    {

    }

    private void GenerateDoors()
    {

    }

    public void ResetMap()
    {
        tilemap.ClearAllTiles();
    }
}
