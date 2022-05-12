using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChunkCoord
{
    public ChunkCoord(int x, int z)
    {
        this.x = x;
        this.z = z;
    }
    
    public int x;
    public int z;
}
