using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

public class Chunk
{
    //basic
    private ChunkCoord chunkPosition;
    private World world;
    
    //rendering
    public GameObject gameObject;

    //data
    public byte[,,] blocks = new byte[Data.chunkWidth, Data.chunkHeight, Data.chunkWidth];

    public Chunk(ChunkCoord chunkPosition, World world)
    {
        this.chunkPosition = chunkPosition;
        this.world = world;
        
        Initialize();
    }
    

    //initializes the chunks components
    private void Initialize()
    {
        gameObject = new GameObject(chunkPosition.x + ", " + chunkPosition.z);
        gameObject.transform.position =
            new Vector3(chunkPosition.x * Data.chunkWidth, 0, chunkPosition.z * Data.chunkWidth);
    }

    //writes the block data
    public void PopulateBlocks()
    {
        for (int y = 0; y < Data.chunkHeight; y++)
        {
            for (int x = 0; x < Data.chunkWidth; x++)
            {
                for (int z = 0; z < Data.chunkWidth; z++)
                {
                    blocks[x, y, z] = world.GetBlockID(new Vector3(x + chunkPosition.x * Data.chunkWidth, 
                        y, z + chunkPosition.z * Data.chunkWidth));
                }
            }
        }
    }

    public void Update(byte x, byte y, byte z, byte blockID)
    {
        blocks[x, y, z] = blockID;
    }
}
