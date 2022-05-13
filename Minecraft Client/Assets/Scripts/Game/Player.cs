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
    private Vector3 lastPosition = new Vector3();
    
    //network
    private Client client = null;
    
    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        client = world.GetClient();
        Cursor.lockState = CursorLockMode.Locked;
        camera = GetComponentInChildren<Camera>();
        transform.position = new Vector3((byte)transform.position.x,
            world.GetHeight((byte)transform.position.x, (byte)transform.position.z) + 2, (byte)transform.position.z);
    }

    private void FixedUpdate()
    {
        transform.Translate(Input.GetAxisRaw("Horizontal") * moveSpeed * Time.fixedDeltaTime, 0, 
            Input.GetAxisRaw("Vertical") * moveSpeed * Time.fixedDeltaTime);
        
        Vector3 position = transform.position;

        if (lastPosition.x != position.x || lastPosition.y != position.y || lastPosition.z != position.z)
        {
            client.SendPositionUpdate(transform.position);
        }
        
        lastPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
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
