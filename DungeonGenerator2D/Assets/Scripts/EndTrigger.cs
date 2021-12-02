/*
 * File:	EndTrigger.cs
 *
 * Author: Mara Dusevic (s200494@students.aie.edu.au)
 * Date Created: Thursday 2 Decemember 2021
 * Date Last Modified: Thursday 2 Decemember 2021
 * 
 * When attached object is entered by player, 
 * end the game.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndTrigger : MonoBehaviour
{
    #region Fields

    private GameObject m_endGameText;

    #endregion

    private void Start()
    {
        m_endGameText = GameObject.FindGameObjectWithTag("EndUI");
        m_endGameText.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D a_collider)
    {
        if (a_collider.tag == "Player")
        {
            m_endGameText.SetActive(true);
            Time.timeScale = 0.0f;
        }
    }
}
