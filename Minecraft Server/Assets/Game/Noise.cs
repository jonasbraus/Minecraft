using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noise
{
    public byte GetHeight(int x, int z, int randomOffsetX, int randomOffsetZ)
    {
        float scale = 0.025f;
        byte perlinHeight = 10;
        byte groundHeight = 50;

        byte height = (byte)(Mathf.PerlinNoise((x + randomOffsetX) * scale + 0.1f, (z + randomOffsetZ) * scale + 0.1f) * perlinHeight + groundHeight);
        return height;
    }

    public bool CheckTree(int x, int z)
    {
        float scale = .99f;
        float threshold = .96f;
        
        if (Mathf.PerlinNoise((x) * scale + 0.1f, (z) * scale + 0.1f) >= threshold)
        {
            return true;
        }
        
        return false;
    }
    
    public bool Get3DPerlin(Vector3 position, float offset, float scale, float threshold)
    {
        float x = (position.x + offset + .1f) * scale;
        float y = (position.y + offset + .1f) * scale;
        float z = (position.z + offset + .1f) * scale;

        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);
        
        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        if ((AB + BC + AC + BA + CB + CA) / 6f > threshold)
        {
            return true;
        }

        return false;
    }
}
