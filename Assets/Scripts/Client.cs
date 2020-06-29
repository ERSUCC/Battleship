using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Client : MonoBehaviour
{
    GameObject waiting1;
    GameObject waiting2;
    GameObject readyButton;
    GameObject board1;
    GameObject board2;
    GameObject networkGroup;
    GameObject turnIndicator;
    GameObject ships;
    GameObject opponentShips;
    GameObject boardText;
    GameObject boardButton;
    GameObject reconnectButton;

    AudioSource explosionAudioSource;
    AudioSource splashAudioSource;
    AudioSource clankAudioSource;

    Socket client;

    Dictionary<int, bool> shipsPlaced = new Dictionary<int, bool>()
    {
        { 1, false },
        { 2, false },
        { 3, false },
        { 4, false },
        { 5, false }
    };

    List<Dictionary<int[], bool>> shipsHit = new List<Dictionary<int[], bool>>()
    {
        new Dictionary<int[], bool>(),
        new Dictionary<int[], bool>(),
        new Dictionary<int[], bool>(),
        new Dictionary<int[], bool>(),
        new Dictionary<int[], bool>()
    };

    List<List<string>> shipsGuessed = new List<List<string>>()
    {
        new List<string>(),
        new List<string>(),
        new List<string>(),
        new List<string>(),
        new List<string>()
    };

    int[,] grid = new int[10, 10];

    string data = "";

    int phase = 0;
    int shipSelected = 0;

    bool connected = false;
    bool turn = false;
    bool ready = false;
    bool opponentReady = false;

    void Start()
    {
        waiting1 = GameObject.Find("Waiting");
        waiting2 = GameObject.Find("Waiting 2");
        readyButton = GameObject.Find("Ready Button");
        board1 = GameObject.Find("Board 1");
        board2 = GameObject.Find("Board 2");
        networkGroup = GameObject.Find("Network Status Group");
        turnIndicator = GameObject.Find("Turn Indicator");
        ships = GameObject.Find("Ships");
        opponentShips = GameObject.Find("Opponent Ships");
        reconnectButton = GameObject.Find("Reconnect Button");

        explosionAudioSource = GetComponents<AudioSource>()[0];
        splashAudioSource = GetComponents<AudioSource>()[1];
        clankAudioSource = GetComponents<AudioSource>()[2];

        boardText = (GameObject)Resources.Load("Board Text");
        boardButton = (GameObject)Resources.Load("Board Button");

        networkGroup.transform.GetChild(1).GetComponent<Text>().text = "Host IP: " + PlayerPrefs.GetString("hostIP");
        networkGroup.transform.GetChild(2).GetComponent<Text>().text = "Your IP: " + GetIP();
        networkGroup.transform.GetChild(3).gameObject.SetActive(false);

        readyButton.SetActive(false);
        turnIndicator.SetActive(false);
        ships.SetActive(false);
        opponentShips.SetActive(false);
        reconnectButton.SetActive(false);

        IPAddress ip = IPAddress.Parse(PlayerPrefs.GetString("hostIP"));
        IPEndPoint remoteEndPoint = new IPEndPoint(ip, 6969);

        client = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        client.BeginConnect(remoteEndPoint, new AsyncCallback(ConnectCallback), client);

        InvokeRepeating("Listen", 0, 0.1f);
    }

    void Update()
    {
        if (data.Length > 0)
        {
            HandleData(data);
        }
    }

    void Listen()
    {
        if (phase == 0)
        {
            if (opponentReady && ready)
            {
                phase = 1;

                Send(client, "Decide Turn<EOF>");

                CreateBoard(board1.transform);
                Destroy(waiting1);

                opponentShips.SetActive(true);
                turnIndicator.SetActive(true);

                foreach (Transform ship in ships.transform)
                {
                    ship.GetComponent<Button>().enabled = false;
                }

                foreach (Transform ship in opponentShips.transform)
                {
                    ship.GetComponent<Button>().enabled = false;
                }
            }
        }

        if (client.Connected)
        {
            if (!connected)
            {
                networkGroup.transform.GetChild(0).GetComponent<Text>().text = "Status: Connected";

                if (reconnectButton.activeInHierarchy)
                {
                    reconnectButton.SetActive(false);
                }

                connected = true;
            }

            Receive(client);
        }

        else
        {
            if (connected)
            {
                networkGroup.transform.GetChild(0).GetComponent<Text>().text = "Status: Connection lost";

                reconnectButton.SetActive(true);

                connected = false;
            }
        }
    }

    public void Ready()
    {
        if (IsReady())
        {
            Destroy(readyButton);

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (grid[i, j] != 0)
                    {
                        shipsHit[grid[i, j] - 1].Add(new int[] { i, j }, false);
                    }
                }
            }

            foreach (Transform ship in ships.transform)
            {
                if (ship.GetComponent<Image>() != null)
                {
                    ship.GetComponent<Image>().color = Color.red;
                }
            }

            ready = true;

            Send(client, "Ready<EOF>");
        }
    }

    public void Leave()
    {
        if (client.Connected)
        {
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        SceneManager.LoadScene("Start");
    }

    public void SelectShip(int ship)
    {
        if (shipSelected == ship)
        {
            ships.transform.GetChild(ship).GetComponent<Image>().color = Color.red;

            shipSelected = 0;
        }

        else
        {
            if (shipSelected != 0)
            {
                ships.transform.GetChild(shipSelected).GetComponent<Image>().color = Color.red;
            }

            ships.transform.GetChild(ship).GetComponent<Image>().color = new Color(0, 0.5f, 1);

            shipSelected = ship;
        }
    }

    public void Reconnect()
    {
        IPAddress ip = IPAddress.Parse(PlayerPrefs.GetString("hostIP"));
        IPEndPoint remoteEndPoint = new IPEndPoint(ip, 6969);

        client.BeginConnect(remoteEndPoint, new AsyncCallback(ConnectCallback), client);
    }

    void HandleData(string data)
    {
        if (data == "Opponent Connected<EOF>")
        {
            CreateBoard(board2.transform);
            Destroy(waiting2);

            waiting1.GetComponent<Text>().text = "Opponent is placing their ships";

            readyButton.SetActive(true);
            ships.SetActive(true);
        }

        else if (data == "Opponent Ready<EOF>")
        {
            waiting1.GetComponent<Text>().text = "Opponent is ready";

            opponentReady = true;
        }

        else if (data[0] == '~')
        {
            bool hit = false;

            int i = 0;
            int[] key = new int[] { -1, -1 };

            foreach (Dictionary<int[], bool> ship in shipsHit)
            {
                foreach (KeyValuePair<int[], bool> square in ship)
                {
                    if (square.Key[0] == int.Parse(data[1].ToString()) && square.Key[1] == int.Parse(data[2].ToString()))
                    {
                        key = square.Key;
                        hit = true;

                        break;
                    }
                }

                if (hit)
                {
                    break;
                }

                else
                {
                    i += 1;
                }
            }

            GameObject square2 = GameObject.Find(data.Substring(1, 2) + "0");

            square2.GetComponent<Button>().enabled = false;

            if (hit)
            {
                shipsHit[i][key] = true;

                bool win = true;

                foreach (Dictionary<int[], bool> dict in shipsHit)
                {
                    if (dict.ContainsValue(false))
                    {
                        win = false;
                    }
                }

                if (win)
                {
                    Send(client, "Win<EOF>");

                    PlayerPrefs.SetInt("win", 0);

                    SceneManager.LoadScene("Game End");
                }

                if (shipsHit[i].ContainsValue(false))
                {
                    //play clank audio

                    square2.GetComponent<Image>().color = Color.red;

                    Send(client, $"$H{i}{data.Substring(1, 2)}<EOF>");
                }

                else
                {
                    explosionAudioSource.Play();

                    square2.GetComponent<Image>().color = Color.white;
                    square2.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Skull and Crossbones");

                    foreach (int[] point in shipsHit[i].Keys)
                    {
                        GameObject square3 = GameObject.Find(point[0].ToString() + point[1].ToString() + "0");

                        square3.GetComponent<Image>().color = Color.white;
                        square3.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Skull and Crossbones");
                    }

                    ships.transform.GetChild(i + 1).GetComponent<Image>().color = Color.green;

                    Send(client, $"$S{i}{data.Substring(1, 2)}<EOF>");
                }
            }

            else
            {
                splashAudioSource.Play();

                square2.GetComponent<Image>().color = Color.white;
                square2.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Red X");

                Send(client, $"$M{i}{data.Substring(1, 2)}<EOF>");
            }
        }

        else if (data[0] == '$')
        {
            GameObject square = GameObject.Find(data.Substring(3, 2) + "1");

            square.GetComponent<Button>().enabled = false;

            if (data[1] == 'H')
            {
                //play clank audio

                square.GetComponent<Image>().color = Color.red;

                shipsGuessed[int.Parse(data[2].ToString())].Add(data.Substring(3, 2));
            }

            else if (data[1] == 'S')
            {
                explosionAudioSource.Play();

                square.GetComponent<Image>().color = Color.white;
                square.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Skull and Crossbones");

                foreach (string point in shipsGuessed[int.Parse(data[2].ToString())])
                {
                    GameObject square2 = GameObject.Find(point[0].ToString() + point[1].ToString() + "1");

                    square2.GetComponent<Image>().color = Color.white;
                    square2.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Skull and Crossbones");
                }

                opponentShips.transform.GetChild(int.Parse(data[2].ToString()) + 1).GetComponent<Image>().color = Color.green;
            }

            else
            {
                splashAudioSource.Play();

                square.GetComponent<Image>().color = new Color(100f / 255, 100f / 255, 100f / 255);
            }

            Send(client, "Turn<EOF>");

            turn = false;

            turnIndicator.GetComponent<RectTransform>().localPosition *= new Vector2(1, -1);
            turnIndicator.GetComponent<Text>().text = "<---- Opponent's turn";
        }

        else if (data == "Turn<EOF>")
        {
            turn = true;

            turnIndicator.GetComponent<RectTransform>().localPosition *= new Vector2(1, -1);
            turnIndicator.GetComponent<Text>().text = "<---- Your turn";
        }

        else if (data == "Win<EOF>")
        {
            PlayerPrefs.SetInt("win", 1);

            SceneManager.LoadScene("Game End");
        }

        this.data = "";
    }

    void CreateBoard(Transform parent)
    {
        GameObject corner = new GameObject("Corner");

        corner.AddComponent<RectTransform>();

        Instantiate(corner, parent);

        for (int i = 0; i < 10; i++)
        {
            Instantiate(boardText, parent).GetComponent<Text>().text = ((char)('A' + i)).ToString();
        }

        for (int i = 0; i < 10; i++)
        {
            Instantiate(boardText, parent).GetComponent<Text>().text = (i + 1).ToString();

            for (int j = 0; j < 10; j++)
            {
                GameObject button = Instantiate(boardButton, parent);

                button.name = i.ToString() + j.ToString() + (parent == board1.transform ? 1 : 0).ToString();

                AddListener(button.GetComponent<Button>(), i, j, parent == board1.transform ? 1 : 0);
            }
        }
    }

    void AddListener(Button button, int i, int j, int player)
    {
        button.onClick.AddListener(() => SquareClicked(i.ToString() + j.ToString() + player.ToString()));
    }

    void SquareClicked(string square)
    {
        if (phase == 0 && !ready && shipSelected > 0 && square[2] == '0')
        {
            CheckGrid(square);
        }

        else
        {
            if (turn && square[2] == '1')
            {
                Send(client, "~" + square.Substring(0, 2) + "<EOF>");
            }
        }
    }

    void CheckGrid(string square)
    {
        int y = int.Parse(square[0].ToString());
        int x = int.Parse(square[1].ToString());

        int count = 0;

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if (grid[i, j] == shipSelected)
                {
                    count += 1;
                }
            }
        }

        if (grid[y, x] == 0)
        {
            int up = y > 0 ? grid[y - 1, x] : 0;
            int right = x < 9 ? grid[y, x + 1] : 0;
            int down = y < 9 ? grid[y + 1, x] : 0;
            int left = x > 0 ? grid[y, x - 1] : 0;

            if (count == 0 || (count == 1 && (up == shipSelected || right == shipSelected || down == shipSelected || left == shipSelected)))
            {
                grid[y, x] = shipSelected;
            }

            else if (up == shipSelected)
            {
                if ((y > 1 && grid[y - 2, x] == shipSelected) || down == shipSelected)
                {
                    grid[y, x] = shipSelected;
                }
            }

            else if (right == shipSelected)
            {
                if ((x < 8 || grid[y, x + 2] == shipSelected) || left == shipSelected)
                {
                    grid[y, x] = shipSelected;
                }
            }

            else if (down == shipSelected)
            {
                if ((y < 8 && grid[y + 2, x] == shipSelected) || up == shipSelected)
                {
                    grid[y, x] = shipSelected;
                }
            }

            else if (left == shipSelected)
            {
                if ((x > 1 && grid[y, x - 2] == shipSelected) || right == shipSelected)
                {
                    grid[y, x] = shipSelected;
                }
            }
        }

        else if (grid[y, x] == shipSelected)
        {
            int up = y > 0 ? grid[y - 1, x] : 0;
            int right = x < 9 ? grid[y, x + 1] : 0;
            int down = y < 9 ? grid[y + 1, x] : 0;
            int left = x > 0 ? grid[y, x - 1] : 0;

            if (right != shipSelected && left != shipSelected && (up != shipSelected || down != shipSelected))
            {
                GameObject.Find(y.ToString() + x.ToString() + "0").GetComponent<Image>().color = new Color(200f / 255, 200f / 255, 200f / 255);

                grid[y, x] = 0;
            }

            else if (up != shipSelected && down != shipSelected && (right != shipSelected || left != shipSelected))
            {
                GameObject.Find(y.ToString() + x.ToString() + "0").GetComponent<Image>().color = new Color(200f / 255, 200f / 255, 200f / 255);

                grid[y, x] = 0;
            }
        }

        List<int[]> positions = new List<int[]>();

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if (grid[i, j] == shipSelected)
                {
                    positions.Add(new int[] { i, j });
                }
            }
        }

        int length = int.Parse(ships.transform.GetChild(shipSelected).name[0].ToString());

        if (positions.Count == length)
        {
            foreach (int[] pos in positions)
            {
                GameObject.Find(pos[0].ToString() + pos[1].ToString() + "0").GetComponent<Image>().color = new Color(0, 0.5f, 1);
            }

            shipsPlaced[shipSelected] = true;
        }

        else
        {
            foreach (int[] pos in positions)
            {
                GameObject.Find(pos[0].ToString() + pos[1].ToString() + "0").GetComponent<Image>().color = Color.red;
            }

            shipsPlaced[shipSelected] = false;
        }
    }

    string GetIP()
    {
        foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        return "No suitable IP found";
    }

    bool IsReady()
    {
        return !shipsPlaced.ContainsValue(false);
    }

    void ConnectCallback(IAsyncResult result)
    {
        Socket client = (Socket)result.AsyncState;

        client.EndConnect(result);
    }

    void Send(Socket client, string data)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(data);

        client.BeginSend(bytes, 0, bytes.Length, 0, new AsyncCallback(SendCallback), client);
    }

    void SendCallback(IAsyncResult result)
    {
        ((Socket)result.AsyncState).EndSend(result);
    }

    void Receive(Socket client)
    {
        StateObject state = new StateObject();

        state.socket = client;

        client.BeginReceive(state.buffer, 0, StateObject.bufferSize, 0, new AsyncCallback(ReceiveCallback), state);
    }

    void ReceiveCallback(IAsyncResult result)
    {
        StateObject state = (StateObject)result.AsyncState;
        Socket client = state.socket;

        int bytesRead = client.EndReceive(result);

        if (bytesRead > 0)
        {
            state.data += Encoding.ASCII.GetString(state.buffer, 0, bytesRead);

            if (state.data.IndexOf("<EOF>") > -1)
            {
                data = state.data;
            }

            else
            {
                client.BeginReceive(state.buffer, 0, StateObject.bufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
        }
    }
}
