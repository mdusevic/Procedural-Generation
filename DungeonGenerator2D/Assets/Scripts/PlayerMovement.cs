/*
 * File:	PlayerMovement.cs
 *
 * Author: Mara Dusevic (s200494@students.aie.edu.au)
 * Date Created: Thursday 2 Decemember 2021
 * Date Last Modified: Thursday 2 Decemember 2021
 * 
 * Basic 2D movement for the player using WASD.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    #region Fields
    [SerializeField]
    private float m_movementSpeed = 5.0f;

    [SerializeField]
    private float m_scrollSpeed = 10.0f;

    private Rigidbody2D m_rigidbody;
    private Camera m_camera;
    private Vector2 m_movement;
    private float m_targetZoom;
    private float m_zoomFactor = 3f;

    #endregion

    // Start is called before the first frame update
    private void Awake()
    {
        m_camera = Camera.main;
        m_targetZoom = m_camera.orthographicSize;
        m_rigidbody = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    private void Update()
    {
        ProcessInputs();
    }

    private void FixedUpdate()
    {
        m_rigidbody.velocity = new Vector2(m_movement.x * m_movementSpeed, m_movement.y * m_movementSpeed);
    }

    private void ProcessInputs()
    {
        float scrollData = Input.GetAxisRaw("Mouse ScrollWheel");

        m_targetZoom -= scrollData * m_zoomFactor;
        m_targetZoom = Mathf.Clamp(m_targetZoom, 4.5f, 12.0f);
        m_camera.orthographicSize = Mathf.Lerp(m_camera.orthographicSize, m_targetZoom, Time.deltaTime * m_scrollSpeed);

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        m_movement = new Vector2(moveX, moveY).normalized;
    }
}
