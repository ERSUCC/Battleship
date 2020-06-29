using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartController : MonoBehaviour
{
    GameObject ipCover;
    GameObject ipPlaceholder;
    GameObject ipText;

    Color whiteClear = new Color(1, 1, 1, 0);

    int host;
    float revealAmount = 0;
    bool reveal = false;

    void Start()
    {
        ipCover = GameObject.Find("IP Cover");
        ipPlaceholder = GameObject.Find("IP Placeholder");
        ipText = GameObject.Find("IP Text");
    }

    void Update()
    {
        ipCover.GetComponent<Image>().color = Color.Lerp(Color.white, whiteClear, revealAmount);

        if (reveal && ipCover.GetComponent<Image>().color != whiteClear)
        {
            revealAmount += 0.02f;
        }

        else if (!reveal && ipCover.GetComponent<Image>().color != Color.white)
        {
            revealAmount -= 0.02f;
        }
    }

    public void Host()
    {
        host = 1;

        string hostIP = "";

        var hostDns = Dns.GetHostEntry(Dns.GetHostName());

        foreach (var ip in hostDns.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                hostIP = ip.ToString();

                break;
            }
        }

        PlayerPrefs.SetInt("host", host);
        PlayerPrefs.SetString("hostIP", hostIP);

        SceneManager.LoadScene("Main");
    }

    public void Join()
    {
        reveal = !reveal;

        ipCover.GetComponent<Image>().raycastTarget = !reveal;
        ipPlaceholder.GetComponent<Text>().text = "Enter host IP...";

        host = 0;
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void LoadMain()
    {
        if (CheckFormat(ipText.GetComponent<Text>().text))
        {
            PlayerPrefs.SetInt("host", host);
            PlayerPrefs.SetString("hostIP", ipText.GetComponent<Text>().text);

            SceneManager.LoadScene("Main");
        }

        else
        {
            ThrowError();
        }
    }

    bool CheckFormat(string ip)
    {
        string[] sections;

        try
        {
            sections = ip.Split('.');
        }

        catch
        {
            return false;
        }

        if (sections.Length != 4)
        {
            return false;
        }

        foreach (string section in sections)
        {
            try
            {
                if (int.Parse(section) < 0 && int.Parse(section) > 255)
                {
                    return false;
                }
            }

            catch
            {
                return false;
            }
        }

        return true;
    }

    void ThrowError()
    {
        ipText.GetComponentInParent<InputField>().text = "Invalid IP address.";
        ipText.GetComponent<Text>().color = Color.red;
        ipText.GetComponentInParent<InputField>().readOnly = true;

        Invoke("ResetError", 2);
    }

    void ResetError()
    {
        ipText.GetComponentInParent<InputField>().text = "";
        ipText.GetComponent<Text>().color = Color.black;
        ipText.GetComponentInParent<InputField>().readOnly = false;
    }
}
