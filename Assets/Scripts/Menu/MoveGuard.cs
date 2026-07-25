using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menu
{
    // Swallows the next navigation event aimed at this selectable.
    [RequireComponent(typeof(Selectable))]
    public class MoveGuard : MonoBehaviour, IMoveHandler
    {
        private Selectable _selectable;
        private Navigation _saved;
        private bool _armed;

        private void Awake() => _selectable = GetComponent<Selectable>();

        public void Arm()
        {
            if (_armed) return;
            _armed = true;
            _saved = _selectable.navigation;

            var nav = _saved;
            nav.mode = Navigation.Mode.None;
            _selectable.navigation = nav;
        }

        public void OnMove(AxisEventData eventData)
        {
            if (!_armed) return;
            _armed = false;
            // Selectable.OnMove may run after this on the same object,
            // so restore once the event has finished dispatching rather than here.
            StartCoroutine(Restore());
        }

        private IEnumerator Restore()
        {
            yield return new WaitForEndOfFrame();
            _selectable.navigation = _saved;
        }
    }
}
