using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    //game
    [SerializeField] private float mouseSpeed;
    [SerializeField] private float moveSpeed;
    [SerializeField] private World world;
    private Camera camera = null;
    
    //network
    private Client client = null;
    
    private void Start()
    {
        client = world.GetClient();
        Cursor.lockState = CursorLockMode.Locked;
        camera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        transform.Rotate(0, Input.GetAxis("Mouse X") * mouseSpeed, 0);
        camera.transform.Rotate(-Input.GetAxis("Mouse Y") * mouseSpeed, 0, 0);

        float up = 0;
        if (Input.GetKey(KeyCode.Space))
        {
            up = 1;
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            up = -1;
        }
        
        transform.Translate(Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime, up * moveSpeed * Time.deltaTime, 
            Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime);

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hit))
            {
                hit.point += camera.transform.forward / 10;
                client.EditBlock(hit.point, 0);
            }
        }
        
        if (Input.GetMouseButtonDown(1))
        {
            RaycastHit hit;
            if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hit))
            {
                hit.point -= camera.transform.forward / 10;
                client.EditBlock(hit.point, 4);
            }
        }
    }
}
