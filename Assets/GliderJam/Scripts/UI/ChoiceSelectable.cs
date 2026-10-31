using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GliderJam.UI
{
	// "< value >" selector: left / right (keys, d-pad, stick) or clicking the arrows cycles through options.
	public class ChoiceSelectable : Selectable, IPointerClickHandler, ISubmitHandler
	{
		public string[] options = new string[0];
		public int index;
		public System.Action<int> onChanged;
		public TextMeshProUGUI valueText;

		public void SetIndex(int i, bool notify)
		{
			if (options.Length == 0) return;
			index = (i % options.Length + options.Length) % options.Length;
			if (valueText != null) valueText.text = options[index];
			if (notify) onChanged?.Invoke(index);
		}

		public override void OnMove(AxisEventData e)
		{
			if (e.moveDir == MoveDirection.Left) { Step(-1); return; }
			if (e.moveDir == MoveDirection.Right) { Step(1); return; }
			base.OnMove(e);
		}

		public void Step(int d)
		{
			if (!IsInteractable()) return;
			Audio.AudioSynth.Play(Audio.AudioSynth.Click, 0.35f, d > 0 ? 1.1f : 0.95f);
			SetIndex(index + d, true);
		}

		public void OnPointerClick(PointerEventData e)
		{
			// Click on the left third steps back, elsewhere steps forward.
			RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out Vector2 local);
			Step(local.x < -((RectTransform)transform).rect.width / 6f ? -1 : 1);
		}

		public void OnSubmit(BaseEventData e)
		{
			Step(1);
		}
	}
}
