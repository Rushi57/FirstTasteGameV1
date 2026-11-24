using UnityEngine;
using UnityEngine.UI;

public class StopButtonGate : MonoBehaviour
{
    public ColorPrecisionGame game;
    public Button stopButton;
    public TutorialInteractable tutorialTarget;

    private bool? lastState;   // null = unknown, forces a refresh

    private void OnEnable()
    {
        lastState = null;
        game.OnRoundStarting += Lock;
        game.OnHandStopped += Lock;     // immediate lock, before the result is checked
        Apply(game.CanStop);
    }

    private void OnDisable()
    {
        game.OnRoundStarting -= Lock;
        game.OnHandStopped -= Lock;
    }

    private void Update()
    {
        Apply(game.CanStop);              // safety net: always match the game's state
    }

    private void Lock() => Apply(false);

    private void Apply(bool ready)
    {
        if (lastState == ready) return;
        lastState = ready;

        Debug.Log($"[StopButtonGate] ready={ready}");
        stopButton.interactable = ready;
        if (tutorialTarget != null) tutorialTarget.enabled = ready;
    }
}