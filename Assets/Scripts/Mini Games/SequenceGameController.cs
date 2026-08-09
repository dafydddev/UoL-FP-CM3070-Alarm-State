using System.Collections;
using System.Collections.Generic;
using Entities.Objectives;
using Menu;
using Run;
using Settings;
using Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hacking
{
    // The key order hacking screen. Opens when the player uses an objective.
    // The objective completes once the order is entered without a slip. The game does not pause for it.
    public class SequenceGameController : MonoBehaviour
    {
        [Header("UI Game Objects")]
        [SerializeField] private MenuPanel panel;
        [SerializeField] private HorizontalLayoutGroup slotRow;
        [SerializeField] private GameObject slotPrefab;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference[] keyActions; // the order is drawn from these
        [SerializeField] private InputActionReference pauseAction;
        [SerializeField] private InputDeviceState deviceState;

        [Header("Slot Colours")]
        [SerializeField] private Color pendingColour = Color.grey;
        [SerializeField] private Color nextColour = Color.yellow;
        [SerializeField] private Color enteredColour = Color.cyan;
        [SerializeField] private Color wrongColour = Color.red;
        [SerializeField, Min(0f)] private float resetFlash = 0.25f; // seconds a wrong key holds before the order restarts
        [SerializeField, Min(0f)] private float winFlash = 0.15f;

        private RunContext _run;
        private Objective _objective; // the objective being hacked while the order is up
        private int[] _order; // indices into keyActions
        private readonly List<TMP_Text> _slots = new(); // the key label of each slot, the part that tints
        private int _entered; // how far in the player has got
        private bool _running;
        private bool _flashing; // a flash is playing; the order is read-only
        private int _openedFrame = -1; // the use key that opened us is still down this frame

        private void OnEnable()
        {
            Objective.HackRequested += Open;
            foreach (var action in keyActions) action.action.Enable();
            pauseAction.action.Enable();
            deviceState.InputTypeChanged += OnDeviceChanged;
        }

        private void OnDisable()
        {
            Objective.HackRequested -= Open;
            foreach (var action in keyActions) action.action.Disable();
            pauseAction.action.Disable();
            deviceState.InputTypeChanged -= OnDeviceChanged;
        }

        private void Update()
        {
            if (!_running) return;
            if (GameLock.Locked || pauseAction.action.WasPressedThisFrame())
            {
                Close();
                return;
            }

            if (_flashing || Time.frameCount == _openedFrame) return;
            ReadKeys();
        }

        // Called by the facility orchestrator on every time a level is generated.
        public void Prepare(RunContext run)
        {
            _run = run;
            if (_running) Close();
        }

        // Presents the objective's order.
        // The seed was stamped at spawn time, so reopening after an abort presents the same order again.
        private void Open(Objective objective)
        {
            if (_running || objective.Hack != HackKind.Sequence) return;
            _objective = objective;
            _running = true;
            _openedFrame = Time.frameCount;
            InputCapture.Acquire();

            _order = Draw(new System.Random(objective.hackSeed), _run.DifficultyProfile.sequenceLength);
            _entered = 0;

            panel.SetActive(true);
            BuildSlots();
        }

        // A run of keys, never repeating one back to back: a doubled key reads as a single long press.
        private int[] Draw(System.Random rng, int length)
        {
            var order = new int[Mathf.Max(2, length)];
            var previous = -1;
            for (var i = 0; i < order.Length; i++)
            {
                // Skip the key just placed.
                var pick = rng.Next(keyActions.Length - (previous < 0 ? 0 : 1));
                if (previous >= 0 && pick >= previous) pick++;
                order[i] = previous = pick;
            }

            return order;
        }

        // The expected key steps on; any other resets the order.
        private void ReadKeys()
        {
            for (var i = 0; i < keyActions.Length; i++)
            {
                if (!keyActions[i].action.WasPressedThisFrame()) continue;
                if (i == _order[_entered]) Advance();
                else StartCoroutine(FailRun());
                return;
            }
        }

        private void Advance()
        {
            _slots[_entered].color = enteredColour;
            _entered++;
            if (_entered >= _order.Length) StartCoroutine(CompleteRun());
            else _slots[_entered].color = nextColour;
        }

        private IEnumerator FailRun()
        {
            _flashing = true;
            foreach (var slot in _slots) slot.color = wrongColour;
            yield return new WaitForSeconds(resetFlash);
            _entered = 0;
            RefreshTints();
            _flashing = false;
        }

        private IEnumerator CompleteRun()
        {
            _flashing = true;
            yield return new WaitForSeconds(winFlash);
            _objective.CompleteHack();
            Close();
        }

        // Rebuilds one slot per key in the order.
        private void BuildSlots()
        {
            var root = slotRow.transform;
            for (var i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);

            _slots.Clear();
            foreach (var _ in _order) _slots.Add(Instantiate(slotPrefab, root).GetComponentInChildren<TMP_Text>());
            RefreshLabels();
            RefreshTints();
        }

        // Labels each slot for the device in use.
        private void RefreshLabels()
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                var action = keyActions[_order[i]].action;
                var index = BindingIndex(action);
                _slots[i].text = index < 0
                    ? ""
                    : action
                        .GetBindingDisplayString(index, InputBinding.DisplayStringOptions.DontIncludeInteractions)
                        .ToUpper();
            }
        }

        // Entered, waiting, and still to come.
        private void RefreshTints()
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                _slots[i].color = i < _entered ? enteredColour : i == _entered ? nextColour : pendingColour;
            }
        }

        private void OnDeviceChanged(InputDevice device)
        {
            if (_running) RefreshLabels();
        }

        private int BindingIndex(InputAction action)
        {
            var path = deviceState.CurrentDevice is Gamepad ? "<Gamepad>" : "<Keyboard>";
            return action.bindings.IndexOf(b => b.path.StartsWith(path));
        }

        // Backing out leaves the process incomplete. Using the objective again reopens the same order.
        private void Close()
        {
            if (!_running) return;
            StopAllCoroutines(); // a rebuild can close us mid-flash
            _flashing = false;
            _objective = null;
            _running = false;
            panel.SetActive(false);
            InputCapture.Release();
        }
    }
}
