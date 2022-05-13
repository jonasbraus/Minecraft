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
    //[0] = 7: 

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
                        players.Remove(tempEndPoint);
                        Console.WriteLine(playerNames[i] + " left the game" + "\n");
                        playerNames.RemoveAt(i);
                 
                    }
                }

                if (data[0] == 0)
                {
                    Thread t = new Thread(TransferWorld);
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

                if (data[0] == 6)
                {
                    int id = GetPlayerID(endPoint);
                    Console.WriteLine(playerNames[id] + " joined" + "\n");

                    foreach (IPEndPoint e in players)
                    {
                        if (id != GetPlayerID(e))
                        {
                            Send(new byte[]{5, (byte)GetPlayerID(e)}, endPoint);
                            Send(new byte[]{5, (byte)GetPlayerID(endPoint)}, e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                
            }
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