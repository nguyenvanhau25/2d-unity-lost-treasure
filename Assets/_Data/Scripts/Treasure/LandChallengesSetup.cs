using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Builds the collectible and challenge objects required by each stage.</summary>
public class LandChallengesSetup : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoadedBootstrap()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "MainMenu" || scene == "BeginScreen") return;
        new GameObject("LandChallengesSetup").AddComponent<LandChallengesSetup>();
    }

    private void Start()
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "ForestStartScene": SpawnFragmentBeforeExit(0, "DarkForestScene", "KeyFragment_Forest", "Mảnh chìa khóa Rừng Xanh"); break;
            case "DarkForestScene": SetupDarkForestScene(); break;
            case "InsideCave": SetupInsideCaveScene(); break;
            case "RecusePrincess":
                if (FindFirstObjectByType<CursedTempleChallenge>() == null) gameObject.AddComponent<CursedTempleChallenge>();
                break;
            case "Special": SetupSpecialSanctumScene(); break;
        }
    }

    private void SetupDarkForestScene()
    {
        SpawnFragmentBeforeExit(1, "InsideCave", "KeyFragment_DarkForest", "Mảnh chìa khóa Rừng Hắc Ám");
        foreach (OrcAttack attack in FindObjectsByType<OrcAttack>()) attack.ApplyDarkForestBalance();
        SpawnSpikeTrap(new Vector3(-5f, -0.7f, 0f), 1.5f);
        SpawnSpikeTrap(new Vector3(8f, -0.7f, 0f), 2.5f);
    }

    private void SetupInsideCaveScene()
    {
        SpawnFragmentBeforeExit(2, "LastWar", "KeyFragment_InsideCave", "Mảnh chìa khóa Hang Động");
        SpawnSpikeTrap(new Vector3(-10f, -0.6f, 0f), 0.3f);
        SpawnSpikeTrap(new Vector3(4f, -0.6f, 0f), 1.1f);
        SpawnSpikeTrap(new Vector3(18f, -0.6f, 0f), 0.6f);
    }

    private void SetupSpecialSanctumScene()
    {
        if (FindFirstObjectByType<MysteryTreasureChest>() != null) return;
        GameObject chest = new GameObject("MysteryTreasureChest");
        chest.transform.position = new Vector3(0.16f, 17.6f, 0f);
        BoxCollider2D collider = chest.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(2f, 1.8f);
        collider.isTrigger = true;
        chest.AddComponent<MysteryTreasureChest>();
    }

    private void SpawnSpikeTrap(Vector3 position, float delay)
    {
        GameObject trapObject = new GameObject("AncientSpikeTrap");
        trapObject.transform.position = position;
        BoxCollider2D collider = trapObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1.8f, 0.8f);
        collider.isTrigger = true;
        trapObject.AddComponent<AncientSpikeTrap>().StartDelay = delay;
    }

    private void SpawnFragmentBeforeExit(int index, string destination, string objectName, string fragmentName)
    {
        if (LostTreasureManager.Instance.HasFragment(index)) return;
        foreach (KeyFragmentPickup existing in FindObjectsByType<KeyFragmentPickup>())
            if (existing.FragmentIndex == index) return;

        foreach (SceneTransition transition in FindObjectsByType<SceneTransition>())
        {
            if (transition.DestinationSceneName != destination) continue;
            GameObject fragment = new GameObject(objectName);
            fragment.transform.position = transition.transform.position + Vector3.left * 2.5f + Vector3.up;
            CircleCollider2D collider = fragment.AddComponent<CircleCollider2D>();
            collider.radius = 0.8f;
            collider.isTrigger = true;
            KeyFragmentPickup pickup = fragment.AddComponent<KeyFragmentPickup>();
            pickup.FragmentIndex = index;
            pickup.FragmentName = fragmentName;
            return;
        }

        Debug.LogWarning($"No exit to {destination} found for fragment {index} in {gameObject.scene.name}.");
    }
}
