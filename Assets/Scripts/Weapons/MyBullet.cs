
using UnityEngine;

public class MyBullet : MonoBehaviour
{
    private DefaultInput _defaultInput;

    [Header("Settings")]
    public float lifeTime = 1f;
    public float damage;

    private void Awake()
    {
       /* _defaultInput = new DefaultInput(); 
        _defaultInput.Weapon.Fire1Pressed.performed += e => Shoot();*/
    }

    public void Shoot()
    {
        Destroy(gameObject, lifeTime);
    }
}
