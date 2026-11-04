using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIDisplay : DinoBehaviourScript
{
    [SerializeField] private GameObject arrowFollow;
    protected override void Start()
    {
        base.Start();
        this.Close();

        if (arrowFollow != null)
        {
            arrowFollow.SetActive(false);
        }
    }
    public void Open()
    {

        gameObject.SetActive(true);
        if (arrowFollow != null)
        {
            arrowFollow.SetActive(true);
        }
    }

    public void Close()
    {

        gameObject.SetActive(false);
        if (arrowFollow != null) arrowFollow.SetActive(false);
    }
}
