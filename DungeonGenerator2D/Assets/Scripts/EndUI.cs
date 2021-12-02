/*
 * File:	EndUI.cs
 *
 * Author: Mara Dusevic (s200494@students.aie.edu.au)
 * Date Created: Thursday 2 Decemember 2021
 * Date Last Modified: Thursday 2 Decemember 2021
 * 
 * Events used for endgame UI.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndUI : MonoBehaviour
{
    public void RestartGame()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("Demo_Scene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
