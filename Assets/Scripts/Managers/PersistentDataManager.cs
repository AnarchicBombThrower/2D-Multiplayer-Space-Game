using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PersistentDataManager : MonoBehaviour
{
    //PC
    const string PC_APPLICATION_PATH = "\\config.txt";

    void Start()
    {
        string[] fileLines = File.ReadAllLines(Application.streamingAssetsPath + PC_APPLICATION_PATH);  
        
        foreach (string fileLine in fileLines)
        {
            string[] splitString = fileLine.Split('=');
            string attribute = splitString[0];
            string value = splitString[1];

            switch (attribute)
            {

            }
        }
    }

    private void OnDestroy()
    {
        string textToWrite = "";
        File.WriteAllText(Application.streamingAssetsPath + PC_APPLICATION_PATH, textToWrite);
    }

    private string writeAttributesAs(string attribute, string value)
    {
        return attribute + "=" + value;
    }
}
