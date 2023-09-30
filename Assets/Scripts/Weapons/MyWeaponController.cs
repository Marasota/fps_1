using UnityEditor;
using UnityEngine;
using static Models;
public class MyWeaponController: MonoBehaviour
{
    private MyCharacterController _characterController;

    [Header("Settings")]
    public WeaponSettingsModel settings;

    bool isInitialized;

    Vector3 newWeaponRotation;
    Vector3 newWeaponRotationVelocity;

    Vector3 targetWeaponRotation;
    Vector3 targetWeaponRotationVelocity;


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
        
        targetWeaponRotation.y += settings.SwayAmount * _characterController.GetInputView().x * Time.deltaTime;
        targetWeaponRotation.x -= settings.SwayAmount * _characterController.GetInputView().y * Time.deltaTime;
       
        targetWeaponRotation.x = Mathf.Clamp(targetWeaponRotation.x, -settings.SwayClampX, settings.SwayClampX);
        targetWeaponRotation.y = Mathf.Clamp(targetWeaponRotation.y, -settings.SwayClampY, settings.SwayClampY);

        targetWeaponRotation = Vector3.SmoothDamp(targetWeaponRotation, Vector3.zero, ref targetWeaponRotationVelocity, settings.SwayResetSmooting);
        newWeaponRotation = Vector3.SmoothDamp(newWeaponRotation, targetWeaponRotation, ref newWeaponRotationVelocity, settings.SwaySmooting);


        transform.localRotation = Quaternion.Euler(newWeaponRotation);
    }
}
