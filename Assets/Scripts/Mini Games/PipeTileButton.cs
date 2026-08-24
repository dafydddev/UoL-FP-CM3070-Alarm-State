using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mini_Games
{
    // One cell of the game board's UI: a button showing its tile's pipe sprite at the tile's current rotation.
    // The minigame builds these and re-reads the board after every click; the button itself never touches puzzle logic.
    [RequireComponent(typeof(Button))]
    public class PipeTileButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
    {
        private PipeGameController _controller;

        [SerializeField] private Image pipeImage;

        public Button Button { get; private set; }

        // The tile this button renders, assigned by the minigame when the board is built.
        public PipeTile Tile { get; private set; }

        public void SetController(PipeGameController controller) => _controller = controller;

        private void Awake() => Button = GetComponent<Button>();

        public void Bind(PipeTile tile, Sprite sprite, Color colour)
        {
            Tile = tile;
            pipeImage.sprite = sprite;
            pipeImage.color = colour;
            Refresh();
        }

        // Spins the sprite to match the tile. Sprites are authored unrotated (see PipeTypeExtensions.Ends),
        // so each clockwise quarter turn is -90 around z.
        public void Refresh() => pipeImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f * Tile.Rotation);

        // Tints the pipe: the selection highlight and the activation surge both use this.
        public void Tint(Color colour) => pipeImage.color = colour;

        // Stops conflict between the use key (right mouse button) and selecting the tiles.
        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            _controller?.OnTileClicked(this);
        }

        // Hovering a tile selects it, the same as the move keys do.
        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) => _controller?.Select(Tile.Cell);
    }
}
