using UnityEngine;
using UnityEngine.SceneManagement;

public class BtnBackMainMenu : BaseButton
{
    [Header("Btn Back Main Menu")]
    [SerializeField] protected SceneName sceneName;

    protected override void OnClick()
    {
        this.BackFirstScene();
    }
    protected void BackFirstScene()
    {
        SceneManager.LoadScene(LostTreasureSceneHelper.ResolveLoadableSceneName(sceneName));
    }
 
}
