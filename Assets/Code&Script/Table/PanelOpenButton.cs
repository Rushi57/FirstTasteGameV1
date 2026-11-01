using UnityEngine;

public class PanelOpenButton : MonoBehaviour
{
    [SerializeField] private GameObject targetPanel;   // MainFridge / TopUtensils / CabinetMain

    public void Open() => PanelExclusivityManager.Instance.TryOpen(targetPanel);
    public void Close() => PanelExclusivityManager.Instance.Close(targetPanel);
}