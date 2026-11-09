using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures only one EventSystem exists in the scene to eliminate Unity warnings.
/// </summary>
public class EventSystemGuard : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CleanDuplicateEventSystems()
    {
        EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (systems.Length > 1)
        {
            for (int i = 1; i < systems.Length; i++)
            {
                if (systems[i] != null)
                {
                    Destroy(systems[i].gameObject);
                }
            }
        }
    }
}
