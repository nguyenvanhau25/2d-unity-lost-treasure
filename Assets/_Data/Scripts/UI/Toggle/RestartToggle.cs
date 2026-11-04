using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RestartToggle : BaseToggle
{
    protected static RestartToggle instance;
    public static RestartToggle Instance => instance;

    [Header("Restart Toggle")]
    [SerializeField] protected Transform restartMenu;
    protected override void Awake()
    {
        base.Awake();
        if (RestartToggle.instance != null) Debug.LogWarning("Only 1 RestartToggle allow to exist");
        RestartToggle.instance = this;
    }
    protected override void Start()
    {
        base.Start();
        if (this.sceneTrans != null) this.sceneTrans.gameObject.SetActive(true);
        if (this.restartMenu != null) this.restartMenu.gameObject.SetActive(false);
    }
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadRestartMenu();
    }

    protected void LoadRestartMenu()
    {
        if (this.restartMenu != null) return;
        GameObject restartMenuObject = GameObject.Find("RestartMenu");
        if (restartMenuObject != null) this.restartMenu = restartMenuObject.transform;
    }

    public void RestartGameMenu()
    {
        StartCoroutine(Wait2DisplayMenu());
    }

    IEnumerator Wait2DisplayMenu()
    {
        yield return new WaitForSeconds(3);
        if (this.sceneTrans != null) this.sceneTrans.gameObject.SetActive(false);
        if (this.restartMenu != null) this.restartMenu.gameObject.SetActive(true);
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.PlaySFX("GameOver");
    }
}
