using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelStats : MonoBehaviour {

    public int startMoney = 022;
    public int moneyAmt;
    [SerializeField] private TextMeshProUGUI moneyText;
    
    // Start is called before the first frame update
    void Start()
    { 
        SetText(moneyText, moneyAmt.ToString());
    }
    
    private void SetText(TextMeshProUGUI text, string value) {
        text.text = value;
    }
    
    public void UpdateMoney(int amount) {
        moneyAmt = amount;
        SetText(moneyText, moneyAmt.ToString());
    }


}
