using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinToggle : BaseToggle
{
    protected static WinToggle instance;
    public static WinToggle Instance => instance;
    [Header("Win Toggle")]
    [SerializeField] protected Transform winMenu;
    [SerializeField] protected Transform playerMenu;
    protected override void Awake()
    {
        base.Awake();
        if (WinToggle.instance != null) Debug.LogWarning("Only 1 WinToggle allow to exist");
        WinToggle.instance = this;
    }
    protected override void Start()
    {
        base.Start();
        if (this.sceneTrans != null) this.sceneTrans.gameObject.SetActive(true);
        if (this.winMenu != null) this.winMenu.gameObject.SetActive(false);
    }
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadWinMenu();
        this.LoadPlayerMenu();
    }

    protected void LoadWinMenu()
    {
        if (this.winMenu != null) return;
        GameObject winMenuObject = GameObject.Find("WinMenu");
        if (winMenuObject != null) this.winMenu = winMenuObject.transform;
    }

    protected void LoadPlayerMenu()
    {
        if (this.playerMenu != null) return;
        GameObject playerMenuObject = GameObject.Find("UITopLeft/PlayerMenu");
        if (playerMenuObject != null) this.playerMenu = playerMenuObject.transform;
    }
    public void WinGameMenu()
    {
        StartCoroutine(Wait2DisplayMenu());
    }

    IEnumerator Wait2DisplayMenu()
    {
        yield return new WaitForSeconds(2);
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.OpenMusic();
        if (this.sceneTrans != null) this.sceneTrans.gameObject.SetActive(false);
        if (this.playerMenu != null) this.playerMenu.gameObject.SetActive(false);
        if (this.winMenu != null) this.winMenu.gameObject.SetActive(true);
    }
}
