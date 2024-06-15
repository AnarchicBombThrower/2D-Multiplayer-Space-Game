using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClientIssuesUiManager : MonoBehaviour
{
    [SerializeField]
    private Image parent;
    [SerializeField]
    private Text messageText;
    const string LOCAL_CLIENT_DISCONNECTED = "We have disconnected (or been disconnected)";
    const string CLIENT_DISCONNECTED = "Player has disconneceted";

    public void clientDisconnected(bool localClient)
    {
        if (localClient) 
        { messageText.text = LOCAL_CLIENT_DISCONNECTED; } 
        else { messageText.text = CLIENT_DISCONNECTED; }
        parent.gameObject.SetActive(true);
    }

    public void clientDisconnectedClose()
    {
        parent.gameObject.SetActive(false);
    }
}
