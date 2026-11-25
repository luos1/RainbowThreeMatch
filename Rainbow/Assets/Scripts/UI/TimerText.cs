using UnityEngine;
using TMPro;

public class TimerText : MonoBehaviour
{
    string strTimer = "Time : ";
    TextMeshProUGUI text;

    private void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        Game.instance.ShowTimer += ShowTimer;
        ShowTimer(Game.instance.TimeSeconds);
    }

    public void ShowTimer(int time)
    {
        //Debug.Log($"Show Timer : {time}");
        text.text = $"{strTimer}{time}";
    }
}
