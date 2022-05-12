using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Login : MonoBehaviour
{
    [SerializeField] private InputField userName;
    [SerializeField] private InputField serverIP;
    [SerializeField] private InputField port;

    private void Start()
    {
        if (PlayerPrefs.HasKey("userName"))
        {
            userName.text = PlayerPrefs.GetString("userName");
        }

        if (PlayerPrefs.HasKey("serverIP"))
        {
            serverIP.text = PlayerPrefs.GetString("serverIP");
        }

        if (PlayerPrefs.HasKey("port"))
        {
            port.text = PlayerPrefs.GetString("port");
        }
    }
    
    public void ButtonSend()
    {
        PlayerPrefs.SetString("userName", userName.text);
        PlayerPrefs.SetString("serverIP", serverIP.text);
        PlayerPrefs.SetString("port", port.text);
        PlayerPrefs.Save();

        SceneManager.LoadScene("Scenes/Game", LoadSceneMode.Single);
    }
}
