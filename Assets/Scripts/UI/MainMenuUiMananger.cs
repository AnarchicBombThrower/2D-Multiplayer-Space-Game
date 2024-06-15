using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUiMananger : MonoBehaviour
{
    [SerializeField]
    private GameObject mainMenuParent;
    [SerializeField]
    private InputField inputCodeToJoin;

    private void Start()
    {
        ConnectionManager.instance.subscribeToGameEnteredCallback(closeMenu);
    }

    public void hostGame()
    {
        ConnectionManager.instance.hostGame();
    }

    public void joinGame()
    {
        inputCodeToJoin.gameObject.SetActive(true);
    }

    public void inputFieldFilled()
    {
        ConnectionManager.instance.joinGame(inputCodeToJoin.text);
    }

    public void closeMenu()
    {
        mainMenuParent.SetActive(false);
    }

    public void quitGame()
    {
        Application.Quit();
    }
}
