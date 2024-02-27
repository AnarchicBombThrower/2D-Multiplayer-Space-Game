using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TemporaryNetworkUiManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Host()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void Client()
    {
        NetworkManager.Singleton.StartClient();
    }

    public void Ready()
    {
        PlayersManager.instance.localClientReadyUp();
    }
}
