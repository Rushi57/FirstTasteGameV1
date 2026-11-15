using UnityEngine;
using UnityEngine.UI;

public class PlatedItem : MonoBehaviour
{
    public GameObject plate;

    public void RemovePlate()
    {
        if (plate != null) Destroy(plate);
        plate = null;
    }
    private void OnDestroy() => RemovePlate();


}
