using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Server
{
    //game
    private World world;
    private List<Vector3> playerPositions = new List<Vector3>();

    //network
    private UdpClient client;
    private int transferDelay;
    private List<IPEndPoint> players = new List<IPEndPoint>();
    private List<string> playerNames = new List<string>();
    private Thread receiveThread;

    public Server(World world, int transferDelay)
    {
        this.transferDelay = transferDelay;
        this.world = world;

        //setup client
        client = new UdpClient(8051);
        client.DontFragment = false;

        //start listening
        receiveThread = new Thread(Receive);
        receiveThread.Start();
        Console.WriteLine("server running on port 8051" + "\n");
    }

    //info:
    //[0] = 0: init message
    //[0] = 1: world transfer [1] = 1: finished, to client
    //[0] = 2: world size
    //[0] = 3: edit block
    //[0] = 4: disconnect player
    //[0] = 5: create new player in client
    //[0] = 6: player ready
    //[0] = 7: player position update
    //[0] = 8: player rotation update
    //[0] = 9: Save the World

    private void Receive()
    {
        while (true)
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 8051);
                byte[] data = client.Receive(ref endPoint);

                if (data[0] == 4)
                {
                    IPEndPoint tempEndPoint = null;
                    int i = 0;
                    foreach (IPEndPoint e in players)
                    {
                        if (e.Address.GetHashCode() == endPoint.Address.GetHashCode())
                        {
                            Send(new byte[] { 4 }, e);
                            tempEndPoint = e;
                            break;
                        }

                        i++;
                    }

                    if (tempEndPoint != null)
                    {
                        foreach (IPEndPoint e in players)
                        {
                            if (i != GetPlayerID(e))
                            {
                                Send(new byte[]{4, (byte)i}, e);
                            }
                        }
                        
                        players.Remove(tempEndPoint);
                        Console.WriteLine(playerNames[i] + " left the game" + "\n");
                        playerNames.RemoveAt(i);
                        playerPositions.RemoveAt(i);
                    }
                }

                //init message / world transfer
                if (data[0] == 0)
                {
                    Thread t = new Thread(TransferWorld);
                    t.Start(endPoint);
                }

                //get the world size (first message from client)
                if (data[0] == 2)
                {
                    Send(new byte[] { 2, (byte)world.worldSize }, endPoint);

                    List<byte> asciiName = new List<byte>();
                    for (int i = 1; i < data.Length; i++)
                    {
                        asciiName.Add(data[i]);
                    }

                    string name = Encoding.ASCII.GetString(asciiName.ToArray());
                    //add player
                    players.Add(endPoint);
                    playerNames.Add(name);
                    playerPositions.Add(new Vector3(0, 0, 0));
                }

                //edit a block in world
                if (data[0] == 3)
                {
                    ChunkCoord chunk = new ChunkCoord(data[1], data[2]);
                    Vector3 positionInChunk = new Vector3(data[3], data[4], data[5]);
                    Vector3 positionInWorld = new Vector3(chunk.x * Data.chunkWidth + positionInChunk.x,
                        positionInChunk.y, chunk.z * Data.chunkWidth + positionInChunk.z);

                    //check that there is no player on the block position
                    bool found = false;
                    foreach (Vector3 v in playerPositions)
                    {
                        if (((int)v.x == (int)positionInWorld.x && (int)v.z == (int)positionInWorld.z) &&
                            ((int)v.y == (int)positionInWorld.y || (int)v.y + 1 == (int)positionInWorld.y))
                        {
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        byte blockID = data[6];
                    
                        world.EditBlock(chunk, positionInChunk, blockID);

                        foreach (IPEndPoint e in players)
                        {
                            Send(new byte[]
                            {
                                3, (byte)chunk.x, (byte)chunk.z,
                                (byte)positionInChunk.x, (byte)positionInChunk.y, (byte)positionInChunk.z, blockID
                            }, e);
                        }
                    }
                }

                //a player successfully joined the game...
                if (data[0] == 6)
                {
                    int id = GetPlayerID(endPoint);
                    Console.WriteLine(playerNames[id] + " joined" + "\n");

                    foreach (IPEndPoint e in players)
                    {
                        if (id != GetPlayerID(e))
                        {
                            //create player objects of that player in every connected client
                            Send(new byte[]{5, (byte)GetPlayerID(e)}, endPoint);
                            //create player objects of all connected clients in the currently connected client
                            Send(new byte[]{5, (byte)GetPlayerID(endPoint)}, e);
                        }
                    }
                }

                //update a players position on the server
                if (data[0] == 7)
                {
                    byte[] positionASCII = new byte[data.Length - 1];
                    for (int i = 0; i < data.Length - 1; i++)
                    {
                        positionASCII[i] = data[i + 1];
                    }

                    string[] positionString = Encoding.ASCII.GetString(positionASCII).Split(' ');
                    float x = float.Parse(positionString[0]);
                    float y = float.Parse(positionString[1]);
                    float z = float.Parse(positionString[2]);

                    int id = GetPlayerID(endPoint);

                    playerPositions[id] = new Vector3(x, y, z);

                    string sendPosition = x + " " + y + " " + z;
                    byte[] sendPositionASCII = Encoding.ASCII.GetBytes(sendPosition);
                    byte[] send = new byte[sendPositionASCII.Length + 2];
                    send[0] = 7;
                    send[1] = (byte)id;
                    for (int i = 2; i < send.Length; i++)
                    {
                        send[i] = sendPositionASCII[i - 2];
                    }

                    //send update to every other client
                    foreach (IPEndPoint e in players)
                    {
                        if (id != GetPlayerID(e))
                        {
                            Send(send, e);
                        }
                    }
                }

                //update a players rotation on server
                if (data[0] == 8)
                {
                    byte[] rotationASCII = new byte[data.Length - 1];
                    for (int i = 0; i < data.Length - 1; i++)
                    {
                        rotationASCII[i] = data[i + 1];
                    }
                    
                    string[] rotationString = Encoding.ASCII.GetString(rotationASCII).Split(' ');
                    string x = rotationString[0];
                    string y = rotationString[1];
                    string z = rotationString[2];
                    string w = rotationString[3];

                    byte id = (byte)GetPlayerID(endPoint);
                    string sendRotation = x + " " + y + " " + z + " " + w;
                    byte[] sendRotationASCII = Encoding.ASCII.GetBytes(sendRotation);
                    byte[] send = new byte[sendRotationASCII.Length + 2];
                    send[0] = 8;
                    send[1] = id;
                    
                    for (int i = 2; i < send.Length; i++)
                    {
                        send[i] = sendRotationASCII[i - 2];
                    }

                    //send update to every other client
                    foreach (IPEndPoint e in players)
                    {
                        if (id != GetPlayerID(e))
                        {
                            Send(send, e);
                        }
                    }
                }
                
                //Save the World
                if (data[0] == 9)
                {
                    world.SaveWorld();
                }
            }
            catch (Exception e)
            {
                
            }
        }
    }

    //returns the id of an player
    private int GetPlayerID(IPEndPoint endPoint)
    {
        int i = 0;
        foreach (IPEndPoint e in players)
        {
            if (e.Address.GetHashCode() == endPoint.Address.GetHashCode())
            {
                return i;
            }

            i++;
        }

        return -1;
    }

    //starts a tcp client for world transfer
    private void TransferWorld(object o)
    {
        try
        {
            IPEndPoint endPoint = (IPEndPoint)o;
            int id = GetPlayerID(endPoint);

            TcpListener listener = new TcpListener(IPAddress.Any, 8051);
            listener.Start();
            TcpClient tcpClient = listener.AcceptTcpClient();
            listener.Stop();
            // Console.WriteLine("tcp connected for " + playerNames[id] + "\n");
            StreamWriter writer = new StreamWriter(tcpClient.GetStream());
            writer.AutoFlush = true;

            Console.WriteLine("begin world transfer for " + playerNames[id] + "\n");
            for (int xChunk = 0; xChunk < world.worldSize; xChunk++)
            {
                for (int zChunk = 0; zChunk < world.worldSize; zChunk++)
                {
                    byte[] send = new byte[Data.chunkWidth * Data.chunkWidth * Data.chunkHeight + 2];
                    send[0] = 1;
                    send[1] = 0;
                    int i = 2;

                    for (int y = 0; y < Data.chunkHeight; y++)
                    {
                        for (int x = 0; x < Data.chunkWidth; x++)
                        {
                            for (int z = 0; z < Data.chunkWidth; z++)
                            {
                                send[i] = world.chunks[xChunk, zChunk].blocks[x, y, z];
                                i++;
                            }
                        }
                    }
                    writer.WriteLine(Encoding.ASCII.GetString(send));
                }
            }

            writer.WriteLine(Encoding.ASCII.GetString(new byte[] { 1, 1 }));
            Console.WriteLine("world transfer finished for " + playerNames[id] + "\n");
            writer.Close();
            tcpClient.Close();
        }
        catch (Exception e)
        {
            
        }
    }

    //default send function
    private void Send(byte[] data, IPEndPoint endPoint)
    {
        client.Send(data, data.Length, endPoint);
    }

    public void Disconnect()
    {
        receiveThread.Abort();

        client.Close();
    }
}