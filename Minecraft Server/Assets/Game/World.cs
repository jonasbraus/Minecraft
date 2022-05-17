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
    private string dataPath;
    private Noise noise = new Noise();

    //chunks
    public Chunk[,] chunks;

    //network
    private Server server;
    [SerializeField] private int transferDelay;
    private void Start()
    {
        dataPath = Application.dataPath;
        
        randomOffsetX = Random.Range(0, 10000);
        randomOffsetZ = Random.Range(0, 10000);

        if (File.Exists(Application.dataPath + "\\save.world"))
        {
            Console.WriteLine("loading world... \n");
            
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
                                chunks[xChunk, zChunk].blocks[x, y, z] = (byte)(save[i] - 32);
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
            Console.WriteLine("generate world... \n");
            
            CreateChunks();
            CreateTrees();
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
    
    
    //Create Trees
    private void CreateTrees()
    {
        for (int x = 4; x < worldSize * Data.chunkWidth - 4; x++)
        {
            for (int z = 4; z < worldSize * Data.chunkWidth - 4; z++)
            {
                if (noise.CheckTree(x, z))
                {
                    ChunkCoord chunk = new ChunkCoord((int)(x / Data.chunkWidth),
                        (int)(z / Data.chunkWidth));
                    int xInChunk = (int)(x - chunk.x * Data.chunkWidth);
                    int yInChunk = (int)(noise.GetHeight(x, z, randomOffsetX, randomOffsetZ));
                    int zInChunk = (int)(z - chunk.z * Data.chunkWidth);

                    int height = Random.Range(5, 9);

                    for (int i = 0; i <= height; i++)
                    {
                        chunks[chunk.x, chunk.z].blocks[xInChunk, yInChunk + i, zInChunk] = 7;
                    }
                    
                    chunks[chunk.x, chunk.z].blocks[xInChunk, yInChunk + height + 2, zInChunk] = 6;
                    
                    for (int ty = -1; ty <= 0; ty++)
                    {
                        for (int tx = -2; tx <= 2; tx++)
                        {
                            for (int tz = -2; tz <= 2; tz++)
                            {
                                ChunkCoord tempChunk = new ChunkCoord((int)((x - tx) / Data.chunkWidth),
                                    (int)((z - tz) / Data.chunkWidth));
                                int xTempInChunk = (int)((x - tx) - tempChunk.x * Data.chunkWidth);
                                int zTempInChunk = (int)((z - tz) - tempChunk.z * Data.chunkWidth);

                                if (chunks[tempChunk.x, tempChunk.z].blocks[xTempInChunk, yInChunk + height + ty,
                                        zTempInChunk] != 7)
                                {
                                    chunks[tempChunk.x, tempChunk.z].blocks[xTempInChunk, yInChunk + height + ty,
                                        zTempInChunk] = 6;
                                }
                            }
                        }
                    }
                    
                    for (int tx = -1; tx <= 1; tx++)
                    {
                        for (int tz = -1; tz <= 1; tz++)
                        {
                            ChunkCoord tempChunk = new ChunkCoord((int)((x - tx) / Data.chunkWidth),
                                (int)((z - tz) / Data.chunkWidth));
                            int xTempInChunk = (int)((x - tx) - tempChunk.x * Data.chunkWidth);
                            int zTempInChunk = (int)((z - tz) - tempChunk.z * Data.chunkWidth);

                            if (chunks[tempChunk.x, tempChunk.z].blocks[xTempInChunk, yInChunk + height + 1,
                                    zTempInChunk] != 7)
                            {
                                chunks[tempChunk.x, tempChunk.z].blocks[xTempInChunk, yInChunk + height + 1,
                                    zTempInChunk] = 6;
                            }
                        }
                    }
                }
            }
        }
    }
    
    //return the ID of a block
    public byte GetBlockID(Vector3 positionInWorld)
    {
        byte height = noise.GetHeight((int)positionInWorld.x, (int)positionInWorld.z, randomOffsetX, randomOffsetZ);
        
        //world pass
        if (positionInWorld.x < 0 || positionInWorld.y < 0 || positionInWorld.z < 0 ||
            positionInWorld.x >= worldSize * Data.chunkWidth || positionInWorld.y > height ||
            positionInWorld.z >= worldSize * Data.chunkWidth)
        {
            return 0;
        }

        //surface pass
        if ((int)positionInWorld.y == height)
        {
            return 3;
        }

        //bedrock pass
        if ((int)positionInWorld.y == 0)
        {

            return 1;
        }

        //dirt pass
        if ((int)positionInWorld.y > height - 3)
        {
            return 2;
        }
        
        //underground pass
        if ((int)positionInWorld.y < height)
        {
            //dirt
            if((int)positionInWorld.y > 6 && (int)positionInWorld.y < height - 3)
            {
                if (noise.Get3DPerlin(positionInWorld, 3, 0.28f, 0.65f))
                {
                    return 2;
                }
            }
            
            //coal
            if((int)positionInWorld.y > 6 && (int)positionInWorld.y < height - 8)
            {
                if (noise.Get3DPerlin(positionInWorld, 5, 0.28f, 0.65f))
                {
                    return 12;
                }
            }
            
            //caves
            if((int)positionInWorld.y > 6 && (int)positionInWorld.y < height - 8)
            {
                if (noise.Get3DPerlin(positionInWorld, 0, 0.18f, 0.5f))
                {
                    return 0;
                }
            }

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


    public void SaveWorld()
    {
        StreamWriter writer = new StreamWriter(dataPath + "\\save.world", false);
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
                            save[i] = (byte)(chunks[xChunk, zChunk].blocks[x, y, z] + 32);
                            i++;
                        }
                    }
                }

                writer.WriteLine(Encoding.ASCII.GetString(save));
                writer.Flush();
            }
        }
        writer.Close();
        Console.WriteLine("world saved! (save.world) \n");
    }
}
