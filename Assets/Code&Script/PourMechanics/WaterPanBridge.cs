using UnityEngine;

public class WaterPanBridge : MonoBehaviour
{
    public PanLiquidFill panLiquid;
    public float waterMl = 250f;

    public void OnWaterPoured()
    {
        Debug.Log("[WaterBridge] OnWaterPoured fired");
        if (panLiquid == null)
        {
            Debug.LogWarning("[WaterBridge] Pan Liquid is NOT assigned.");
            return;
        }
        panLiquid.AddWater(waterMl);
        Debug.Log($"[WaterBridge] Added {waterMl} ml water to the pan");
    }
}