using System;
using System.Collections.Generic;
using Guards;
using Menu;
using Player;
using Settings;
using Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tutorials
{
    // Holds the game lock while a tutorial is being shown, like the pause menu.
    // Requests only queue: the queue drains on the first frame nothing else holds the lock and the step has finished.
    public class TutorialController : MonoBehaviour
    {
        [Header("Content")] [SerializeField] private MenuPanel panel;
        [SerializeField] private TutorialCatalogue catalogue;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Image image;

        [Header("Dismissal")] [SerializeField] private Button continueButton;

        [Header("Timing")] [SerializeField] private SimulationClock clock;

        private readonly Queue<(TutorialEntry entry, Action onDismissed)> _queue = new();

        private bool _showing;
        private int _openedFrame = -1; // the key that opened the panel is still down this frame
        private Action _onDismissed;

        private float _alpha; // the clock's alpha
        private bool _stepped; // a tick has landed since the request was made

        private void OnEnable()
        {
            Tutorial.Handler = Enqueue;
            AlarmState.ActiveChanged += OnAlarmChanged;
            GuardAgent.PlayerSpotted += OnPlayerSpotted;
            PlayerHiding.OnHiddenChanged += OnHiddenChanged;
            if (continueButton) continueButton.onClick.AddListener(Dismiss);
        }

        private void OnDisable()
        {
            Tutorial.Handler = null;
            AlarmState.ActiveChanged -= OnAlarmChanged;
            GuardAgent.PlayerSpotted -= OnPlayerSpotted;
            PlayerHiding.OnHiddenChanged -= OnHiddenChanged;
            if (continueButton) continueButton.onClick.RemoveListener(Dismiss);
            if (!_showing) return; // torn down mid-tutorial
            _showing = false;
            GameLock.Release();
        }

        private static void OnAlarmChanged(bool active)
        {
            if (active) Tutorial.ShowOnce(TutorialTopic.AlarmRaised);
        }

        private static void OnPlayerSpotted() => Tutorial.ShowOnce(TutorialTopic.PlayerSpotted);

        private bool Enqueue(TutorialTopic topic, Action onDismissed)
        {
            var entry = catalogue ? catalogue.Find(topic) : null;
            if (!entry || !TutorialSettings.TryMarkSeen(topic)) return false;
            _queue.Enqueue((entry, onDismissed));
            WaitOutStep();
            return true;
        }

        // Requests are raised inside the tick, so the step they interrupt still has to finish drawing.
        private void WaitOutStep()
        {
            _alpha = clock ? clock.Alpha : 0f;
            _stepped = false;
        }

        // The lock stays held for the rest of the frame it was dropped on, so a follow-up waits a frame.
        private void LateUpdate()
        {
            if (_showing || GameLock.Locked) return;

            if (_onDismissed != null)
            {
                var followUp = _onDismissed;
                _onDismissed = null;
                followUp();
                return;
            }

            if (_queue.Count == 0) return; // nothing waiting: no clock to watch

            var alpha = clock ? clock.Alpha : 0f;
            if (alpha < _alpha) _stepped = true; // alpha only falls when a tick lands
            _alpha = alpha;

            if (!clock || _stepped) Present(_queue.Dequeue()); // an unwired clock shows straight away rather than never
        }

        private void Present((TutorialEntry entry, Action onDismissed) request)
        {
            _onDismissed = request.onDismissed;
            _showing = true;
            _openedFrame = Time.frameCount;
            WaitOutStep(); // so anything still queued waits out its own step rather than inheriting this one

            titleText.text = request.entry.title;
            bodyText.text = request.entry.body;
            if (image)
            {
                image.sprite = request.entry.image;
                image.gameObject.SetActive(request.entry.image);
            }

            GameLock.Acquire();
            panel.SetActive(true);
        }

        private void Dismiss()
        {
            if (!_showing || Time.frameCount == _openedFrame) return;
            _showing = false;
            panel.SetActive(false);
            GameLock.Release();
        }

        private static void OnHiddenChanged(bool hidden)
        {
            if (hidden) Tutorial.ShowOnce(TutorialTopic.CoverEntered);
        }
    }
}