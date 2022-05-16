using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

public class Chunk
{
    //basic
    private Material material;
    private ChunkCoord chunkPosition;
    private World world;
    
    //rendering
    public GameObject gameObject;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    //data
    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector2> uv = new List<Vector2>();
    private List<int> triangles = new List<int>();
    public byte[,,] blocks;
    private int triangleIndex = 0;
    
    //physics
    private MeshCollider collider;

    public Chunk(Material material, ChunkCoord chunkPosition, World world, byte[,,] blocks)
    {
        this.blocks = blocks;
        this.material = material;
        this.chunkPosition = chunkPosition;
        this.world = world;
    }
    

    //initializes the chunks components
    public void Initialize()
    {
        gameObject = new GameObject(chunkPosition.x + ", " + chunkPosition.z);
        gameObject.transform.position =
            new Vector3(chunkPosition.x * Data.chunkWidth, 0, chunkPosition.z * Data.chunkWidth);
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        collider = gameObject.AddComponent<MeshCollider>();
    }

    public void AddBlocks()
    {
        for (int y = 0; y < Data.chunkHeight; y++)
        {
            for (int x = 0; x < Data.chunkWidth; x++)
            {
                for (int z = 0; z < Data.chunkWidth; z++)
                {
                    AddBlock(x, y, z);
                }
            }
        }
    }

    //adds the data of a single block
    private void AddBlock(int x, int y, int z)
    {
        byte[] textures = world.GetTextures(blocks[x, y, z]);
        
        //only draw solid blocks
        if(blocks[x, y, z] != 0)
        {
            for (int f = 0; f < 6; f++)
            {
                //check for connecting faces
                if (!world.CheckBlock(new Vector3(x + Data.faceChecks[f].x + chunkPosition.x * Data.chunkWidth, 
                        y + Data.faceChecks[f].y, 
                        z + Data.faceChecks[f].z + chunkPosition.z * Data.chunkWidth)))
                {
                    for (int v = 0; v < 6; v++)
                    {
                        vertices.Add(Data.vertices[Data.triangles[f, v]] + new Vector3(x, y, z));
                        triangles.Add(triangleIndex);
                        triangleIndex++;
                    }
                    
                    AddUV(textures[f]);
                }
                else if(world.IsBlockTransparent(world.GetBlockID(new Vector3(x + Data.faceChecks[f].x + chunkPosition.x * Data.chunkWidth, 
                            y + Data.faceChecks[f].y, 
                            z + Data.faceChecks[f].z + chunkPosition.z * Data.chunkWidth))))
                {
                    if(!world.IsBlockTransparent(world.GetBlockID(new Vector3(x + chunkPosition.x * Data.chunkWidth, 
                           y, 
                           z + chunkPosition.z * Data.chunkWidth))))
                    {
                        for (int v = 0; v < 6; v++)
                        {
                            vertices.Add(Data.vertices[Data.triangles[f, v]] + new Vector3(x, y, z));
                            triangles.Add(triangleIndex);
                            triangleIndex++;
                        }

                        AddUV(textures[f]);
                    }
                }
            }
        }
    }

    
    //add uv to a face
    private void AddUV(byte textureID)
    {
        float y = (int)(textureID / Data.textureAtlasSize);
        float x = (int)(textureID - (y * Data.textureAtlasSize));
        float textureSize = 1f / Data.textureAtlasSize;
        
        y /= Data.textureAtlasSize;
        x /= Data.textureAtlasSize;
        y = 1 - y;
        
        uv.Add(new Vector2(x, y - textureSize));
        uv.Add(new Vector2(x, y));
        uv.Add(new Vector2(x + textureSize, y - textureSize));
        uv.Add(new Vector2(x + textureSize, y - textureSize));
        uv.Add(new Vector2(x, y));
        uv.Add(new Vector2(x + textureSize, y));
    }
    
    //creates the final mesh
    public void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        meshRenderer.material = material;
        collider.sharedMesh = mesh;
    }

    public void Edit(byte x, byte y, byte z, byte blockID)
    {
        blocks[x, y, z] = blockID;
    }

    public void Update()
    {
        vertices.Clear();
        triangles.Clear();
        uv.Clear();
        triangleIndex = 0;
        AddBlocks();
        CreateMesh();
    }
}
