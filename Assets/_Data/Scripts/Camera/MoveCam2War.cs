using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCam2War : DinoBehaviourScript
{
    [SerializeField] protected Camera mainCam;
    [SerializeField] protected FollowPlayer followPlayer;
    [SerializeField] protected FollowTarget followTarget;


    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadCamera();
        this.LoadFollowPlayer();
        this.LoadFollowTarget();
    }
    protected void Update()
    {
        if (BossCtrl.Instance == null || BossCtrl.Instance.BossMovement == null || !BossCtrl.Instance.BossMovement.CanMove()) return;
        if (this.mainCam != null) this.mainCam.orthographicSize = 10;
        if (this.followPlayer != null) this.followPlayer.enabled = false;
        if (this.followTarget != null) this.followTarget.enabled = true;

    }
    protected void LoadCamera()
    {
        if (this.mainCam != null) return;
        this.mainCam = FindAnyObjectByType<Camera>();
    }
    protected void LoadFollowPlayer()
    {
        if (this.followPlayer != null) return;
        GameObject cameraHolder = GameObject.Find("HoldCamera");
        if (cameraHolder != null) this.followPlayer = cameraHolder.GetComponent<FollowPlayer>();
    }

    protected void LoadFollowTarget()
    {
        if (this.followTarget != null) return;
        GameObject cameraHolder = GameObject.Find("HoldCamera");
        if (cameraHolder != null) this.followTarget = cameraHolder.GetComponent<FollowTarget>();
    }


}
