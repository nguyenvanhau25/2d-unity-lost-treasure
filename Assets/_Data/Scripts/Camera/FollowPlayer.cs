using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : DinoBehaviourScript
{
    [SerializeField] protected Camera mainCam;
    [SerializeField] protected Transform player;
    [SerializeField] protected float speed;
    [SerializeField] protected Vector2 minXY;
    [SerializeField] protected Vector2 maxXY;
    protected override void LoadComponent()
    {
        base.LoadComponent();
        this.LoadCamera();
        this.LoadPlayer();
    }
    protected void FixedUpdate()
    {
        this.Following();
    }
    protected void LoadCamera()
    {
        if (this.mainCam != null) return;
        this.mainCam = FindAnyObjectByType<Camera>();
    }
    protected void LoadPlayer()
    {
        if (this.player != null) return;
        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null) this.player = playerObject.transform;
    }
    protected void Following()
    {
        if (this.player == null) return;
        transform.position = Vector3.Lerp(transform.position, this.player.position, Time.fixedDeltaTime * this.speed);
        Vector3 newPos = transform.position;
        newPos.x = Mathf.Clamp(newPos.x, this.minXY.x, this.maxXY.x);
        newPos.y = Mathf.Clamp(newPos.y, this.minXY.y, this.maxXY.y);
        transform.position = newPos;
    }
}
