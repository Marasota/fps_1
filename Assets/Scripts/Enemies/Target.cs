using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public float health = 50f;
    public Animator targetAnimator;

    public void TakeDamage(float amount)
    {
        health -= amount;

        if(health <= 0f)
        {
            StartCoroutine( Die());
        }
    }

    IEnumerator Die()
    {
        if (targetAnimator != null)
        {
            targetAnimator.SetTrigger("Death");
        }
        yield return new WaitForSeconds(3);
        Destroy(gameObject);
    }
}
