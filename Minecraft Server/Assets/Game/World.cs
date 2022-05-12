using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    //basic
    [SerializeField] public int worldSize;
    
    //chunks
    public Chunk[,] chunks;

    //network
    private Server server;
    [SerializeField] private int transferDelay;

    private void Start()
    {
        CreateChunks();
        server = new Server(this, transferDelay);
    }

    //creates an array of chunks
    private void CreateChunks()
    {
        chunks = new Chunk[worldSize, worldSize];
        for (int x = 0; x < worldSize; x++)
        {
            for (int z = 0; z < worldSize; z++)
            {
                chunks[x, z] = new Chunk(new ChunkCoord(x, z), this);
                chunks[x, z].gameObject.transform.SetParent(gameObject.transform);
            }
        }
        
        for (int x = 0; x < worldSize; x++)
        {
            for (int z = 0; z < worldSize; z++)
            {
                chunks[x, z].PopulateBlocks();
            }
        }
    }

    //return the ID of a block
    public byte GetBlockID(Vector3 positionInWorld)
    {
        byte height = GetHeight((int)positionInWorld.x, (int)positionInWorld.z);
        
        //world pass
        if (positionInWorld.x < 0 || positionInWorld.y < 0 || positionInWorld.z < 0 ||
            positionInWorld.x >= worldSize * Data.chunkWidth || positionInWorld.y > height ||
            positionInWorld.z >= worldSize * Data.chunkWidth)
        {
            return 0;
        }

        //chunk pass
        if ((int)positionInWorld.y == height)
        {
            return 3;
        }

        if ((int)positionInWorld.y == 0)
        {
            return 1;
        }

        if ((int)positionInWorld.y > height - 3)
        {
            return 2;
        }
        
        if ((int)positionInWorld.y < height)
        {
            return 4;
        }

        return 0;
    }
    
    //returns if a block is on the position
    public bool CheckBlock(Vector3 positionInWorld)
    {
        if (positionInWorld.x < -1 || positionInWorld.z < -1 ||
            positionInWorld.x > worldSize * Data.chunkWidth ||
            positionInWorld.z > worldSize * Data.chunkWidth)
        {
            return false;
        }

        
        if (positionInWorld.x < 0 || positionInWorld.y < 0 || positionInWorld.z < 0 ||
            positionInWorld.x >= worldSize * Data.chunkWidth || positionInWorld.y >= Data.chunkHeight ||
            positionInWorld.z >= worldSize * Data.chunkWidth)
        {
            return true;
        }

        ChunkCoord chunk = new ChunkCoord((int)(positionInWorld.x / Data.chunkWidth),
            (int)(positionInWorld.z / Data.chunkWidth));

        int xInChunk = (int)(positionInWorld.x - chunk.x * Data.chunkWidth);
        int yInChunk = (int)positionInWorld.y;
        int zInChunk = (int)(positionInWorld.z - chunk.z * Data.chunkWidth);

        if (chunks[chunk.x, chunk.z] != null)
        {
            if (chunks[chunk.x, chunk.z].blocks[xInChunk, yInChunk, zInChunk] == 0)
            {
                return false;
            }
        }

        return true;
    }

    private byte GetHeight(int x, int z)
    {
        float scale = 0.025f;
        byte perlinHeight = 10;
        byte groundHeight = 10;

        byte height = (byte)(Mathf.PerlinNoise((x) * scale + 0.1f, (z) * scale + 0.1f) * perlinHeight + groundHeight);
        return height;
    }

    public void EditBlock(ChunkCoord chunk, Vector3 positionInChunk, byte blockID)
    {
        byte xInChunk = (byte)(positionInChunk.x);
        byte yInChunk = (byte)positionInChunk.y;
        byte zInChunk = (byte)(positionInChunk.z);
        
        //edit block in chunk
        if(chunks[chunk.x, chunk.z].blocks[xInChunk, yInChunk, zInChunk] != 1)
        {
            chunks[chunk.x, chunk.z].Update(xInChunk, yInChunk, zInChunk, blockID);
        }
    }
    
    private void OnApplicationQuit()
    {
        server.Disconnect();
    }


    private void SaveWorld()
    {
    }
}
