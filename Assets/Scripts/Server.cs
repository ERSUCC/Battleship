using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class Server : MonoBehaviour
{
    Socket listener;
    Socket player1;
    Socket player2;

    List<string> dataQueue = new List<string>();

    List<int> dataPlayerQueue = new List<int>();

    bool connected = false;
    bool turnDecided = false;

    void Start()
    {
        IPAddress ip = IPAddress.Parse(PlayerPrefs.GetString("hostIP"));
        IPEndPoint localEndPoint = new IPEndPoint(ip, 6969);

        listener = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        listener.Bind(localEndPoint);
        listener.Listen(2);

        InvokeRepeating("Listen", 0, 0.1f);
    }

    void Update()
    {
        if (dataQueue.Count > 0)
        {
            HandleData(dataPlayerQueue[0], dataQueue[0]);
        }
    }

    void Listen()
    {
        if (player1 != null && player2 != null)
        {
            if (!connected)
            {
                connected = true;
            }

            if (player1.Connected && player2.Connected)
            {
                Receive(player1);
                Receive(player2);
            }
        }

        else if (!connected)
        {
            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
        }
    }

    void HandleData(int player, string data)
    {
        if (data == "Ready<EOF>")
        {
            Send(player == 1 ? player2 : player1, "Opponent Ready<EOF>");
        }

        else if (data == "Decide Turn<EOF>" && !turnDecided)
        {
            bool turn = UnityEngine.Random.Range(-1, 1) < 0;

            Send(turn ? player1 : player2, "Turn<EOF>");

            turnDecided = true;
        }

        else if (data == "Win<EOF>")
        {
            Invoke("CloseSocket", 3);

            Send(player == 1 ? player2 : player1, data);
        }

        else
        {
            Send(player == 1 ? player2 : player1, data);
        }

        dataQueue.RemoveAt(0);
        dataPlayerQueue.RemoveAt(0);
    }

    void CloseSocket()
    {
        listener.Shutdown(SocketShutdown.Both);
        listener.Close();
    }

    void AcceptCallback(IAsyncResult result)
    {
        Socket listener = (Socket)result.AsyncState;
        Socket handler = listener.EndAccept(result);

        if (player1 == null)
        {
            player1 = handler;
        }

        else
        {
            player2 = handler;

            Send(player1, "Opponent Connected<EOF>");
            Send(player2, "Opponent Connected<EOF>");
        }
    }

    void Receive(Socket client)
    {
        StateObject state = new StateObject();

        state.socket = client;

        client.BeginReceive(state.buffer, 0, StateObject.bufferSize, 0, new AsyncCallback(ReadCallback), state);
    }

    void ReadCallback(IAsyncResult result)
    {
        StateObject state = (StateObject)result.AsyncState;
        Socket handler = state.socket;

        int bytesRead = handler.EndReceive(result);

        if (bytesRead > 0)
        {
            state.data += Encoding.ASCII.GetString(state.buffer, 0, bytesRead);

            if (state.data.IndexOf("<EOF>") > -1)
            {
                int player = handler == player1 ? 1 : 2;

                dataQueue.Add(state.data);
                dataPlayerQueue.Add(player);
            }

            else
            {
                handler.BeginReceive(state.buffer, 0, StateObject.bufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
        }
    }

    void Send(Socket handler, string data)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(data);

        handler.BeginSend(bytes, 0, bytes.Length, 0, new AsyncCallback(SendCallback), handler);
    }

    void SendCallback(IAsyncResult result)
    {
        ((Socket)result.AsyncState).EndSend(result);
    }
}
