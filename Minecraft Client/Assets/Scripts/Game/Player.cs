using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    //game
    [SerializeField] private float mouseSpeed;
    [SerializeField] private float walkSpeed = 5;
    [SerializeField] private float jumpForce;
    [SerializeField] private World world;
    private float playerWidth = 0.15f;
    private float playerOffsetWidth = 0.1f;
    private float horizontal;
    private float vertical;
    private float mouseX;
    private float mouseY;
    private Vector3 velocity = new Vector3(0, 0, 0);
    private float rotX = 0;
    private float verticalMomentum = 0;
    private bool jumpRequest;
    private bool isGrounded = true;

    private float gravity = -20f;
    private Camera camera = null;
    private Vector3 lastPosition = new Vector3();
    
    //network
    private Client client = null;
    
    private void Start()
    {
        client = world.GetClient();
        Cursor.lockState = CursorLockMode.Locked;
        camera = GetComponentInChildren<Camera>();
        transform.position = new Vector3((byte)transform.position.x,
            world.GetHeight((byte)transform.position.x, (byte)transform.position.z) + 2, (byte)transform.position.z);
    }

    private void Jump()
    {
        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
    }

    private void FixedUpdate()
    {
        if (jumpRequest)
        {
            Jump();
        }

        CalculateVelocity();
        transform.Translate(velocity, Space.World);
        
        Vector3 position = transform.position;

        if (lastPosition.x != position.x || lastPosition.y != position.y || lastPosition.z != position.z)
        {
            client.SendPositionUpdate(transform.position);
        }
        
        lastPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
    }

    private void Update()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");

        if (isGrounded && Input.GetButton("Jump"))
        {
            jumpRequest = true;
        }

        rotX -= mouseY * 1.4f;
        rotX = Mathf.Clamp(rotX, -90, 90);

        camera.transform.localRotation = Quaternion.Euler(rotX, 0, 0);
        
        transform.Rotate(0, mouseX, 0);

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

    private void CalculateVelocity()
    {
        if (verticalMomentum > gravity)
        {
            verticalMomentum += Time.fixedDeltaTime * gravity;
        }

        velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * walkSpeed;
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        if ((velocity.z > 0 && front) || (velocity.z < 0 && back))
        {
            velocity.z = 0;
        }

        if ((velocity.x > 0 && right) || (velocity.x < 0 && left))
        {
            velocity.x = 0;
        }

        if (velocity.y < 0)
        {
            velocity.y = CheckDownSpeed(velocity.y);
        }
        else if (velocity.y > 0)
        {
            velocity.y = CheckUpSpeed(velocity.y);
        }
        else
        {
            velocity.y = 0;
        }
    }

    private float CheckDownSpeed(float downSpeed)
    {
        if (
            world.CheckBlock(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckBlock(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckBlock(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) ||
            world.CheckBlock(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth))
        )
        {
            isGrounded = true;
            return 0;
        }

        isGrounded = false;
        return downSpeed;
    }
    
    private float CheckUpSpeed(float upSpeed)
    {
        if (
            world.CheckBlock(new Vector3(transform.position.x - playerWidth, transform.position.y + upSpeed + 2f, transform.position.z - playerWidth)) ||
            world.CheckBlock(new Vector3(transform.position.x + playerWidth, transform.position.y + upSpeed + 2f, transform.position.z - playerWidth)) ||
            world.CheckBlock(new Vector3(transform.position.x + playerWidth, transform.position.y + upSpeed + 2f, transform.position.z + playerWidth)) ||
            world.CheckBlock(new Vector3(transform.position.x - playerWidth, transform.position.y + upSpeed + 2f, transform.position.z + playerWidth))
        )
        {
            return 0;
        }
        
        return upSpeed;
    }

    public bool front
    {
        get
        {
            if (
                world.CheckBlock(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth + playerOffsetWidth)) ||
                world.CheckBlock(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth + playerOffsetWidth))
            )
            {
                return true;
            }

            return false;
        }
    }
    
    public bool back
    {
        get
        {
            if (
                world.CheckBlock(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth - playerOffsetWidth)) ||
                world.CheckBlock(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth - playerOffsetWidth))
            )
            {
                return true;
            }

            return false;
        }
    }
    
    public bool left
    {
        get
        {
            if (
                world.CheckBlock(new Vector3(transform.position.x - playerWidth - playerOffsetWidth, transform.position.y, transform.position.z )) ||
                world.CheckBlock(new Vector3(transform.position.x - playerWidth - playerOffsetWidth, transform.position.y + 1f, transform.position.z))
            )
            {
                return true;
            }

            return false;
        }
    }
    
    public bool right
    {
        get
        {
            if (
                world.CheckBlock(new Vector3(transform.position.x + playerWidth + playerOffsetWidth, transform.position.y, transform.position.z)) ||
                world.CheckBlock(new Vector3(transform.position.x + playerWidth + playerOffsetWidth, transform.position.y + 1f, transform.position.z))
            )
            {
                return true;
            }

            return false;
        }
    }

    public class PlayerUpdateData
    {
        public PlayerUpdateData(byte id, Vector3 position, bool destroy)
        {
            this.id = id;
            this.position = position;
            this.destroy = destroy;
        }
        
        public byte id;
        public Vector3 position;
        public bool destroy;
    }
}

