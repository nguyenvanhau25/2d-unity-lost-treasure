using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages environmental puzzles involving AncientRunePillars.
/// When the runes are activated correctly, unlocks a hidden path or KeyFragment.
/// </summary>
public class AncientRunePuzzle : MonoBehaviour
{
    [Header("Puzzle Configuration")]
    [SerializeField] private AncientRunePillar[] runePillars;
    [SerializeField] private bool requireOrderedSequence = false;
    [SerializeField] private int[] requiredSequence;

    [Header("Unlock Targets")]
    [SerializeField] private GameObject objectToReveal;
    [SerializeField] private GameObject barrierToDisable;
    [TextArea(2, 4)]
    [SerializeField] private string solveMessage = "Cơ quan cổ đại đã giải mã thành công!\nMảnh chìa khóa ẩn đã xuất hiện!";

    private readonly List<int> currentInputSequence = new List<int>();
    private bool isSolved;

    public bool IsPuzzleSolved => this.isSolved;

    public void Configure(AncientRunePillar[] pillars, GameObject revealOnSolve, GameObject barrierToDisable)
    {
        this.runePillars = pillars;
        this.objectToReveal = revealOnSolve;
        this.barrierToDisable = barrierToDisable;

        for (int index = 0; index < pillars.Length; index++)
            if (pillars[index] != null) pillars[index].Configure(index, this);
    }

    private void Start()
    {
        if (this.objectToReveal != null)
        {
            this.objectToReveal.SetActive(false);
        }
    }

    public void OnPillarActivated(AncientRunePillar pillar)
    {
        if (this.isSolved) return;

        if (this.requireOrderedSequence && this.requiredSequence != null && this.requiredSequence.Length > 0)
        {
            this.currentInputSequence.Add(pillar.RuneId);

            int currentStep = this.currentInputSequence.Count - 1;
            if (pillar.RuneId != this.requiredSequence[currentStep])
            {
                // Wrong step, reset
                StartCoroutine(this.ResetAllPillarsAfterMistake());
                return;
            }

            if (this.currentInputSequence.Count == this.requiredSequence.Length)
            {
                this.SolvePuzzle();
            }
        }
        else
        {
            // Simple: All pillars must be activated
            bool allActive = true;
            foreach (AncientRunePillar p in this.runePillars)
            {
                if (p != null && !p.IsActivated)
                {
                    allActive = false;
                    break;
                }
            }

            if (allActive)
            {
                this.SolvePuzzle();
            }
        }
    }

    private void SolvePuzzle()
    {
        if (this.isSolved) return;
        this.isSolved = true;

        if (AudioManagerr.Instance != null)
        {
            AudioManagerr.Instance.PlaySFX("SelectSound", 1.4f);
        }

        if (this.objectToReveal != null)
        {
            this.objectToReveal.SetActive(true);
        }

        if (this.barrierToDisable != null)
        {
            this.barrierToDisable.SetActive(false);
        }

        SceneStoryOverlay overlay = SceneStoryOverlay.GetOrCreate();
        if (overlay != null && !string.IsNullOrEmpty(this.solveMessage))
        {
            StartCoroutine(overlay.ShowMessage("CƠ QUAN CỔ ĐẠI", this.solveMessage, 3f, new Color(0.3f, 0.9f, 1f)));
        }
    }

    private IEnumerator ResetAllPillarsAfterMistake()
    {
        yield return new WaitForSeconds(0.4f);
        this.currentInputSequence.Clear();
        foreach (AncientRunePillar p in this.runePillars)
        {
            if (p != null) p.ResetRune();
        }
    }
}
