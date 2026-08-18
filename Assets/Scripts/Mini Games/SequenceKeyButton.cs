using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mini_Games
{
    // One clickable key of the pointer variant's row. The minigame owns the tinting.
    [RequireComponent(typeof(Image))]
    public class SequenceKeyButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private SequenceGameController _controller;
        private int _step;

        public void Bind(SequenceGameController controller, int step)
        {
            _controller = controller;
            _step = step;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) => _controller?.OnKeyClicked(_step);

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) => _controller?.OnKeyHovered(_step, true);

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) => _controller?.OnKeyHovered(_step, false);
    }
}
