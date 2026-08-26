using System;
using Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Menu
{
    public class PauseMenu : MenuPanelController
    {
        // Raised when the player quits the run from the pause menu, so it can end through the results screen.
        public static event Action Quit;

        [Header("Pause Action")]
        [SerializeField] private InputActionReference pauseAction;

        [Header("Pause Menu Panels")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;

        private bool _isPaused;

        private void OnEnable()
        {
            pauseAction.action.performed += OnPausePressed;
            pauseAction.action.Enable();
            pauseButton.onClick.AddListener(OnPausePressed);
            resumeButton.onClick.AddListener(OnPausePressed);
            mainMenuButton.onClick.AddListener(QuitToMenu);
        }

        private void OnDisable()
        {
            pauseAction.action.performed -= OnPausePressed;
            pauseAction.action.Disable();
            pauseButton.onClick.RemoveListener(OnPausePressed);
            resumeButton.onClick.RemoveListener(OnPausePressed);
            mainMenuButton.onClick.RemoveListener(QuitToMenu);
        }

        private void OnPausePressed(InputAction.CallbackContext context) => OnPausePressed();

        private void OnPausePressed()
        {
            if (_isPaused)
            {
                Resume();
                return;
            }

            // Outside the tick loop, so check the lock ourselves.
            // Only guards pausing — while paused we hold the lock, so Locked is always true.
            if (GameLock.Locked) return;
            // A minigame holding the keys backs out on this one instead.
            if (InputCapture.Captured) return;
            Pause();
        }

        // The pause button hides itself, so the panel's resume button is the only way back.
        private void Pause()
        {
            _isPaused = true;
            GameLock.Acquire();
            ShowPanel(defaultPanel);
            pauseButton.gameObject.SetActive(false);
        }

        private void Resume()
        {
            _isPaused = false;
            GameLock.Release();
            HideAllMenuPanels();
            pauseButton.gameObject.SetActive(true);
        }

        // Hands the quit to the run orchestrator, which wipes to the results screen before the menu.
        // Our panel closes first so it doesn't sit over the results.
        private void QuitToMenu()
        {
            HideAllMenuPanels();
            Quit?.Invoke();
        }
    }
}