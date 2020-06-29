using System.Net.Sockets;

public class StateObject
{
    public Socket socket = null;
    public const int bufferSize = 1024;
    public byte[] buffer = new byte[bufferSize];
    public string data = "";
}
