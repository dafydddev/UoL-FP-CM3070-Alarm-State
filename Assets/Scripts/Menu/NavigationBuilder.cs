using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    // Builds Explicit navigation links between selectables in hierarchy order instead of on-screen geometry.
    // Solves Unity's automatic ordering struggling with Vertical and Horizontal layouts groups.
    public class NavigationBuilder : MonoBehaviour
    {
        private readonly List<Selectable> _selectables = new();

        // Rebuild each time the panel is shown.
        private void OnEnable() => Build();

        [ContextMenu("Build Navigation")]
        public void Build()
        {
            // Use the inspector list when set, otherwise gather children in hierarchy order.
            var items = _selectables.Count > 0
                ? _selectables
                : new List<Selectable>(GetComponentsInChildren<Selectable>(false));

            // Nothing to link with fewer than two selectables.
            if (items.Count < 2) return;

            for (var i = 0; i < items.Count; i++)
            {
                var current = items[i];
                if (!current) continue;

                // Resolve the neighbours, wrapping at both ends so the navigation loops.
                var previous = i > 0 ? items[i - 1] : items[^1];
                var next = i < items.Count - 1 ? items[i + 1] : items[0];

                // Switch to Explicit
                var nav = current.navigation;
                nav.mode = Navigation.Mode.Explicit;

                // A slider is a case where horizontal movement changes its value, rather than paginate the UI.
                var slider = current as Slider;
                var horizontal = slider && slider.direction is Slider.Direction.LeftToRight or Slider.Direction.RightToLeft;
                var vertical = slider && !horizontal;
                nav.selectOnUp = vertical ? null : previous;
                nav.selectOnDown = vertical ? null : next;
                nav.selectOnLeft = horizontal ? null : previous;
                nav.selectOnRight = horizontal ? null : next;
                // Navigation is a struct, so assign the modified copy back to persist it.
                current.navigation = nav;
            }
        }
    }
}