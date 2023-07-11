using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private Camera currentCamera;
    [SerializeField] private Transform cameraRoot;
    [SerializeField, Range(0.1f, 10f)] private float sensitivity = 2f;
    [SerializeField] private float speed;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -15f;
    private CharacterController _characterController;
    private PlayerInput _playerInput;
    private InputAction _moveActionIDs;
    private InputAction _jumpActionIDs;
    private InputAction _lookActionIDs;

    private Animator _animator;
    private bool _hasAnimator;
    
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;
    
    private float _animationBlend;
    private float _currentVelocity;
    private float lerpSpeed;
    private float _verticalVelocity;
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    private bool _isGround = true;
    
    
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;
    
    private void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        _hasAnimator = TryGetComponent(out _animator);
        
        _playerInput = GetComponent<PlayerInput>();
        _characterController = GetComponent<CharacterController>();
        
        AssignActionsIDs();
        AssignAnimationIDs();
        
        _jumpTimeoutDelta = 0.3f;
        _fallTimeoutDelta = 0.15f;
    }

    private void AssignActionsIDs()
    {
        _moveActionIDs = _playerInput.actions["Move"];
        _jumpActionIDs = _playerInput.actions["Jump"];
        _lookActionIDs = _playerInput.actions["MouseLook"];
    }
    
    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void Update()
    {
        GroundedCheck();
        JumpAndGravity();
        Move();
    }
    
    private void LateUpdate()
    { 
        CameraRotation();
    }
    
    private void GroundedCheck()
    {
        var position = transform.position;
        var spherePosition = new Vector3(position.x, position.y + 0.14f, position.z);
        _isGround = Physics.CheckSphere(spherePosition, 0.28f, groundLayers, QueryTriggerInteraction.Ignore);
        if (_hasAnimator)
            _animator.SetBool(_animIDGrounded, _isGround);
    }

    private void Move()
    {
        var targetSpeed = speed;
        var move = ReadValueVector2(_moveActionIDs);
        var velocity = _characterController.velocity;
        var currentHorizontalSpeed = new Vector3(velocity.x, 0, velocity.z).magnitude;
    
        if (move == Vector2.zero) targetSpeed = 0.0f;
        
        lerpSpeed = targetSpeed;
        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * 10f);
        
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        var inputDirection = new Vector3(move.x, 0.0f, move.y).normalized;

        var targetRotation = 0f;
        if (move != Vector2.zero)
        {
            targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                             currentCamera.transform.eulerAngles.y;
            var rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref _currentVelocity, 0.12f);

            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }
        
        var targetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;
        
        var verticalMotion = new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;
        
        _characterController.Move(targetDirection.normalized * (lerpSpeed * Time.deltaTime) + verticalMotion);
        
        
        
        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, move.magnitude);
        }
    }
    
    private void JumpAndGravity()
    {
        if (_isGround)
        {
            _fallTimeoutDelta = 0.15f;
            var jump = ReadValueFloat(_jumpActionIDs);
            
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            if (_verticalVelocity < -0.01f)
            {
                _verticalVelocity = -2f;
            }

            if (jump > 0f && _jumpTimeoutDelta <= 0.0f)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2 * gravity);

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            _jumpTimeoutDelta = 0.3f;
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else 
            {
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }
        }

        if (_verticalVelocity < 53f)
        {
            _verticalVelocity += gravity * Time.deltaTime;
        }
    }
    
    private void CameraRotation()
    {
        var look = ReadValueVector2(_lookActionIDs);

        if (look.sqrMagnitude >= 0.01f)
        {
            _cinemachineTargetYaw += look.x * sensitivity * Time.deltaTime;
            _cinemachineTargetPitch += -look.y * sensitivity * Time.deltaTime;
        }
        
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, -30f, 70f);

        cameraRoot.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);
        
        float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    private Vector2 ReadValueVector2(InputAction inputAction)
    {
        return inputAction.ReadValue<Vector2>();
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private float ReadValueFloat(InputAction inputAction)
    {
        return inputAction.ReadValue<float>();
    }

    /*private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);
        if (_isGround) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        var position = transform.position;
        Gizmos.DrawSphere(new Vector3(position.x, position.y + 0.14f, position.z), 0.28f);
    }*/
}