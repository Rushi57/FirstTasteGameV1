using UnityEngine;
using System.Collections.Generic;

public class OSUSpawnManager : MonoBehaviour
{
    [Header("Prefab & Parent References")]
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private RectTransform spawnArea;

    [Header("Random Target Quantity Settings")]
    [SerializeField] private int minCircles = 1;
    [SerializeField] private int maxCircles = 5;

    [Header("Speed & Timing Settings")]
    [SerializeField] private float baseShrinkDuration = 1.2f;  // Base time for Target 1 to shrink
    [SerializeField] private float sequenceDelayOffset = 0.45f; // Delay between each target's ring start time
    [SerializeField] private float speedMultiplierPerTarget = 0.05f; // Slightly speeds up each subsequent target
    [SerializeField] private float perfectGraceWindow = 0.35f;

    [Header("Scale Settings")]
    [SerializeField] private float minStartScale = 2.0f;
    [SerializeField] private float maxStartScale = 3.5f;

    private List<Vector2> spawnedPositions = new List<Vector2>();

    private void OnEnable()
    {
        SpawnRandomSequence();
    }

    public void SpawnRandomSequence()
    {
        foreach (Transform child in spawnArea)
        {
            Destroy(child.gameObject);
        }
        spawnedPositions.Clear();

        int totalToSpawn = Random.Range(minCircles, maxCircles + 1);

        for (int i = 1; i <= totalToSpawn; i++)
        {
            GameObject newTarget = Instantiate(targetPrefab, spawnArea);
            RectTransform targetRect = newTarget.GetComponent<RectTransform>();

            Vector2 randomPos = GetRandomPosition(targetRect);
            targetRect.anchoredPosition = randomPos;

            float startScale = Random.Range(minStartScale, maxStartScale);

            // 1. Calculate staggered start delay (Target 1 starts immediately, Target 2 waits 0.45s, etc.)
            float delay = (i - 1) * sequenceDelayOffset;

            // 2. Adjust duration per sequence number so later targets shrink slightly faster or remain steady
            float targetDuration = Mathf.Max(0.5f, baseShrinkDuration - ((i - 1) * speedMultiplierPerTarget));

            OSUTargetItem targetScript = newTarget.GetComponent<OSUTargetItem>();
            targetScript.SetupTarget(i, targetDuration, perfectGraceWindow, startScale, delay, OnTargetCompleted);
        }
    }

    private Vector2 GetRandomPosition(RectTransform targetRect)
    {
        float widthBoundary = (spawnArea.rect.width - targetRect.rect.width) / 2f;
        float heightBoundary = (spawnArea.rect.height - targetRect.rect.height) / 2f;

        Vector2 pos;
        int attempts = 0;

        do
        {
            float randomX = Random.Range(-widthBoundary, widthBoundary);
            float randomY = Random.Range(-heightBoundary, heightBoundary);
            pos = new Vector2(randomX, randomY);
            attempts++;
        }
        while (IsTooCloseToOthers(pos) && attempts < 20);

        spawnedPositions.Add(pos);
        return pos;
    }

    private bool IsTooCloseToOthers(Vector2 pos)
    {
        foreach (Vector2 existingPos in spawnedPositions)
        {
            if (Vector2.Distance(pos, existingPos) < 130f)
                return true;
        }
        return false;
    }

    private void OnTargetCompleted(OSUTargetItem item, int score)
    {
        if (spawnArea.childCount <= 1)
        {
            Debug.Log("Sequence Complete!");
        }
    }
}