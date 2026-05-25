using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStarter : MonoBehaviour
{
    [SerializeField] private UnitPlacer unitPlacer;
    [SerializeField] private GameObject preGameUI;

    
    // Start is called before the first frame update
    public void StartGame()
    {
        if(unitPlacer != null)
            if (unitPlacer.placedUnitsCount == 0) {
                Debug.LogWarning(
                    "No units placed! Starting the game without any units may lead to unexpected behaviour.");
                return;
            }
        
        GameManager.Instance.StartGame();
        preGameUI.SetActive(false);

    }


}
