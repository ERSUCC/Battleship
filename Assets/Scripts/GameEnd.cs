using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameEnd : MonoBehaviour
{
    void Start()
    {
        int win = PlayerPrefs.GetInt("win");

        if (win > 0)
        {
            GameObject.Find("Title").GetComponent<Text>().text = "You Win!";
            GameObject.Find("Subtitle").GetComponent<Text>().text = "You sunk all of your opponent's ships!";
        }

        else
        {
            GameObject.Find("Title").GetComponent<Text>().text = "You Lose!";
            GameObject.Find("Subtitle").GetComponent<Text>().text = "Your opponent sunk all of your ships!";
        }
    }

    public void Menu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Start");
    }
}
