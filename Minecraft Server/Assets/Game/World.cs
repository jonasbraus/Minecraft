using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

public class World : MonoBehaviour
{
    //basic
    [SerializeField] public int worldSize;
    private int randomOffsetX, randomOffsetZ;
    private int seed = 0;
    
    //chunks
    public Chunk[,] chunks;

    //network
    private Server server;
    [SerializeField] private int transferDelay;
    private void Start()
    {
        randomOffsetX = Random.Range(0, 10000);
        randomOffsetZ = Random.Range(0, 10000);

        if (File.Exists(Application.dataPath + "\\save.world"))
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
            
            StreamReader reader = new StreamReader(Application.dataPath + "\\save.world");
            for (int xChunk = 0; xChunk < worldSize; xChunk++)
            {
                for (int zChunk = 0; zChunk < worldSize; zChunk++)
                {
                    byte[] save = Encoding.ASCII.GetBytes(reader.ReadLine());
                    
                    int i = 0;

                    for (int y = 0; y < Data.chunkHeight; y++)
                    {
                        for (int x = 0; x < Data.chunkWidth; x++)
                        {
                            for (int z = 0; z < Data.chunkWidth; z++)
                            {
                                chunks[xChunk, zChunk].blocks[x, y, z] = save[i];
                                i++;
                            }
                        }
                    }
                }
            }
            reader.Close();
        }
        else
        {
            CreateChunks();   
        }
        Console.WriteLine("world loaded! \n");
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
        byte height = GetHeight((int)positionInWorld.x + randomOffsetX, (int)positionInWorld.z + randomOffsetZ);
        
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
        SaveWorld();
        server.Disconnect();
    }


    private void SaveWorld()
    {
        StreamWriter writer = new StreamWriter(Application.dataPath + "\\save.world", false);
        for (int xChunk = 0; xChunk < worldSize; xChunk++)
        {
            for (int zChunk = 0; zChunk < worldSize; zChunk++)
            {
                byte[] save = new byte[Data.chunkWidth * Data.chunkWidth * Data.chunkHeight];
                int i = 0;

                for (int y = 0; y < Data.chunkHeight; y++)
                {
                    for (int x = 0; x < Data.chunkWidth; x++)
                    {
                        for (int z = 0; z < Data.chunkWidth; z++)
                        {
                            save[i] = chunks[xChunk, zChunk].blocks[x, y, z];
                            i++;
                        }
                    }
                }

                writer.WriteLine(Encoding.ASCII.GetString(save));
                writer.Flush();
            }
        }
        writer.Close();
        Console.WriteLine("world saved! \n");
    }
}
