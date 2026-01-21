using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cozy.Match.Puzzle.Components
{
    public struct PuzzleInputState
    {
        public bool IsDown;

        public Vector3 Position;
        public Vector3 Delta;
    }

    public interface IPuzzleInputReceiver
    {
        void OnInputDown(PuzzleInputState inputState);
        void OnInputUp(PuzzleInputState inputState);
        void OnInputMove(PuzzleInputState inputState);
        void OnInputClicked(PuzzleInputState inputState);
    }

    public class PuzzleInputComponent : MonoBehaviour
    {

        [SerializeField]
        private LayerMask inputLayerMask;

        private InputSystem_Actions inputActions;

        private HashSet<IPuzzleInputReceiver> inputReceivers = new();

        private Vector3? lastPosition;
        private bool wasDown;

        private void Awake()
        {
            inputActions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            inputActions.Player.Enable();
        }

        private void OnDisable()
        {
            inputActions.Player.Disable();
        }
        
        public void Subscribe(IPuzzleInputReceiver receiver)
        {
            if (receiver == null || inputReceivers.Contains(receiver))
            {
                return;
            }
            inputReceivers.Add(receiver);
        }

        public void Unsubscribe(IPuzzleInputReceiver receiver)
        {
            if (receiver == null)
            {
                return;
            }
            inputReceivers.Remove(receiver);
        }

        private void Update()
        {
            Vector2 position = inputActions.Player.Position.ReadValue<Vector2>();
            
            bool isDown = inputActions.Player.LeftClick.ReadValue<float>() > 0.5f;
            bool wasPreviouslyDown = wasDown; 
            
            wasDown = isDown;

            Ray ray = Camera.main.ScreenPointToRay(position);
            if (!Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, inputLayerMask))
            {
                return;
            }

            if (!lastPosition.HasValue)
            {
                lastPosition = hitInfo.point;
            }
            
            Vector3 worldPosition = hitInfo.point;
            Vector3 delta = worldPosition - lastPosition.Value;

            lastPosition = worldPosition;

            PuzzleInputState inputState = new()
            {
                IsDown = isDown,
                Position = worldPosition,
                Delta = delta
            };

            if (isDown && !wasPreviouslyDown)
            {
                NotifyDown(inputState);
                return;
            }

            if (wasPreviouslyDown && !isDown)
            {
                if (delta.magnitude < 0.01f)
                {
                    NotifyClicked(inputState);
                }
                else
                {
                    NotifyUp(inputState);
                }                
                return;
            }

            NotifyMove(inputState);
        }

        private void NotifyDown(PuzzleInputState inputState)
        {
            foreach (var receiver in inputReceivers)
            {
                receiver.OnInputDown(inputState);
            }
        }

        private void NotifyUp(PuzzleInputState inputState)
        {
            foreach (var receiver in inputReceivers)
            {
                receiver.OnInputUp(inputState);
            }
        }

        private void NotifyMove(PuzzleInputState inputState)
        {
            foreach (var receiver in inputReceivers)
            {
                receiver.OnInputMove(inputState);
            }
        }

        private void NotifyClicked(PuzzleInputState inputState)
        {
            foreach (var receiver in inputReceivers)
            {
                receiver.OnInputClicked(inputState);
            }
        }
    }
}