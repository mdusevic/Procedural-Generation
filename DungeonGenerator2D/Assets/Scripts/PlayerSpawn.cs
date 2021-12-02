/*
 * File:	PlayerSpawn.cs
 *
 * Author: Mara Dusevic (s200494@students.aie.edu.au)
 * Date Created: Thursday 2 Decemember 2021
 * Date Last Modified: Thursday 2 Decemember 2021
 * 
 * Spawns the player at runtime at the given position.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null)
        {
            player.transform.position = gameObject.transform.position;
        }
    }
}
