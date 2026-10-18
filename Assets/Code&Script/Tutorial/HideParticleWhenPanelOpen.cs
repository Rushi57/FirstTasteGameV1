using UnityEngine;

public class HideParticleWhenPanelOpen : MonoBehaviour
{
    [Tooltip("The Particle System to hide. Leave empty to use the one on this object.")]
    public ParticleSystem target;

    [Tooltip("If any of these objects is active, the particle is hidden.")]
    public GameObject[] blockingPanels;

    private ParticleSystemRenderer psRenderer;

    private void Awake()
    {
        if (target == null) target = GetComponent<ParticleSystem>();
        if (target != null) psRenderer = target.GetComponent<ParticleSystemRenderer>();
    }

    private void LateUpdate()
    {
        if (psRenderer == null) return;

        bool blocked = false;
        for (int i = 0; i < blockingPanels.Length; i++)
        {
            GameObject p = blockingPanels[i];
            if (p != null && p.activeInHierarchy)
            {
                blocked = true;
                break;
            }
        }

        // Only write when the value actually changes
        if (psRenderer.enabled == blocked)
            psRenderer.enabled = !blocked;
    }
}