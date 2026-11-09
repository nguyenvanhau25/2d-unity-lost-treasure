using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCollidePrincess : MonoBehaviour
{
    protected void OnTriggerEnter2D(Collider2D other)
    {
        RescuePrincessStory rescueStory = other.GetComponent<RescuePrincessStory>();
        if (rescueStory != null)
        {
            rescueStory.BeginTempleTransition();
            return;
        }

        if (other.gameObject.CompareTag("Princess"))
        {

            if (PlayerCtrl.Instance != null && PlayerCtrl.Instance.PlayerMovement != null && PlayerCtrl.Instance.PlayerMovement._Rb != null)
                PlayerCtrl.Instance.PlayerMovement._Rb.constraints = RigidbodyConstraints2D.FreezeAll;
            if (WinToggle.Instance != null) WinToggle.Instance.WinGameMenu();
        }
    }
}
