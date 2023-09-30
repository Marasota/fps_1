using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Models;

public class MyCharacterController : MonoBehaviour
{
    private CharacterController _characterController;
    private DefaultInput _defaultInput;
    private Vector2 _inputMovement;
    private Vector2 _inputView;

    private Vector3 _newCameraRotation;
    private Vector3 _newCharacterRotation;

    [Header("References")]
    public Transform cameraHolder;
    public Transform feetTransform;


    [Header("Settings")]
    public PlayerSettingsModel playerSettings;
    public float viewClampYMin = -70;
    public float viewClampXMin = 80;
    public LayerMask playerMask;


    [Header("Gravity")]
    public float gravityAmount;
    public float gravityMin;
    private float _playerGravity;

    public Vector3 jumpingForce;
    private Vector3 _jumpingForceVelocity;

    [Header("Stance")]
    public PlayerStance playerStance;
    public float playerStanceSmoothing;
    public CharacterStance playerStandStance;
    public CharacterStance playerCrouchStance;
    public CharacterStance playerProneStance;
    public float stanceCheckErrorMargin = 0.05f;
    private float _cameraHeight;
    private float _cameraHeightVelocity;

    private Vector3 _stanceCapsuleCenterVelocity;
    private float _stanceCapsuleHeightVelocity;

    private bool _isSprinting;

    private Vector3 _newMovementSpeed;
    private Vector3 _newMovementSpeedVelocity;

    [Header("Weapon")]
    public MyWeaponController currentWeapon;

    private void Start()
    {
        Cursor.visible = false;
    }
    private void Awake()
    {
        _defaultInput = new DefaultInput();
        
        _defaultInput.Character.Movement.performed += e => _inputMovement = e.ReadValue<Vector2>();
        _defaultInput.Character.View.performed += e => _inputView = e.ReadValue<Vector2>();
        _defaultInput.Character.Jump.performed += e => Jump();
        _defaultInput.Character.Crouch.performed += e => Crouch();
        _defaultInput.Character.Prone.performed += e => Prone();
        _defaultInput.Character.Sprint.performed += e => ToggleSprint();
        _defaultInput.Character.SprintReleased.performed += e => StopSprint();

        _defaultInput.Enable();

        _newCameraRotation = cameraHolder.localRotation.eulerAngles;
        _newCharacterRotation = transform.localRotation.eulerAngles;

       if(!TryGetComponent<CharacterController>(out _characterController))
        {
            _characterController = null;
        }

        _cameraHeight = cameraHolder.localPosition.y;

        if (currentWeapon)
        {
            currentWeapon.Initialise(this);
         }
    }   

    private void Update()
    {
        CalculateView();
        CalculateMovement();
        CalculateJump();
        CalculateStance();
    }

    private void CalculateView()
    {
        _newCharacterRotation.y += playerSettings.ViewXSensitivity * _inputView.x * Time.deltaTime;
        transform.localRotation = Quaternion.Euler(_newCharacterRotation);

        _newCameraRotation.x -= playerSettings.ViewYSensitivity * _inputView.y * Time.deltaTime;
        _newCameraRotation.x = Mathf.Clamp(_newCameraRotation.x, viewClampYMin, viewClampXMin);

        cameraHolder.localRotation = Quaternion.Euler(_newCameraRotation);
    }

    private void CalculateMovement()
    {
        if (_inputMovement.y <= 0.2f)
        {
            _isSprinting = false;
        }

        var verticalSpeed = playerSettings.WalkingForwardSpeed;
        var horizontalSpeed = playerSettings.WalkingStrafeSpeed;

        if (_isSprinting)
        {
            verticalSpeed = playerSettings.RunningForwardSpeed;
            horizontalSpeed = playerSettings.RunningStrafeSpeed;
        }

        if (!_characterController.isGrounded)
        {
            playerSettings.SpeedEffector = playerSettings.FallingSpeedEffector;
        }
        else if(playerStance == PlayerStance.Crouch)
        {
            playerSettings.SpeedEffector = playerSettings.CrouchSpeedEffector;
        }
        else if (playerStance == PlayerStance.Prone)
        {
            playerSettings.SpeedEffector = playerSettings.ProneSpeedEffector;
        }
        else
        {
            playerSettings.SpeedEffector = 1;
        }

        verticalSpeed *= playerSettings.SpeedEffector;
        horizontalSpeed *= playerSettings.SpeedEffector;

        _newMovementSpeed = Vector3.SmoothDamp(_newMovementSpeed, new Vector3(horizontalSpeed * _inputMovement.x * Time.deltaTime, 0, verticalSpeed * _inputMovement.y * Time.deltaTime), ref _newMovementSpeedVelocity, _characterController.isGrounded ? playerSettings.MovementSmoothing : playerSettings.FallingSmoothing);
        var movementSpeed = transform.TransformDirection(_newMovementSpeed);

        if(_playerGravity > gravityMin)
        {
            _playerGravity -= gravityAmount * Time.deltaTime;
        }
     
        if(_playerGravity < -0.1f && _characterController.isGrounded)
        {
            _playerGravity = -0.1f;
        }

        movementSpeed.y += _playerGravity;
        movementSpeed += jumpingForce;

        _characterController.Move(movementSpeed );
    }

    private void CalculateJump()
    {
        jumpingForce = Vector3.SmoothDamp(jumpingForce, Vector3.zero, ref _jumpingForceVelocity, playerSettings.JumpingFalloff);
    } 

    private void CalculateStance()
    {
        var currentStance = playerStandStance;
        
        if(playerStance == PlayerStance.Crouch)
        {
            currentStance = playerCrouchStance;
        }
        else if(playerStance == PlayerStance.Prone)
        {
            currentStance = playerProneStance;
        }

        _cameraHeight = Mathf.SmoothDamp(cameraHolder.localPosition.y, currentStance.CameraHeight, ref _cameraHeightVelocity, playerStanceSmoothing);
        cameraHolder.localPosition = new Vector3(cameraHolder.localPosition.x, _cameraHeight, cameraHolder.localPosition.z);

        _characterController.height = Mathf.SmoothDamp(_characterController.height, currentStance.StanceCollider.height, ref _stanceCapsuleHeightVelocity, playerStanceSmoothing);
        _characterController.center = Vector3.SmoothDamp(_characterController.center, currentStance.StanceCollider.center,ref _stanceCapsuleCenterVelocity, playerStanceSmoothing);

    }

    private void Jump()
    {
        if(!_characterController.isGrounded || playerStance == PlayerStance.Prone)
        {
            return;
        }
        if(playerStance == PlayerStance.Crouch )
        {
            if (StanceCheck(playerStandStance.StanceCollider.height))
            {
                return;
            }
        }

        jumpingForce = Vector3.up * playerSettings.JumpingHeight;
        _playerGravity = 0;
        playerStance = PlayerStance.Stand;
    }

    private void Crouch()
    {
        if(playerStance == PlayerStance.Crouch)
        {
            if (!StanceCheck(playerStandStance.StanceCollider.height))
            {
                playerStance = PlayerStance.Stand;
            }
        }
        else if (!StanceCheck(playerCrouchStance.StanceCollider.height))
        {
            playerStance = PlayerStance.Crouch;
        }
        
    }

    private void Prone()
    {
        if (playerStance == PlayerStance.Prone)
        {
            if (!StanceCheck(playerStandStance.StanceCollider.height))
            {
                playerStance = PlayerStance.Stand;
            }
        }
        else
        {
            playerStance = PlayerStance.Prone;
        }
    }

    private bool StanceCheck(float stanceCheckHeight)
    {
        var start = new Vector3(feetTransform.position.x, feetTransform.position.y + _characterController.radius  + stanceCheckErrorMargin, feetTransform.position.z);
        var end = new Vector3(feetTransform.position.x, feetTransform.position.y - _characterController.radius - stanceCheckErrorMargin + stanceCheckHeight, feetTransform.position.z);

        return Physics.CheckCapsule(start,end, _characterController.radius, playerMask);
    }

    private void ToggleSprint()
    {
        if (_inputMovement.y <= 0.2f)
        {
            _isSprinting = false;
        }
        else
        {
            _isSprinting = !_isSprinting;

        }
    }

    private void StopSprint()
    {
        if (playerSettings.SprintingHold)
        {
            _isSprinting = false;
        }
    }

   public Vector2 GetInputView() { return _inputView; }
}
