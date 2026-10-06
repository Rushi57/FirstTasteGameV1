using UnityEngine;

/// <summary>
/// Drop this on any UI GameObject that a tutorial step needs to point at or highlight
/// (the onion, the chopping board, the spatula, a shaker, etc). Set a unique targetId
/// per object and use that same string in the TutorialStep asset's Target Id / Drop Zone Id field.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TutorialTarget : MonoBehaviour
{
    [SerializeField] private string targetId;

    private RectTransform rt;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        TutorialTargetRegistry.Instance?.Register(targetId, rt);
    }

    void OnDisable()
    {
        TutorialTargetRegistry.Instance?.Unregister(targetId);
    }
}