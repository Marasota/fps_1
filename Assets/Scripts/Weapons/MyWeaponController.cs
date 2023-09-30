using UnityEditor;
using UnityEngine;
using static Models;
public class MyWeaponController: MonoBehaviour
{
    private MyCharacterController _characterController;

    [Header("References")]
    public Animator weaponAnimator;

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
    }

    public void TriggerJump()
    {
        weaponAnimator.SetTrigger("Jump");
        isGroundedTrigger = false;
    }

    private void CalculateWeaponRotation()
    { 
        targetWeaponRotation.y += settings.SwayAmount * _characterController.GetInputView().x * Time.deltaTime;
        targetWeaponRotation.x -= settings.SwayAmount * _characterController.GetInputView().y * Time.deltaTime;

        targetWeaponRotation.x = Mathf.Clamp(targetWeaponRotation.x, -settings.SwayClampX, settings.SwayClampX);
        targetWeaponRotation.y = Mathf.Clamp(targetWeaponRotation.y, -settings.SwayClampY, settings.SwayClampY);
        targetWeaponRotation.z = targetWeaponRotation.y;

        targetWeaponRotation = Vector3.SmoothDamp(targetWeaponRotation, Vector3.zero, ref targetWeaponRotationVelocity, settings.SwayResetSmooting);
        newWeaponRotation = Vector3.SmoothDamp(newWeaponRotation, targetWeaponRotation, ref newWeaponRotationVelocity, settings.SwaySmooting);

        targetWeaponMovementRotation.y = settings.MovementSwayX * -_characterController.GetInputMovement().x;
        targetWeaponMovementRotation.x = settings.MovementSwayY * -_characterController.GetInputMovement().y;
        targetWeaponMovementRotation.z = targetWeaponMovementRotation.y;

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
}
