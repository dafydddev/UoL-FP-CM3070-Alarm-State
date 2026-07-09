using UnityEngine;

namespace Menu
{
    public class MenuController : MonoBehaviour
    {
        // The menu panels know to the scene
        [SerializeField] private MenuPanel[] menuPanels;

        // The default panel to show on scene load
        [SerializeField] private MenuPanel defaultPanel;

        // The current panel being displayed
        private MenuPanel _currentPanel;
        
        private void Awake() => UpdateMenu(defaultPanel);

        private void UpdateMenu(MenuPanel panel)
        {
            // Early exit when there are no menu panels
            if (menuPanels == null || menuPanels.Length == 0) return;
            // Hide all the menu panels know to the script
            ShowPanel(panel);
        }


        public void ShowPanel(MenuPanel panel)
        {
            // Early exit when there is no panel, or it is already the current panel
            if (!panel || panel == _currentPanel) return;
            // Hide all the other panels
            HideAll();
            // Set the panel to be active
            panel.SetActive(true);
            // Set the panel as the active one
            _currentPanel = panel;
        }

        private void HideAll()
        {
            // Early exit when there are no menu panels
            if (menuPanels == null || menuPanels.Length == 0) return;
            // Loop through the panels and set them all to inactive
            foreach (var panel in menuPanels) panel.SetActive(false);
        }
    }
}