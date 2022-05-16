using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Data
{
    public static int chunkWidth = 15, chunkHeight = 250;
    public static int textureAtlasSize = 16;
    public static int viewDistance = 9;
    
    public static readonly Vector3[] vertices =
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(0, 1, 1),
            new Vector3(1, 1, 1)
        };
    
        public static readonly int[,] triangles =
        {
            //front
            {0, 3, 1, 1, 3, 2},
            //back
            {4, 7, 5, 5, 7, 6},
            //top
            {3, 6, 2, 2, 6, 7},
            //bottom
            {5, 0, 4, 4, 0, 1},
            //left
            {5, 6, 0, 0, 6, 3},
            //right
            {1, 2, 4, 4, 2, 7},
        };
        
        public static readonly Vector3[] faceChecks =
            {   
                //front
                new Vector3(0, 0, -1),
                //back
                new Vector3(0, 0, 1),
                //top
                new Vector3(0, 1, 0),
                //bottom
                new Vector3(0, -1, 0),
                //left
                new Vector3(-1, 0, 0),
                //right
                new Vector3(1, 0, 0),
            };
}
