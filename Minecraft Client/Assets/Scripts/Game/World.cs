using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public class World : MonoBehaviour
{
    //basic
    [SerializeField] private Material material;
    [SerializeField] private GameObject playerPrefab;
    public int worldSize;
    private string name;
    private Dictionary<byte, GameObject> otherPlayers = new Dictionary<byte, GameObject>();
    private Queue<byte> playersToCreate = new Queue<byte>();
    private Queue<Player.PlayerPositionUpdateData> playersPositionsToUpdate = new Queue<Player.PlayerPositionUpdateData>();
    
    private Queue<Player.PlayerRotationsUpdateData> playersRotationsToUpdate =
        new Queue<Player.PlayerRotationsUpdateData>();

    private bool backToMenuRequest = false;

    [SerializeField] private Player player;
    [SerializeField] private GameObject deadScreen;
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private GameObject optionsScreen;
    private ChunkCoord lastPlayerChunkCoord = new ChunkCoord(0, 0);

    //chunks
    private Chunk[,] chunks;
    private Queue<ChunkCoord> chunksToUpdate = new Queue<ChunkCoord>();
    private Queue<ChunkCoord> chunksToUpdate1 = new Queue<ChunkCoord>();
    private List<Chunk> lastActiveChunks = new List<Chunk>();

    //blocks
    [SerializeField] public BlockData[] blockData;

    //network
    private Client client;
    private string ip;
    private int port;

    private void Start()
    {
        //get default information

        if (PlayerPrefs.HasKey("viewDistance"))
        {
            Data.viewDistance = PlayerPrefs.GetInt("viewDistance");
        }
        
        name = PlayerPrefs.GetString("userName");
        ip = PlayerPrefs.GetString("serverIP");
        port = int.Parse(PlayerPrefs.GetString("port"));
        client = new Client(ip, port, this, name);
    }

    //removes a player from the game world
    public void RemovePlayer(byte id)
    {
        playersPositionsToUpdate.Enqueue(new Player.PlayerPositionUpdateData(id, Vector3.zero, true));
    }

    private void Update()
    {
        ChunkCoord playerChunkCoord = new ChunkCoord((int)(player.transform.position.x / Data.chunkWidth),
            (int)(player.transform.position.z / Data.chunkWidth));

        if (lastPlayerChunkCoord.x != playerChunkCoord.x || lastPlayerChunkCoord.z != playerChunkCoord.z)
        {
            LoadChunks(playerChunkCoord);
        }

        lastPlayerChunkCoord = new ChunkCoord((int)(player.transform.position.x / Data.chunkWidth),
            (int)(player.transform.position.z / Data.chunkWidth));
        
        //update 1 chunk in list
        if (chunksToUpdate.Count > 0)
        {
            ChunkCoord chunk = chunksToUpdate.Dequeue();
            chunks[chunk.x, chunk.z].Update();
        }
        
        //update 1 chunk in list
        if (chunksToUpdate1.Count > 0)
        {
            ChunkCoord chunk = chunksToUpdate1.Dequeue();
            chunks[chunk.x, chunk.z].Update();
        }

        //create 1 player in list
        if (playersToCreate.Count > 0)
        {
            byte id = playersToCreate.Dequeue();
            GameObject player = Instantiate(playerPrefab);
            player.transform.position = new Vector3((int)player.transform.position.x,
                GetHeight((byte)player.transform.position.x, (byte)player.transform.position.z) + 2,
                (int)player.transform.position.z);
            player.transform.SetParent(gameObject.transform);
            otherPlayers.Add(id, player);
        }

        //update 1 player position in list
        if (playersPositionsToUpdate.Count > 0)
        {
            Player.PlayerPositionUpdateData data = playersPositionsToUpdate.Dequeue();
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

        //update 1 player rotation in list
        if (playersRotationsToUpdate.Count > 0)
        {
            Player.PlayerRotationsUpdateData data = playersRotationsToUpdate.Dequeue();
            otherPlayers.TryGetValue(data.id, out GameObject g);
            if (g != null)
            {
                g.transform.rotation = data.rotation;
            }
        }
        
        //go bock to menu
        if (backToMenuRequest)
        {
            SceneManager.LoadScene("Scenes/Login", LoadSceneMode.Single);
        }
    }

    private void LoadChunks(ChunkCoord playerChunk)
    {
        List<Chunk> checkList = new List<Chunk>();
        
        chunksToUpdate1.Clear();
        
        for (int x = playerChunk.x - Data.viewDistance; x < playerChunk.x + Data.viewDistance; x++)
        {
            for (int z = playerChunk.z - Data.viewDistance; z < playerChunk.z + Data.viewDistance; z++)
            {
                if(z >= 0 && x >= 0 && z < worldSize && x < worldSize)
                {
                    if (chunks[x, z].gameObject == null)
                    {
                        chunks[x, z].Initialize();
                        chunks[x, z].gameObject.transform.SetParent(gameObject.transform);
                    }

                    if (!chunks[x, z].active)
                    {
                        chunksToUpdate1.Enqueue(new ChunkCoord(x, z));
                    }

                    checkList.Add(chunks[x, z]);
                }
            }
        }

        foreach (Chunk c in lastActiveChunks)
        {
            if (!checkList.Contains(c))
            {
                c.DestroyMesh();
            }
        }
        
        lastActiveChunks.Clear();

        lastActiveChunks = new List<Chunk>(checkList);
    }
    

    //enqueue player position update to list
    public void UpdatePlayerPosition(Vector3 position, byte id)
    {
        if(playersPositionsToUpdate.Count < 50)
        {
            playersPositionsToUpdate.Enqueue(new Player.PlayerPositionUpdateData(id, position, false));
        }
    }

    //enqueue player rotation update to list
    public void UpdatePlayerRotation(Quaternion rotation, byte id)
    {
        if(playersRotationsToUpdate.Count < 50)
        {
            playersRotationsToUpdate.Enqueue(new Player.PlayerRotationsUpdateData(id, rotation));
        }
    }

    //enqueue player create data to list
    public void AddPlayer(byte id)
    {
        if(!otherPlayers.ContainsKey(id))
        {
            playersToCreate.Enqueue(id);
        }
    }

    //creates the array for chunk objects
    public void CreateChunkArray()
    {
        chunks = new Chunk[worldSize, worldSize];
    }

    //initialized a chunk
    public void InitChunk(int x, int z, byte[,,] blocks)
    {
        chunks[x, z] = new Chunk(material, new ChunkCoord(x, z), this, blocks);
    }

    //initialized the chunk objects
    public void CreateChunks()
    {
        for (int x = worldSize / 2 - Data.viewDistance; x < worldSize / 2 + Data.viewDistance; x++)
        {
            for (int z = worldSize / 2 - Data.viewDistance; z < worldSize / 2 + Data.viewDistance; z++)
            {
                chunks[x, z].Initialize();
                chunks[x, z].gameObject.transform.SetParent(gameObject.transform);
                chunksToUpdate.Enqueue(new ChunkCoord(x, z));
                lastActiveChunks.Add(chunks[x, z]);
            }
        }
    }

    //returns if a block is on the position
    public bool CheckBlock(Vector3 positionInWorld)
    {
        if (positionInWorld.x < 0 || positionInWorld.z < 0 ||
            positionInWorld.x >= worldSize * Data.chunkWidth ||
            positionInWorld.z >= worldSize * Data.chunkWidth)
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
        int yInChunk = (int)(positionInWorld.y);
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

    //get the height
    public byte GetHeight(int x, int z)
    {
        int xChunk = x / Data.chunkWidth;
        int zChunk = z / Data.chunkWidth;

        int xInChunk = x - (xChunk * Data.chunkWidth);
        int zInChunk = z - (zChunk * Data.chunkWidth);

        Chunk c = chunks[xChunk, zChunk];
        for (int y = Data.chunkHeight - 1; y > 0; y--)
        {
            if (c.blocks[xInChunk, y, zInChunk] != 0)
            {
                return (byte)(y + 1);
            }
        }

        return 250;
    }

    //edit a block in local chunk storage
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

    public bool IsBlockTransparent(byte id)
    {
        return blockData[id].transparent;
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
        public bool transparent;
    }
    
        
    public void ShowDeadScreen()
    {
        client.SendDeadMessage();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        deadScreen.SetActive(true);
        Time.timeScale = 0;
    }
    
    public void ShowPauseScreen()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        optionsScreen.SetActive(false);
        pauseScreen.SetActive(true);
        Time.timeScale = 0;

        if (PlayerPrefs.HasKey("viewDistance"))
        {
            int value = PlayerPrefs.GetInt("viewDistance");
            if (value != Data.viewDistance)
            {
                Data.viewDistance = value;
                LoadChunks(new ChunkCoord((int)(player.transform.position.x / Data.chunkWidth),
                    (int)(player.transform.position.z / Data.chunkWidth)));
            }
        }
    }
    
    public void RespawnButton()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
        Time.timeScale = 1;
        player.transform.position = player.GetDefaultPlayerPosition();
        deadScreen.SetActive(false);
        LoadChunks(new ChunkCoord((int)(player.transform.position.x / Data.chunkWidth),
            (int)(player.transform.position.z / Data.chunkWidth)));
    }
    
    public void ResumeButton()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
        Time.timeScale = 1;
        pauseScreen.SetActive(false);
    }
    
    public void QuitButton()
    {
        Time.timeScale = 1;
        client.Disconnect();
        SceneManager.LoadScene("Login");
        deadScreen.SetActive(false);
    }

    public void ShowOptionsScreen()
    {
        pauseScreen.SetActive(false);
        optionsScreen.SetActive(true);
    }

    public void SaveButton()
    {
        client.SaveWorld();
    }
    
    //return the ID of a block
    public byte GetBlockID(Vector3 positionInWorld)
    {
        if (!(positionInWorld.x < 0 || positionInWorld.y < 0 || positionInWorld.z < 0 ||
             positionInWorld.x >= worldSize * Data.chunkWidth || positionInWorld.y >= Data.chunkHeight ||
             positionInWorld.z >= worldSize * Data.chunkWidth))
        {
            ChunkCoord chunk = new ChunkCoord((int)(positionInWorld.x / Data.chunkWidth),
                (int)(positionInWorld.z / Data.chunkWidth));

            int xInChunk = (int)(positionInWorld.x - chunk.x * Data.chunkWidth);
            int yInChunk = (int)positionInWorld.y;
            int zInChunk = (int)(positionInWorld.z - chunk.z * Data.chunkWidth);

            return chunks[chunk.x, chunk.z].blocks[xInChunk, yInChunk, zInChunk];   
        }

        return 0;
    }
}