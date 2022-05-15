using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Client
{
    //game
    private World world;
    private string name;
    
    //network
    private UdpClient client;
    private int port;
    private Thread receiveThread;

    public Client(string hostname, int port, World world, string name)
    {
        this.port = port;
        this.world = world;
        this.name = name;

        //setup client
        client = new UdpClient();
        client.DontFragment = false;

        if (IPAddress.TryParse(hostname, out IPAddress address))
        {
            address = IPAddress.Parse(hostname);
            client.Connect(address, port);
        }
        else
        {
            client.Connect(hostname, port);
        }

        //get world size

        List<byte> sendList = new List<byte>();
        sendList.Add(2);

        byte[] asciiName = Encoding.ASCII.GetBytes(name);
        foreach (byte b in asciiName)
        {
            sendList.Add(b);
        }
        
        Send(sendList.ToArray());
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 8051);
        byte[] data = client.Receive(ref endPoint);
        if (data[0] == 2)
        {
            world.worldSize = data[1];
            world.CreateChunkArray();
        }
        
        //send init message
        Send(new byte[]{0});

        byte xChunk = 0;
        byte zChunk = 0;
        
        
        TcpClient tcpClient = new TcpClient();
        if (address != null)
        {
            address = IPAddress.Parse(hostname);
            tcpClient.Connect(address, port);
        }
        else
        {
            tcpClient.Connect(hostname, port);
        }

        StreamReader reader = new StreamReader(tcpClient.GetStream());
        
        while (true)
        {
            data = Encoding.ASCII.GetBytes(reader.ReadLine());

            if (data[0] == 1)
            {
                if (data[1] == 1)
                {
                    break;
                }
                
                //in world transfer
                byte[,,] blocks = new byte[Data.chunkWidth, Data.chunkHeight, Data.chunkWidth];
                int i = 2;
                
                for (int y = 0; y < Data.chunkHeight; y++)
                {
                    for (int x = 0; x < Data.chunkWidth; x++)
                    {
                        for (int z = 0; z < Data.chunkWidth; z++)
                        {
                            blocks[x, y, z] = data[i];
                            i++;
                        }
                    }
                }
                
                world.InitChunk(xChunk, zChunk, blocks);
                
                zChunk++;
                xChunk += (byte)(zChunk / world.worldSize);

                if (zChunk / world.worldSize == 1)
                {
                    zChunk = 0;
                }
            }
        }
        
        reader.Close();
        tcpClient.Close();

        world.CreateChunks();
        //world transfer finished
        
        //start listening
        receiveThread = new Thread(Receive);
        receiveThread.Start();
        
        Send(new byte[]{6});
    }

    //1.12 == [1] = 1, [2] = 12
    public void SendPositionUpdate(Vector3 position)
    {
        string x = position.x.ToString("n4");
        string y = position.y.ToString("n4");
        string z = position.z.ToString("n4");

        string sendPosition = x + " " + y + " " + z;
        byte[] sendPositionASCII = Encoding.ASCII.GetBytes(sendPosition);
        byte[] send = new byte[sendPositionASCII.Length + 1];
        send[0] = 7;
        for (int i = 1; i < send.Length; i++)
        {
            send[i] = sendPositionASCII[i - 1];
        }

        Send(send);
    }

    public void SendRotationUpdate(Quaternion rotation)
    {
        string x = rotation.x.ToString("n4");
        string y = rotation.y.ToString("n4");
        string z = rotation.z.ToString("n4");
        string w = rotation.w.ToString("n4");

        string sendRotation = x + " " + y + " " + z + " " + w;
        byte[] sendRotationASCII = Encoding.ASCII.GetBytes(sendRotation);
        byte[] send = new byte[sendRotationASCII.Length + 1];
        send[0] = 8;
        for (int i = 1; i < send.Length; i++)
        {
            send[i] = sendRotationASCII[i - 1];
        }
        
        Send(send);
    }
    
    private void Receive()
    {
        while(true)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 8051);
            byte[] data = client.Receive(ref endPoint);

            if (data[0] == 3)
            {
                ChunkCoord chunk = new ChunkCoord(data[1], data[2]);
                Vector3 positionInChunk = new Vector3(data[3], data[4], data[5]);
                byte blockID = data[6];
                
                world.EditBlock(chunk, positionInChunk, blockID);
            }

            //add a new player
            if (data[0] == 5)
            {
                world.AddPlayer(data[1]);
            }

            //update a players position
            if (data[0] == 7)
            {
                byte[] positionASCII = new byte[data.Length - 2];
                for (int i = 0; i < data.Length - 2; i++)
                {
                    positionASCII[i] = data[i + 2];
                }

                byte id = data[1];
                
                string[] positionString = Encoding.ASCII.GetString(positionASCII).Split(' ');
                float x = float.Parse(positionString[0]);
                float y = float.Parse(positionString[1]);
                float z = float.Parse(positionString[2]);

                world.UpdatePlayerPosition(new Vector3(x, y, z), id);
            }

            //remove a player from local world
            if (data[0] == 4)
            {
                world.RemovePlayer(data[1]);
            }

            //update a players rotation
            if (data[0] == 8)
            {
                byte[] rotationASCII = new byte[data.Length - 2];
                for (int i = 0; i < data.Length - 2; i++)
                {
                    rotationASCII[i] = data[i + 2];
                }

                byte id = data[1];
                
                string[] rotationString = Encoding.ASCII.GetString(rotationASCII).Split(' ');
                float x = float.Parse(rotationString[0]);
                float y = float.Parse(rotationString[1]);
                float z = float.Parse(rotationString[2]);
                float w = float.Parse(rotationString[3]);
                
                world.UpdatePlayerRotation(new Quaternion(x, y, z, w), id);
            }
        }
        
    }

    //sends an edit block request to server
    public void EditBlock(Vector3 positionInWorld, byte blockID)
    {
        ChunkCoord chunk = new ChunkCoord((int)(positionInWorld.x / Data.chunkWidth),
            (int)(positionInWorld.z / Data.chunkWidth));
        
        byte xInChunk = (byte)(positionInWorld.x - chunk.x * Data.chunkWidth);
        byte yInChunk = (byte)positionInWorld.y;
        byte zInChunk = (byte)(positionInWorld.z - chunk.z * Data.chunkWidth);
        
        //send Chunk with position in Chunk
        Send(new byte[] { 3, (byte)chunk.x, (byte)chunk.z, xInChunk, yInChunk, zInChunk, blockID });
    }
    
    //default send method
    private void Send(byte[] data)
    {
        client.Send(data, data.Length);
    }
    

    public void Disconnect()
    {
        Send(new byte[] { 4 });
        receiveThread.Abort();
        client.Close();
    }
}
