using UnityEngine;

public class UISfxTrigger : MonoBehaviour
{
    public void FridgeOpen() { if (SFXManager.Instance != null) SFXManager.Instance.PlayFridgeOpen(); }
    public void FridgeClose() { if (SFXManager.Instance != null) SFXManager.Instance.PlayFridgeClose(); }
    public void CabinetOpen() { if (SFXManager.Instance != null) SFXManager.Instance.PlayCabinetOpen(); }
    public void CabinetClose() { if (SFXManager.Instance != null) SFXManager.Instance.PlayCabinetClose(); }
}