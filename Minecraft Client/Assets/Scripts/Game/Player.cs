using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    //game
    [SerializeField] private float mouseSpeed;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private World world;
    private Camera camera = null;
    private Rigidbody rigidbody;
    
    //network
    private Client client = null;
    
    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        client = world.GetClient();
        Cursor.lockState = CursorLockMode.Locked;
        camera = GetComponentInChildren<Camera>();
    }

    private void FixedUpdate()
    {
        transform.Translate(Input.GetAxisRaw("Horizontal") * moveSpeed * Time.fixedDeltaTime, 0, 
            Input.GetAxisRaw("Vertical") * moveSpeed * Time.fixedDeltaTime);
    }

    private void Update()
    {
        transform.Rotate(0, Input.GetAxis("Mouse X") * mouseSpeed, 0);
        camera.transform.Rotate(-Input.GetAxis("Mouse Y") * mouseSpeed, 0, 0);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Physics.Raycast(transform.position, Vector3.down, 0.96f))
            {
                rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit))
            {
                hit.point += camera.transform.forward / 10;
                client.EditBlock(hit.point, 0);
            }
        }
        
        if (Input.GetMouseButtonDown(1))
        {
            if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit))
            {
                hit.point -= camera.transform.forward / 10;
                client.EditBlock(hit.point, 4);
            }
        }
    }
}
