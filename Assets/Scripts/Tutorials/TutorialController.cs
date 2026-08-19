using System;
using System.Collections.Generic;
using Menu;
using Settings;
using Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tutorials
{
    // Holds the game lock while a tutorial is being shown, like the pause menu.
    // Requests only queue: the queue drains on the first frame nothing else holds the lock.
    public class TutorialController : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private MenuPanel panel;
        [SerializeField] private TutorialCatalogue catalogue;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Image image;

        [Header("Dismissal")]
        [SerializeField] private Button continueButton;

        private readonly Queue<(TutorialEntry entry, Action onDismissed)> _queue = new();

        private bool _showing;
        private int _openedFrame = -1; // the key that opened the panel is still down this frame
        private Action _onDismissed;

        private void OnEnable()
        {
            Tutorial.Handler = Enqueue;
            AlarmState.ActiveChanged += OnAlarmChanged;
            if (continueButton) continueButton.onClick.AddListener(Dismiss);
        }

        private void OnDisable()
        {
            Tutorial.Handler = null;
            AlarmState.ActiveChanged -= OnAlarmChanged;
            if (continueButton) continueButton.onClick.RemoveListener(Dismiss);

            if (!_showing) return; // torn down mid-tutorial
            _showing = false;
            GameLock.Release();
        }

        private static void OnAlarmChanged(bool active)
        {
            if (active) Tutorial.ShowOnce(TutorialTopic.AlarmRaised);
        }

        private bool Enqueue(TutorialTopic topic, Action onDismissed)
        {
            var entry = catalogue ? catalogue.Find(topic) : null;
            if (!entry || !TutorialSettings.TryMarkSeen(topic)) return false;
            _queue.Enqueue((entry, onDismissed));
            return true;
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

            if (_queue.Count > 0) Present(_queue.Dequeue());
        }

        private void Present((TutorialEntry entry, Action onDismissed) request)
        {
            _onDismissed = request.onDismissed;
            _showing = true;
            _openedFrame = Time.frameCount;

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
    }
}
