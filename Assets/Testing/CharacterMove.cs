using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Testing
{
    public class CharacterMove : MonoBehaviour
    {
        [SerializeField]
        private CharacterController controller;
        [SerializeField]
        private float speed = 10;

        private InputAction moveAction;
        
        private void Start()
        {
            moveAction = InputSystem.actions.FindAction("Move");
        }

        // Update is called once per frame
        private void Update()
        {
            var move = moveAction.ReadValue<Vector2>();
            controller.Move(new Vector3(move.x, 0, move.y) * speed * Time.deltaTime);
        }
    }
}
