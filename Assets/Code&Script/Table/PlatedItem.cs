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

    // If the food is destroyed (consumed, chopped, etc.) the plate goes with it.
    private void OnDestroy() => RemovePlate();


}
