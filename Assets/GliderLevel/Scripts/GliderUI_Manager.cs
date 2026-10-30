using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace yajaber
{
	public class GliderUI_Manager : MonoBehaviour
	{
		[Header("Speed Data")]
		public TextMeshProUGUI Airspeed;

		public TextMeshProUGUI Ias;

		public TextMeshProUGUI Tas;

		[Header("Attitude Data")]
		public TextMeshProUGUI Roll;

		public TextMeshProUGUI Pitch;

		public TextMeshProUGUI Slope;

		public TextMeshProUGUI AOA;

		[Header("Forces")]
		public TextMeshProUGUI Drag;

		public TextMeshProUGUI Lift;

		[Header("Environment")]
		public TextMeshProUGUI Air;

		public TextMeshProUGUI Wing;

		public TextMeshProUGUI Temp;

		[Header("Menu & Toggle")]
		public GameObject HUD_Prefab;

		private PhysicOnGlider Glider;

		private const float WingArea = 28f;

		private void Start()
		{
			if (Singleton<GliderSurface_Controller>.Instance != null)
			{
				Glider = Singleton<GliderSurface_Controller>.Instance.GetComponent<PhysicOnGlider>();
			}
			Time.timeScale = 1f;
			if (HUD_Prefab != null)
			{
				HUD_Prefab.SetActive(value: false);
			}
		}

		private void Update()
		{
			if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && HUD_Prefab != null)
			{
				HUD_Prefab.SetActive(!HUD_Prefab.activeSelf);
			}
			if (!(Glider == null))
			{
				UpdateUI(Glider);
			}
		}

		private void UpdateUI(PhysicOnGlider glider)
		{
			float y = glider.transform.position.y;
			float magnitude = glider._rb.linearVelocity.magnitude;
			float value = 15f - y / 1000f * 6.5f;
			Temp.text = FormatText(value) + " °C";
			float num = 1.225f * Mathf.Pow(1f - 2.25577E-05f * y, 4.25588f);
			Air.text = FormatText(num) + " kg/m³";
			Wing.text = FormatText(28f) + " m²";
			Airspeed.text = FormatText(glider.constantSpeed) + " m/s";
			Ias.text = FormatText(magnitude) + " m/s";
			float value2 = magnitude / Mathf.Sqrt(num / 1.225f);
			Tas.text = FormatText(value2) + " m/s";
			float num2 = NormalizeAngle(glider.transform.eulerAngles.x);
			Pitch.text = FormatText(0f - num2) + "°";
			float value3 = NormalizeAngle(glider.transform.eulerAngles.z);
			Roll.text = FormatText(value3) + "°";
			float num3 = 0f;
			if (Singleton<GliderSurface_Controller>.Instance != null)
			{
				num3 = Singleton<GliderSurface_Controller>.Instance.PitchInput * -15f;
			}
			AOA.text = FormatText(num3) + "°";
			float value4 = num2 - num3;
			Slope.text = FormatText(value4) + "°";
			float num4 = 0.5f * num * (magnitude * magnitude);
			float num5 = Mathf.Clamp(Mathf.Abs(num3) * 0.11f, 0.2f, 1.6f) * num4 * 28f;
			Lift.text = FormatText(num5) + " N";
			float value5 = (0.02f + Mathf.Pow(num5 / (num4 * 28f), 2f) / (MathF.PI * 17f)) * num4 * 28f;
			Drag.text = FormatText(value5) + " N";
		}

		private float NormalizeAngle(float angle)
		{
			if (angle > 180f)
			{
				angle -= 360f;
			}
			return angle;
		}

		private string FormatText(float value)
		{
			return $"{value:#.0}";
		}

		public void Restart()
		{
			Time.timeScale = 1f;
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}
	}
}
