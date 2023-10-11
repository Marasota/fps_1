using System.Collections;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Models;
public class MyWeaponController: MonoBehaviour
{
    private MyCharacterController _characterController;

    [Header("References")]
    public Animator weaponAnimator;
    public MyBullet bulletPrefab;
    private MyBullet currentArrow;
    public Transform bulletSpawn;
    public Camera fpsCamera;



    [Header("Settings")]
    public WeaponSettingsModel settings;

    bool isInitialized;

    Vector3 newWeaponRotation;
    Vector3 newWeaponRotationVelocity;

    Vector3 targetWeaponRotation;
    Vector3 targetWeaponRotationVelocity;

    Vector3 newWeaponMovementRotation;
    Vector3 newWeaponMovementRotationVelocity;

    Vector3 targetWeaponMovementRotation;
    Vector3 targetWeaponMovementRotationVelocity;

    private bool isGroundedTrigger;

    public float fallingDelay;

    [Header("Weapon Sway")]
    public Transform weaponSwayObject;

    public float swayAmountA = 1;
    public float swayAmountB = 2;
    public float swayScale = 600;
    public float swayLerpSpeed = 14;

    public float swayTime;
    public Vector3 swayPosition;

    [Header("Sights")]
    public Transform sightTarget;
    public float sightOffset;
    public float aimingInTime;
    public Vector3 weaponSwayPosition;
    public Vector3 weaponSwayPositionVelocity;


    [Header("Shooting")]
    public float rateOfFire;
    public float rangeOfFire;
    [HideInInspector]
    public bool isShooting;
    public float reloadTime;



    [HideInInspector]
    public bool isAimingIn;

    private void Start()
    {
        newWeaponRotation = transform.localRotation.eulerAngles;
    }

    public void Initialise(MyCharacterController characterController)
    {
        _characterController = characterController;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized)  { return; }
        
        CalculateWeaponRotation();
        SetWeaponAnimations();
        CalculateWeaponSway();
        CalculateAimingIn();
        CalculateShooting();
    }

    private void CalculateShooting()
    {
        if (isShooting)
        {
            Shoot();
            isShooting = false;
        }
    }

    private void Shoot()
    {
        RaycastHit hit;

        if (Physics.Raycast(fpsCamera.transform.position, fpsCamera.transform.forward, out hit, rangeOfFire))
        {
            Debug.Log(hit.transform.name);

            Target target = hit.transform.GetComponent<Target>();
            if(target != null)
            {
                target.TakeDamage(rateOfFire);
            }
        }

//        var bullet = Instantiate(bulletPrefab, bulletSpawn);
    }

    private void CalculateAimingIn()
    {
        var targetPosition = transform.position;

        if (isAimingIn)
        {
            targetPosition = _characterController.camera.transform.position + 
                (weaponSwayObject.transform.position - sightTarget.position)
                + (_characterController.camera.transform.forward * sightOffset);
        }

        weaponSwayPosition = weaponSwayObject.transform.position;
        weaponSwayPosition = Vector3.SmoothDamp(weaponSwayPosition, targetPosition, ref weaponSwayPositionVelocity, aimingInTime);
        weaponSwayObject.transform.position = weaponSwayPosition + swayPosition;
    }

    public void TriggerJump()
    {
        weaponAnimator.SetTrigger("Jump");
        isGroundedTrigger = false;
    }
     
    private void CalculateWeaponRotation()
    { 
        targetWeaponRotation.y += (isAimingIn ? settings.SwayAmount / 3 : settings.SwayAmount) * _characterController.GetInputView().x * Time.deltaTime;
        targetWeaponRotation.x -= (isAimingIn ? settings.SwayAmount / 3 : settings.SwayAmount) * _characterController.GetInputView().y * Time.deltaTime;

        targetWeaponRotation.x = Mathf.Clamp(targetWeaponRotation.x, -settings.SwayClampX, settings.SwayClampX);
        targetWeaponRotation.y = Mathf.Clamp(targetWeaponRotation.y, -settings.SwayClampY, settings.SwayClampY);
        targetWeaponRotation.z = isAimingIn ? 0 : targetWeaponRotation.y;

        targetWeaponRotation = Vector3.SmoothDamp(targetWeaponRotation, Vector3.zero, ref targetWeaponRotationVelocity, settings.SwayResetSmooting);
        newWeaponRotation = Vector3.SmoothDamp(newWeaponRotation, targetWeaponRotation, ref newWeaponRotationVelocity, settings.SwaySmooting);

        targetWeaponMovementRotation.y = (isAimingIn ? settings.MovementSwayX / 3 : settings.MovementSwayX ) * -_characterController.GetInputMovement().x;
        targetWeaponMovementRotation.x = (isAimingIn ? settings.MovementSwayY / 3 : settings.MovementSwayY) * -_characterController.GetInputMovement().y;
        //targetWeaponMovementRotation.z = targetWeaponMovementRotation.y;

        targetWeaponMovementRotation = Vector3.SmoothDamp(targetWeaponMovementRotation, Vector3.zero, ref targetWeaponMovementRotationVelocity, settings.SwayResetSmooting);
        newWeaponMovementRotation = Vector3.SmoothDamp(newWeaponMovementRotation, targetWeaponMovementRotation, ref newWeaponMovementRotationVelocity, settings.SwaySmooting);

        transform.localRotation = Quaternion.Euler(newWeaponRotation + newWeaponMovementRotation);
    }

    private void SetWeaponAnimations()
    {
        if (isGroundedTrigger)
        {
            fallingDelay = 0;
        }
        else
        {
            fallingDelay += Time.deltaTime;
        }

        if (_characterController.isGrounded && !isGroundedTrigger && fallingDelay > 0.1f)
        {
            weaponAnimator.SetTrigger("Land");
            isGroundedTrigger = true;
        }
        else if (!_characterController.isGrounded && isGroundedTrigger)
        {
            weaponAnimator.SetTrigger("Falling");
            isGroundedTrigger = false;
        }
        
        weaponAnimator.SetBool("IsSprinting", _characterController.IsSprinting());
        weaponAnimator.SetFloat("WeaponAnimationSpeed", _characterController.weaponAnimationSpeed);
    }

    #region - Sway -

    private void CalculateWeaponSway()
    {
        var targetPosition = LissajousCurve(swayTime, swayAmountA, swayAmountB) / (isAimingIn ? swayScale * 3 : swayScale);

        swayPosition = Vector3.Lerp(swayPosition, targetPosition, Time.smoothDeltaTime * swayLerpSpeed);
        swayTime += Time.deltaTime;

        if (swayTime > 6.3f)
        {
            swayTime = 0;
        }
    }

    private Vector3 LissajousCurve(float Time, float A, float B)
    {
        return new Vector3(Mathf.Sin(Time), A * Mathf.Sin(B * Time + Mathf.PI));
    }

    #endregion
}
