using System;
using System.Collections;
using System.Collections.Generic;
using Entities.Objectives;
using Menu;
using Run;
using Simulation;
using Tutorials;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Mini_Games
{
    // The pipe minigame. Opens when the player activates a primary objective.
    // The objective completes once the pipes are rotated into a circuit from the feed to the outlet.
    public class PipeGameController : MonoBehaviour
    {
        // Fires each time the player turns a tile, by click or by the use key.
        public static event Action TileRotated;

        [Header("UI Game Objects")] 
        [SerializeField] private MenuPanel panel;
        [SerializeField] private Button backdrop;
        [SerializeField] private GridLayoutGroup boardLayout;
        [SerializeField] private PipeTileButton tileButtonPrefab;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference upAction;
        [SerializeField] private InputActionReference downAction;
        [SerializeField] private InputActionReference leftAction;
        [SerializeField] private InputActionReference rightAction;
        [SerializeField] private InputActionReference useAction;
        [SerializeField] private InputActionReference pauseAction;

        [Header("Start and End Markers")]
        // Markers sat just outside the board showing where the circuit enters and leaves.
        [SerializeField] private RectTransform startMarker;
        [SerializeField] private Image startMarkerImage;
        [SerializeField] private RectTransform endMarker;
        [SerializeField] private Image endMarkerImage;

        [Header("Pipe Sprites")]
        // One sprite per pipe shape, authored in the unrotated orientations PipeTypeExtensions.Ends describes.
        [SerializeField] private Sprite capSprite;
        [SerializeField] private Sprite straightSprite;
        [SerializeField] private Sprite elbowSprite;
        [SerializeField] private Sprite teeSprite;
        [SerializeField] private Sprite crossSprite;

        [Header("Pipe Colours")] 
        [SerializeField] private Color pipeColour = Color.white;
        [SerializeField] private Color selectedColour = Color.yellow;
        [SerializeField] private Color poweredColour = Color.cyan;
        [SerializeField, Min(0f)] private float surgeStep = 0.08f; // seconds the activation surge spends per tile

        private RunContext _run;
        private Objective _objective; // the objective being attempted
        private PipeBoard _board;
        private PipeTileButton[,] _buttons;
        private Vector2Int _selected; // the tile the move keys are parked on
        private bool _attempting; // a minigame is on screen and holding the game lock
        private bool _surging; // the win animation is playing; the board is read-only

        private void OnEnable()
        {
            Objective.MiniGameRequested += Open;
            upAction.action.Enable();
            downAction.action.Enable();
            leftAction.action.Enable();
            rightAction.action.Enable();
            useAction.action.Enable();
            pauseAction.action.Enable();
            if (backdrop) backdrop.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            Objective.MiniGameRequested -= Open;
            upAction.action.Disable();
            downAction.action.Disable();
            leftAction.action.Disable();
            rightAction.action.Disable();
            useAction.action.Disable();
            pauseAction.action.Disable();
            if (backdrop) backdrop.onClick.RemoveListener(Close);
        }

        private void Update()
        {
            if (!_attempting || _surging) return;
            if (upAction.action.WasPressedThisFrame()) MoveSelection(Vector2Int.up);
            if (downAction.action.WasPressedThisFrame()) MoveSelection(Vector2Int.down);
            if (leftAction.action.WasPressedThisFrame()) MoveSelection(Vector2Int.left);
            if (rightAction.action.WasPressedThisFrame()) MoveSelection(Vector2Int.right);
            if (useAction.action.WasPressedThisFrame()) OnTileClicked(_buttons[_selected.x, _selected.y]);
            if (pauseAction.action.WasPressedThisFrame()) Close();
        }

        // Called by the facility orchestrator every time a level is generated.
        // Boards scale with the run's difficulty profile and level.
        public void Prepare(RunContext run)
        {
            _run = run;
            if (_attempting) Close();
        }

        // Presents the objective's puzzle.
        // The seed was stamped at spawn time, so reopening after an abort presents the same board again.
        private void Open(Objective objective)
        {
            if (_attempting) return; // already attempting
            if (objective.Game != MiniGameType.Pipes) return;
            Tutorial.ShowOnce(TutorialTopic.PipeMiniGame, () => Begin(objective));
        }

        private void Begin(Objective objective)
        {
            _objective = objective;
            _attempting = true;
            GameLock.Acquire();

            // One stream, drawn in this order, so a board is settled by its seed alone.
            var rng = new System.Random(objective.miniGameSeed);
            var profile = _run.DifficultyProfile;
            var size = profile.PipeGameBoardSize(_run.CurrentLevel, _run.TotalLevels, rng);
            var complexity = profile.PipeGameComplexity(_run.CurrentLevel, _run.TotalLevels, rng);
            var decoys = profile.PipeGameDecoyPathCount(_run.CurrentLevel, _run.TotalLevels, rng);
            var scramble = profile.PipeGameScrambleChance(_run.CurrentLevel, _run.TotalLevels, rng);
            _board = PipePuzzleGenerator.Generate(rng, size, complexity, decoys, scramble);

            startMarkerImage.color = pipeColour;
            endMarkerImage.color = pipeColour;

            panel.SetActive(true);
            if (backdrop) backdrop.gameObject.SetActive(true);
            BuildBoardUi();

            // Park the selection on the feed end of the circuit.
            Select(_board.StartCell);
        }

        // Rebuilds the grid of tile buttons for the board.
        // The cell size is recomputed, so any board dimensions fill the same panel space.
        private void BuildBoardUi()
        {
            var root = (RectTransform)boardLayout.transform;
            // The markers live under the same root as the tiles, so they are spared the clear-out.
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child == startMarker || child == endMarker) continue;
                Destroy(child.gameObject);
            }

            boardLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            boardLayout.constraintCount = _board.Width;
            var cellSize = Mathf.Min(root.rect.width / _board.Width, root.rect.height / _board.Height);
            boardLayout.cellSize = new Vector2(cellSize, cellSize);

            // The layout group fills left to right from the top, so walk rows from the top down.
            _buttons = new PipeTileButton[_board.Width, _board.Height];
            _selected = _board.StartCell;
            for (var y = _board.Height - 1; y >= 0; y--)
            {
                for (var x = 0; x < _board.Width; x++)
                {
                    var tile = _board.At(new Vector2Int(x, y));
                    var button = Instantiate(tileButtonPrefab, root);
                    button.Bind(tile, SpriteFor(tile.Type), pipeColour);
                    // The move keys drive the selection, so uGUI's own navigation would double-step it.
                    button.Button.navigation = new Navigation { mode = Navigation.Mode.None };
                    button.SetController(this);
                    _buttons[x, y] = button;
                }
            }

            PlaceMarker(startMarker, _board.StartCell, Vector2Int.left, cellSize);
            PlaceMarker(endMarker, _board.EndCell, Vector2Int.right, cellSize);
        }

        // Parks a feed marker on the outside edge of the given cell, sized to match the tiles.
        private void PlaceMarker(RectTransform marker, Vector2Int cell, Vector2Int outside, float cellSize)
        {
            if (!marker) return;
            var row = _board.Height - 1 - cell.y; // rows count down from the top of the panel
            marker.anchorMin = marker.anchorMax = new Vector2(0f, 1f);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.sizeDelta = new Vector2(cellSize, cellSize);
            marker.anchoredPosition = new Vector2(
                (cell.x + outside.x + 0.5f) * cellSize,
                -(row + 0.5f) * cellSize);
        }

        // Steps the selection to a neighbouring tile.
        private void MoveSelection(Vector2Int dir)
        {
            var next = _selected + dir;
            if (_board.At(next) == null) return;
            Select(next);
        }

        // Select the tile, tinting it. The move keys and the cursor both come through here.
        public void Select(Vector2Int cell)
        {
            if (_surging) return; // the surge owns the colours
            _buttons[_selected.x, _selected.y].Tint(pipeColour); // clear the old highlight
            _selected = cell;
            _buttons[cell.x, cell.y].Tint(selectedColour);
            _buttons[cell.x, cell.y].Button.Select();
        }

        // Rotates the clicked tile and re-validates the circuit.
        public void OnTileClicked(PipeTileButton button)
        {
            if (_surging) return;
            Select(button.Tile.Cell); // a mouse click also moves the selection
            button.Tile.Rotate();
            TileRotated?.Invoke();
            button.Refresh();
            if (_board.TryTraceCircuit(out var path)) StartCoroutine(ActivateCircuit(path));
        }

        // The electrical surge running the completed circuit. Tiles light up one by one from the feed.
        private IEnumerator ActivateCircuit(List<Vector2Int> path)
        {
            _surging = true;
            startMarkerImage.color = poweredColour;
            _buttons[_selected.x, _selected.y].Tint(pipeColour); // the surge owns the colours now
            foreach (var cell in path)
            {
                _buttons[cell.x, cell.y].Tint(poweredColour);
                yield return new WaitForSeconds(surgeStep);
            }

            endMarkerImage.color = poweredColour;
            _objective.CompleteMiniGame();
            Close();
        }

        // Backing out leaves the process incomplete. Using the objective again reopens the same puzzle.
        private void Close()
        {
            // several paths can fire on one frame, and the shared backdrop calls this for both screens
            if (!_attempting) return;
            StopAllCoroutines(); // a rebuild can close us mid-surge
            _surging = false;
            _objective = null;
            _attempting = false;
            panel.SetActive(false);
            if (backdrop) backdrop.gameObject.SetActive(false);
            GameLock.Release();
        }

        private Sprite SpriteFor(PipeType type) => type switch
        {
            PipeType.Cap => capSprite,
            PipeType.Straight => straightSprite,
            PipeType.Elbow => elbowSprite,
            PipeType.Tee => teeSprite,
            _ => crossSprite,
        };
    }
}