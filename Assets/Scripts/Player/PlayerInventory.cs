using System;
using System.Collections.Generic;
using Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private InputActionReference useAction;
        
        public event Action OnInventoryReset;
        
        public event Action<string> OnKeycardCollected;
        public event Action<string> OnDistractionCollected;
        
        private readonly HashSet<string> _keys = new();
        private readonly List<DistractionItem> _distractions = new();

        private void Awake() => OnInventoryReset?.Invoke();

        private void OnEnable()
        {
            useAction.action.performed += OnUse;
            useAction.action.Enable();
        }

        private void OnDisable()
        {
            useAction.action.performed -= OnUse;
            useAction.action.Disable();
        }
        
        public bool HasKey(string keyId) => keyId != null && _keys.Contains(keyId);
        
        public void CollectKey(string keyId)
        {
            _keys.Add(keyId);
            OnKeycardCollected?.Invoke(keyId);
        }
        
        public void CollectDistraction(DistractionItem item)
        {
            _distractions.Add(item);
            OnDistractionCollected?.Invoke(item.distractionId);
        }

        // Drops the most recently collected distraction at the player's current position.
        private void OnUse(InputAction.CallbackContext _)
        {
            Debug.Log("Pressed use");
            if (_distractions.Count == 0) return;
            var last = _distractions.Count - 1;
            var item = _distractions[last];
            _distractions.RemoveAt(last);
            item.Drop(transform.position);
        }
    }
}