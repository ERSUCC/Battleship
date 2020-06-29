using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    void Start()
    {
        if (PlayerPrefs.GetInt("host") == 1)
        {
            Instantiate(Resources.Load("Server"));
        }
    }
}
