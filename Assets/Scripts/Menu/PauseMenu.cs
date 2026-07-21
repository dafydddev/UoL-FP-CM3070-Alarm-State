using Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Menu
{
    public class PauseMenu : MenuPanelController
    {
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
            mainMenuButton.onClick.AddListener(MainMenu);
        }

        private void OnDisable()
        {
            pauseAction.action.performed -= OnPausePressed;
            pauseAction.action.Disable();
            pauseButton.onClick.RemoveListener(OnPausePressed);
            resumeButton.onClick.RemoveListener(OnPausePressed);
            mainMenuButton.onClick.RemoveListener(MainMenu);
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
            Pause();
        }

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

        private static void MainMenu()
        {
            SceneManager.LoadScene("Main Menu");
        }
    }
}