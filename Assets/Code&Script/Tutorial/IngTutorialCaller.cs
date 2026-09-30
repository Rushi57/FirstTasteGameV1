using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class IngInteractable : MonoBehaviour
{
    public List<Button> buttons;   // drag all your buttons here

    void Awake()
    {
        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            var interactable = btn.GetComponent<TutorialInteractable>();
            if (interactable == null) continue;   // no interactable = not reported

            btn.onClick.AddListener(interactable.ReportTap);
        }
    }
}