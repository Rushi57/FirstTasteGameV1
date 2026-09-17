using UnityEngine;

public class SpoonSpawner : MonoBehaviour
{
    [System.Serializable]
    public struct SpoonData
    {
        public string label;
        public float value;
    }

    [Header("References")]
    public GameObject containerPrefab; // Drag ContainerPrefab here
    public Transform gridParent;         // Drag TableSpoonPickerGameObject here

    [Header("Measurement Options")]
    public SpoonData[] standardSpoons = new SpoonData[]
    {
        new SpoonData { label = "1/4 tbsp", value = 0.25f },
        new SpoonData { label = "1/2 tbsp", value = 0.5f },
        new SpoonData { label = "1 tbsp",   value = 1.0f },
        new SpoonData { label = "2 tbsp",   value = 2.0f }
    };

    private void Awake()
    {
        GenerateSpoons();
    }

    public void GenerateSpoons()
    {
        // Clear any existing editor placeholders
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        // Spawn a slot container for each measurement option
        foreach (SpoonData data in standardSpoons)
        {
            GameObject slot = Instantiate(containerPrefab, gridParent);

            // Find the SpoonDraggable script located on the child TableSpoon object
            SpoonDraggable draggable = slot.GetComponentInChildren<SpoonDraggable>();

            if (draggable != null)
            {
                draggable.SetupSpoon(data.value, data.label);
            }
        }
    }
}