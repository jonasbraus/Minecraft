using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Server
{
    //game
    private World world;

    //network
    private UdpClient client;
    private int transferDelay;
    private List<IPEndPoint> players = new List<IPEndPoint>();
    private List<string> playerNames = new List<string>();
    private List<bool> nextTransfer = new List<bool>();
    private Thread receiveThread;
    private List<Thread> transferThreads = new List<Thread>();

    public Server(World world, int transferDelay)
    {
        this.transferDelay = transferDelay;
        this.world = world;

        //setup client
        client = new UdpClient(8051);
        client.DontFragment = false;

        //start listening
        receiveThread = new Thread(Receive); //info:
        receiveThread.Start();
        //0: init message
    }

    //info:
    //[0] = 0: init message
    //[0] = 1: world transfer [1] = 1: finished
    //[0] = 2: world size
    //[0] = 3: edit block
    //[0] = 4: disconnect player
    //[0] = 5: next transfer

    private void Receive()
    {
        try
        {
            while (true)
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref endPoint);

                if (data[0] == 5)
                {
                    int id = GetPlayerID(endPoint);
                    nextTransfer[id] = true;
                }

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
                        players.Remove(tempEndPoint);
                        Console.WriteLine(playerNames[i] + " left the game");
                        playerNames.RemoveAt(i);
                        nextTransfer.RemoveAt(i);
                    }

                }

                if (data[0] == 0)
                {
                    Thread t = new Thread(TransferWorld);
                    transferThreads.Add(t);
                    t.Start(endPoint);
                }

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
                    nextTransfer.Add(false);
                    Console.WriteLine(name + " joined the game");
                }

                if (data[0] == 3)
                {
                    ChunkCoord chunk = new ChunkCoord(data[1], data[2]);
                    Vector3 positionInChunk = new Vector3(data[3], data[4], data[5]);
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
        }
        catch (Exception e)
        {
            
        }
    }

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

    private void TransferWorld(object o)
    {
        IPEndPoint endPoint = (IPEndPoint)o;
        int id = GetPlayerID(endPoint);

        Console.WriteLine("begin world transfer for " + playerNames[id]);
        for (int xChunk = 0; xChunk < world.worldSize; xChunk++)
        {
            for (int zChunk = 0; zChunk < world.worldSize; zChunk++)
            {
                nextTransfer[id] = false;
                
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
                Send(send, endPoint);

                while (!nextTransfer[id])
                {
                }

                nextTransfer[id] = false;
            }
        }

        Send(new byte[] { 1, 1 }, endPoint);
        Console.WriteLine("world transfer finished for " + playerNames[id]);
    }

    private void Send(byte[] data, IPEndPoint endPoint)
    {
        client.Send(data, data.Length, endPoint);
    }

    public void Disconnect()
    {
        receiveThread.Abort();
        for (int i = 0; i < transferThreads.Count; i++)
        {
            if (transferThreads[i] != null)
            {
                transferThreads[i].Abort();
            }
        }
        client.Close();
    }
}