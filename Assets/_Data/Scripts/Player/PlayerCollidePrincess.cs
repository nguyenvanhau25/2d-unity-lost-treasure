using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCollidePrincess : MonoBehaviour
{
    protected void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Princess"))
        {
            RescuePrincessStory rescueStory = other.GetComponent<RescuePrincessStory>();
            if (rescueStory != null)
            {
                rescueStory.BeginRescue();
                return;
            }

            if (PlayerCtrl.Instance != null && PlayerCtrl.Instance.PlayerMovement != null && PlayerCtrl.Instance.PlayerMovement._Rb != null)
                PlayerCtrl.Instance.PlayerMovement._Rb.constraints = RigidbodyConstraints2D.FreezeAll;
            if (WinToggle.Instance != null) WinToggle.Instance.WinGameMenu();
        }
    }
}
