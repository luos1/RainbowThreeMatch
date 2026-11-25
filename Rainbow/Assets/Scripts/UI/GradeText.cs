using UnityEngine;
using TMPro;

public class GradeText : MonoBehaviour
{
    string strGrade = "Grade : ";
    TextMeshProUGUI text;

    private void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        ShowGrade(Game.instance.Grade);
        Game.instance.ShowGrade += ShowGrade;
    }

    public void ShowGrade(int grade)
    {
        text.text = $"{strGrade}{grade+1}";
    }
}
