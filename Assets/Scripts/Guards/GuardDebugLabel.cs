using UnityEngine;

namespace Guards
{
    // Floats a text label above the guard showing its current goal,
    // so what the AI is "thinking" is visible at a glance while playing.
    [RequireComponent(typeof(GuardAgent))]
    public class GuardDebugLabel : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new(0f, 0.8f, 0f);
        [SerializeField] private int fontSize = 32;
        [SerializeField] private float characterSize = 0.08f;

        private GuardAgent _agent;
        private TextMesh _text;

        private void Awake()
        {
            _agent = GetComponent<GuardAgent>();

            var go = new GameObject("DebugLabel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = offset;

            _text = go.AddComponent<TextMesh>();
            _text.anchor = TextAnchor.LowerCenter;
            _text.alignment = TextAlignment.Center;
            _text.fontSize = fontSize;
            _text.characterSize = characterSize;
            _text.color = Color.white;

            go.GetComponent<MeshRenderer>().sortingOrder = 1000; // draw over the facility
        }

        private void LateUpdate()
        {
            if (_text) _text.text = _agent.CurrentGoalName;
        }
    }
}
