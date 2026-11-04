using UnityEngine;

/// <summary>
/// Scene-local manager marker kept for existing scene references. It provides a
/// stable access point without surviving scene changes or creating duplicates.
/// </summary>
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
