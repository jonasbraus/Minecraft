using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    //basic
    [SerializeField] private Material material;
    [SerializeField] private GameObject playerPrefab;
    public int worldSize;
    private string name;
    private Dictionary<byte, GameObject> otherPlayers = new Dictionary<byte, GameObject>();
    private Queue<byte> playersToCreate = new Queue<byte>();
    private Queue<Player.PlayerUpdateData> playersToUpdate = new Queue<Player.PlayerUpdateData>();

    //chunks
    private Chunk[,] chunks;
    private Queue<ChunkCoord> chunksToUpdate = new Queue<ChunkCoord>();

    //blocks
    [SerializeField] private BlockData[] blockData;

    //network
    private Client client;
    private string ip;
    private int port;

    private void Start()
    {
        name = PlayerPrefs.GetString("userName");
        ip = PlayerPrefs.GetString("serverIP");
        port = int.Parse(PlayerPrefs.GetString("port"));
        client = new Client(ip, port, this, name);
    }

    public void RemovePlayer(byte id)
    {
        playersToUpdate.Enqueue(new Player.PlayerUpdateData(id, Vector3.zero, true));
    }

    private void Update()
    {
        if (chunksToUpdate.Count > 0)
        {
            ChunkCoord chunk = chunksToUpdate.Dequeue();
            chunks[chunk.x, chunk.z].Update();
        }

        if (playersToCreate.Count > 0)
        {
            byte id = playersToCreate.Dequeue();
            GameObject player = Instantiate(playerPrefab);
            player.transform.position = new Vector3((byte)player.transform.position.x,
                GetHeight((byte)player.transform.position.x, (byte)player.transform.position.z) + 2,
                (byte)player.transform.position.z);
            player.transform.SetParent(gameObject.transform);
            otherPlayers.Add(id, player);
        }

        if (playersToUpdate.Count > 0)
        {
            Player.PlayerUpdateData data = playersToUpdate.Dequeue();
            otherPlayers.TryGetValue(data.id, out GameObject g);
            if (g != null)
            {
                g.transform.position = data.position;
                if (data.destroy)
                {
                    Destroy(g);
                    otherPlayers.Remove(data.id);
                }
            }
        }
    }

    public void UpdatePlayerPosition(Vector3 position, byte id)
    {
        playersToUpdate.Enqueue(new Player.PlayerUpdateData(id, position, false));
    }

    public void AddPlayer(byte id)
    {
        playersToCreate.Enqueue(id);
    }

    public void CreateChunkArray()
    {
        chunks = new Chunk[worldSize, worldSize];
    }

    public void InitChunk(int x, int z, byte[,,] blocks)
    {
        chunks[x, z] = new Chunk(material, new ChunkCoord(x, z), this, blocks);
    }

    //creates an array of chunks
    public void CreateChunks()
    {
        for (int x = 0; x < worldSize; x++)
        {
            for (int z = 0; z < worldSize; z++)
            {
                chunks[x, z].Initialize();
                chunks[x, z].gameObject.transform.SetParent(gameObject.transform);
                chunks[x, z].AddBlocks();
                chunks[x, z].CreateMesh();
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

    //return textures array of a block
    public byte[] GetTextures(byte blockID)
    {
        return blockData[blockID].textures;
    }

    public byte GetHeight(int x, int z)
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
        if (chunks[chunk.x, chunk.z].blocks[xInChunk, yInChunk, zInChunk] != 1)
        {
            chunks[chunk.x, chunk.z].Edit(xInChunk, yInChunk, zInChunk, blockID);
            chunksToUpdate.Enqueue(new ChunkCoord(chunk.x, chunk.z));

            //update surrounding chunks
            if (chunk.x - 1 >= 0)
            {
                chunksToUpdate.Enqueue(new ChunkCoord(chunk.x - 1, chunk.z));
            }

            if (chunk.x + 1 < worldSize)
            {
                chunksToUpdate.Enqueue(new ChunkCoord(chunk.x + 1, chunk.z));
            }

            if (chunk.z - 1 >= 0)
            {
                chunksToUpdate.Enqueue(new ChunkCoord(chunk.x, chunk.z - 1));
            }

            if (chunk.z + 1 < worldSize)
            {
                chunksToUpdate.Enqueue(new ChunkCoord(chunk.x, chunk.z + 1));
            }
        }
    }

    private void OnApplicationQuit()
    {
        client.Disconnect();
    }


    public Client GetClient()
    {
        return client;
    }


    [Serializable]
    public class BlockData
    {
        public string name;
        public byte[] textures = new byte[6];
    }
}