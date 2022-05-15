using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    //game
    [SerializeField] private float mouseSpeed;
    [SerializeField] private float defaultSpeed = 5;
    [SerializeField] private float sprintSpeed = 8;
    private float walkSpeed;
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
    private Quaternion lastRotation = new Quaternion();
    
    
    //network
    private Client client = null;
    
    private void Start()
    {
        client = world.GetClient();
        
        //lock the cursor
        Cursor.lockState = CursorLockMode.Locked;
        camera = GetComponentInChildren<Camera>();
        transform.position = GetDefaultPlayerPosition();

        walkSpeed = defaultSpeed;
    }

    private void Jump()
    {
        //add velocity up
        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
    }

    private void FixedUpdate()
    {
        //if space was pressed
        if (jumpRequest)
        {
            Jump();
        }

        CalculateVelocity();
        transform.Translate(velocity, Space.World);
        
        //check if position or rotation had changed and send an update
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        if (lastPosition.x != position.x || lastPosition.y != position.y || lastPosition.z != position.z)
        {
            client.SendPositionUpdate(transform.position);
        }

        if (rotation.w != lastRotation.w || rotation.x != lastRotation.x || rotation.y != lastRotation.y ||
            rotation.z != lastRotation.z)
        {
            client.SendRotationUpdate(transform.rotation);
        }
        
        lastPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        lastRotation = transform.rotation;
    }

    private void Update()
    {
        //get the player input
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            walkSpeed = sprintSpeed;
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            walkSpeed = defaultSpeed;
        }
        
        if (isGrounded && Input.GetButton("Jump"))
        {
            jumpRequest = true;
        }

        //get the camera up and down movement and clamp it
        rotX -= mouseY * 1.4f;
        rotX = Mathf.Clamp(rotX, -90, 90);

        camera.transform.localRotation = Quaternion.Euler(rotX * Time.timeScale, 0, 0);
        
        transform.Rotate(0, mouseX * Time.timeScale, 0);

        //send destroy block request to server
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit))
            {
                hit.point += camera.transform.forward / 10;
                client.EditBlock(hit.point, 0);
            }
        }
        
        //send build block request to server
        if (Input.GetMouseButtonDown(1))
        {
            if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit))
            {
                hit.point -= camera.transform.forward / 10;
                client.EditBlock(hit.point, 5);
            }
        }

        if (transform.position.y < 0)
        {
            world.ShowDeadScreen();
        }
    }

    private void CalculateVelocity()
    {
        //check if player can accelerate any faster (there is a max speed)
        if (verticalMomentum > gravity)
        {
            verticalMomentum += Time.fixedDeltaTime * gravity;
        }

        //add velocity for moving, falling and jumping
        velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * walkSpeed;
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        //check colliders on z axis
        if ((velocity.z > 0 && front) || (velocity.z < 0 && back))
        {
            velocity.z = 0;
        }

        //check colliders on x axis
        if ((velocity.x > 0 && right) || (velocity.x < 0 && left))
        {
            velocity.x = 0;
        }

        //check colliders on y axis
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

    //check colliders for -y
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
    
    //check colliders for +y
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

    //check colliders for +z
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
    
    //check colliders for -z
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
    
    //check colliders for -x
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
    
    //check colliders for +x
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

    //stores update data for position and if a player should be removed from the world
    public class PlayerPositionUpdateData
    {
        public PlayerPositionUpdateData(byte id, Vector3 position, bool destroy)
        {
            this.id = id;
            this.position = position;
            this.destroy = destroy;
        }
        
        public byte id;
        public Vector3 position;
        public bool destroy;
    }

    //stores update data for rotations
    public class PlayerRotationsUpdateData
    {
        public byte id;
        public Quaternion rotation;
        
        public PlayerRotationsUpdateData(byte id, Quaternion rotation)
        {
            this.id = id;
            this.rotation = rotation;
        }
    }

    public Vector3 GetDefaultPlayerPosition()
    {
        return new Vector3((byte)transform.position.x,
            world.GetHeight((byte)transform.position.x, (byte)transform.position.z) + 2, (byte)transform.position.z);
    }
}

