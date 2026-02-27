using System;
using UnityEngine;

public class UIGridGenerator : MonoBehaviour
{
    public GameObject cellPrefab;
    public static int Rows = 10;
    public NetworkCommunicationHandler NetworkCommunication;
    public String type;

    private void Start()
    {
        GenerateUIFields();
        
    }

    private void GenerateUIFields()
    {
        for (int i = -5; i < 5; i++)
        {
            for (int j = -5; j < 5; j++)
            {
                var newCell = Instantiate(cellPrefab, transform);
                newCell.name = $"Cell-{i}-{j}";
                var cellScript = newCell.GetComponent<UICell>();
                cellScript.Setup(i + 5, j + 5, NetworkCommunication);
                newCell.tag = type;
                newCell.transform.localPosition = new Vector3(0.075f + i * 0.15f, 0.015f, 0.075f + j * 0.15f);
            }
        }
    }
}