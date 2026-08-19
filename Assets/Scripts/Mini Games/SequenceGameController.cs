using System.Collections;
using System.Collections.Generic;
using Entities.Objectives;
using Generation;
using Menu;
using Run;
using Settings;
using Simulation;
using TMPro;
using Tutorials;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Mini_Games
{
    // The sequence minigame. Opens when the player activates a secondary objective.
    // The objective completes once the order is entered without a mistake. 
    public class SequenceGameController : MonoBehaviour
    {
        [Header("UI Game Objects")]
        [SerializeField] private MenuPanel panel;
        [SerializeField] private Button backdrop;
        [SerializeField] private HorizontalLayoutGroup slotRow;
        [SerializeField] private GameObject slotPrefab;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference[] keyActions; // the order is drawn from these
        [SerializeField] private InputActionReference pauseAction;
        [SerializeField] private InputActionReference useAction;
        [SerializeField] private InputDeviceState deviceState;

        [Header("Slot Colours")]
        [SerializeField] private Color pendingColour = Color.grey;
        [SerializeField] private Color nextColour = Color.yellow;
        [SerializeField] private Color enteredColour = Color.cyan;
        [SerializeField] private Color wrongColour = Color.red;
        [SerializeField] private Color hoverColour = Color.white; // the clicked key under the cursor
        [SerializeField, Min(0f)] private float resetFlash = 0.25f; // seconds a wrong key holds before the order restarts
        [SerializeField, Min(0f)] private float winFlash = 0.15f;

        private RunContext _run;
        private Objective _objective; // the objective being attempted
        private int[] _order; // typed variant: indices into keyActions
        private int[] _steps; // clicked variant: the place in the order each row position fills
        private bool _pointer; // opened with the mouse, so the row is clicked rather than typed

        // The key label of each slot, the part that tints. Held in entry order, which the scramble undoes.
        private readonly List<TMP_Text> _slots = new();
        private int _entered; // how far in the player has got
        private bool _running;
        private bool _flashing; // a flash is playing; the order is read-only
        private int _openedFrame = -1; // the use key that opened us is still down this frame

        private void OnEnable()
        {
            Objective.MiniGameRequested += Open;
            foreach (var action in keyActions) action.action.Enable();
            pauseAction.action.Enable();
            deviceState.InputTypeChanged += OnDeviceChanged;
            if (backdrop) backdrop.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            Objective.MiniGameRequested -= Open;
            foreach (var action in keyActions) action.action.Disable();
            pauseAction.action.Disable();
            deviceState.InputTypeChanged -= OnDeviceChanged;
            if (backdrop) backdrop.onClick.RemoveListener(Close);
        }

        private void Update()
        {
            if (!_running) return;
            if (GameLock.Locked || pauseAction.action.WasPressedThisFrame())
            {
                Close();
                return;
            }

            if (_pointer || _flashing || Time.frameCount == _openedFrame) return;
            ReadKeys();
        }

        // Called by the facility orchestrator on every time a level is generated.
        public void Prepare(RunContext run)
        {
            _run = run;
            if (_running) Close();
        }

        // Presents the objective's order in the variant the control that used it calls for.
        // The seed was stamped at spawn time, so each variant reopens on the order it always had.
        private void Open(Objective objective)
        {
            if (_running || objective.Game != MiniGameType.Sequence) return;
            // The control that fired the use, so that the right input options can be shown
            var pointer = useAction && useAction.action.activeControl?.device is Mouse;
            Tutorial.ShowOnce(TutorialTopic.SequenceMiniGame, () => Begin(objective, pointer));
        }

        private void Begin(Objective objective, bool pointer)
        {
            _objective = objective;
            _running = true;
            _openedFrame = Time.frameCount;
            _pointer = pointer;
            InputCapture.Acquire();

            var rng = new System.Random(objective.miniGameSeed);
            var length = Mathf.Max(2, _run.DifficultyProfile.sequenceLength);
            if (_pointer) _steps = Scramble(rng, length);
            else _order = Draw(rng, length);
            _entered = 0;

            panel.SetActive(true);
            if (backdrop) backdrop.gameObject.SetActive(true);
            BuildSlots();
        }

        // The clicked row: every place in the order once, dealt with scrambled positions.
        private static int[] Scramble(System.Random rng, int length)
        {
            var steps = new int[length];
            for (var i = 0; i < length; i++) steps[i] = i;
            Shuffle.InPlace(steps, rng);
            return steps;
        }

        // A run of keys, never repeating one back to back: a doubled key reads as a single long press.
        private int[] Draw(System.Random rng, int length)
        {
            var order = new int[length];
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
                Step(i == _order[_entered]);
                return;
            }
        }

        // The key whose place is next steps on; any other resets the order.
        public void OnKeyClicked(int step) => Step(step == _entered);

        // Marks the key under the cursor. Only what is still to come lights up.
        public void OnKeyHovered(int step, bool inside)
        {
            if (!_pointer || !_running || _flashing || step < _entered) return;
            _slots[step].color = inside ? hoverColour : pendingColour;
        }

        // Both variants land here. A click arrives outside Update, so it repeats the guards.
        private void Step(bool correct)
        {
            if (!_running || _flashing || GameLock.Locked || Time.frameCount == _openedFrame) return;
            if (correct) Advance();
            else StartCoroutine(FailRun());
        }

        private void Advance()
        {
            _slots[_entered].color = enteredColour;
            _entered++;
            if (_entered >= _slots.Count) StartCoroutine(CompleteRun());
            else if (!_pointer) _slots[_entered].color = nextColour; // clicked, finding the next key is the game
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
            _objective.CompleteMiniGame();
            Close();
        }

        // Rebuilds one slot per key, left to right. A clicked row fills _slots through its scramble.
        private void BuildSlots()
        {
            var root = slotRow.transform;
            for (var i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);

            var length = _pointer ? _steps.Length : _order.Length;
            _slots.Clear();
            for (var i = 0; i < length; i++) _slots.Add(null);

            for (var position = 0; position < length; position++)
            {
                var slot = Instantiate(slotPrefab, root);
                var step = _pointer ? _steps[position] : position;
                _slots[step] = slot.GetComponentInChildren<TMP_Text>();
                if (_pointer) slot.AddComponent<SequenceKeyButton>().Bind(this, step);
            }

            RefreshLabels();
            RefreshTints();
        }

        // Labels each slot with the key to press, or with the place it fills when clicked.
        private void RefreshLabels()
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_pointer)
                {
                    _slots[i].text = (i + 1).ToString();
                    continue;
                }

                var action = keyActions[_order[i]].action;
                var index = BindingIndex(action);
                _slots[i].text = index < 0
                    ? ""
                    : action
                        .GetBindingDisplayString(index, InputBinding.DisplayStringOptions.DontIncludeInteractions)
                        .ToUpper();
            }
        }

        // Entered, waiting, and still to come. A clicked row never marks what is next.
        private void RefreshTints()
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                _slots[i].color = i < _entered ? enteredColour
                    : i == _entered && !_pointer ? nextColour
                    : pendingColour;
            }
        }

        // Only the typed variant labels off the device; a clicked row is numbered either way.
        private void OnDeviceChanged(InputDevice device)
        {
            if (_running && !_pointer) RefreshLabels();
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
            if (backdrop) backdrop.gameObject.SetActive(false);
            InputCapture.Release();
        }
    }
}
