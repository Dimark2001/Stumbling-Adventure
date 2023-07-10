using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed;
    
    private PlayerInput _playerInput;

    private InputAction _moveAction;
    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _moveAction = _playerInput.actions["Move"];
    }

    private void Update()
    {
        var move = _moveAction.ReadValue<Vector2>();
        transform.Translate(new Vector3(move.x, 0, move.y) * speed);
    }
}