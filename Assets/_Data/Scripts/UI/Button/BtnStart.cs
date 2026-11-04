using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BtnStart : BaseButton
{
    [Header("Btn Start")]
    [SerializeField] protected Animator transAnim;
    [SerializeField] protected Canvas canvas;
    protected override void Start()
    {
        base.Start();
        StartCoroutine(this.StartMenuGame());
        if (AudioManagerr.Instance != null) AudioManagerr.Instance.OpenMusic();
    }
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadTransAnim();
    }
    protected void LoadTransAnim()
    {
        if (this.transAnim != null) return;
        if (transform.parent != null)
            this.transAnim = transform.parent.GetComponentInChildren<Animator>();
    }
    protected override void OnClick()
    {
        this.PlayGame();
    }
    protected void PlayGame()
    {
        StartCoroutine(this.LoadGame());
    }
    IEnumerator LoadGame()
    {
        if (this.canvas != null) this.canvas.enabled = true;
        if (this.transAnim != null) this.transAnim.SetTrigger("End");
        yield return new WaitForSeconds(1);
        SceneManager.LoadScene("ForestStartScene");
    }
    IEnumerator StartMenuGame()
    {
        yield return new WaitForSeconds(1);
        if (this.canvas != null) this.canvas.enabled = false;
    }
}
