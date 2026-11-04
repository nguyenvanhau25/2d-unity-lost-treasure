using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayGoalTutorial : DinoBehaviourScript
{
    [SerializeField] private NotificationEffect notificationEffect;
    [SerializeField] GameObject arrowFollow1;
    protected void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (UIGoalTutorial.Instance != null) UIGoalTutorial.Instance.Open();
        }
    }

    protected void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (UIGoalTutorial.Instance != null) UIGoalTutorial.Instance.Close();
            if (notificationEffect != null) notificationEffect.HideNotification();
            if (arrowFollow1 != null) arrowFollow1.SetActive(false);
        }
    }
}
