using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Models;

public class MyCharacterController : MonoBehaviour
{
    private CharacterController _characterController;
    private DefaultInput _defaultInput;
    public Vector2 inputMovement;
    public Vector2 inputView;

    private Vector3 _newCameraRotation;
    private Vector3 _newCharacterRotation;

    [Header("References")]
    public Transform cameraHolder;


    [Header("Settings")]
    public PlayerSettingsModel playerSettings;
    public float viewClampYMin = -70;
    public float viewClampXMin = 80;


    [Header("Gravity")]
    public float gravityAmount;
    public float gravityMin;
    private float _playerGravity;

    public Vector3 jumpingForce;
    private Vector3 _jumpingForceVelocity;
    private void Start()
    {
        Cursor.visible = false;
    }
    private void Awake()
    {
        _defaultInput = new DefaultInput();
        
        _defaultInput.Character.Movement.performed += e => inputMovement = e.ReadValue<Vector2>();
        _defaultInput.Character.View.performed += e => inputView = e.ReadValue<Vector2>();
        _defaultInput.Character.Jump.performed += e => Jump();

        _defaultInput.Enable();

        _newCameraRotation = cameraHolder.localRotation.eulerAngles;
        _newCharacterRotation = transform.localRotation.eulerAngles;

       if(!TryGetComponent<CharacterController>(out _characterController))
        {
            _characterController = null;
        }
    }   

    private void Update()
    {
        CalculateView();
        CalculateMovement();
        CalculateJump();
    }

    private void CalculateView()
    {
        _newCharacterRotation.y += playerSettings.ViewXSensitivity * inputView.x * Time.deltaTime;
        transform.localRotation = Quaternion.Euler(_newCharacterRotation);

        _newCameraRotation.x -= playerSettings.ViewYSensitivity * inputView.y * Time.deltaTime;
        _newCameraRotation.x = Mathf.Clamp(_newCameraRotation.x, viewClampYMin, viewClampXMin);

        cameraHolder.localRotation = Quaternion.Euler(_newCameraRotation);
    }

    private void CalculateMovement()
    {
        var verticalSpeed = playerSettings.WalkingForwardSpeed * inputMovement.y * Time.deltaTime;
        var horizontalSpeed = playerSettings.WalkingStrafeSpeed * inputMovement.x * Time.deltaTime;

        var newMovementSpeed = new Vector3(horizontalSpeed, 0, verticalSpeed);

        newMovementSpeed = transform.TransformDirection(newMovementSpeed);

        if(_playerGravity > gravityMin && jumpingForce.y < 0.1f)
        {
            _playerGravity -= gravityAmount * Time.deltaTime;
        }
     
        if(_playerGravity < -1 && _characterController.isGrounded)
        {
            _playerGravity = -1;
        }

        if(jumpingForce.y > 0.1f)
        {
            _playerGravity = 0;
        }
        newMovementSpeed.y += _playerGravity;
        newMovementSpeed += jumpingForce;

        _characterController.Move(newMovementSpeed);
    }

    private void CalculateJump()
    {
        jumpingForce = Vector3.SmoothDamp(jumpingForce, Vector3.zero, ref _jumpingForceVelocity, playerSettings.JumpingFalloff);
    } 

    private void Jump()
    { 
        if (!_characterController.isGrounded)
        {
            return;
        }
        jumpingForce = Vector3.up * playerSettings.JumpingHeight;
    }
}
