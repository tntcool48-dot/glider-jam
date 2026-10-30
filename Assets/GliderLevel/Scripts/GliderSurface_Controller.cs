using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace yajaber
{
	public class GliderSurface_Controller : Singleton<GliderSurface_Controller>
	{
		public enum ControlType
		{
			Mouse = 0,
			Keyboard = 1
		}

		[Header("Visuals")]
		public Transform aileronLeft;

		[Header("Visuals")]
		public Transform aileronRight;

		[Header("Visuals")]
		public Transform elevator;

		[Header("Visuals")]
		public Transform slatsLeft;

		[Header("Visuals")]
		public Transform slatsRight;

		[Header("Mode Selection")]
		public ControlType currentControlMode;

		[Header("Mouse Stats (Smoothed)")]
		public float mouseTurnSpeed = 5f;

		public float mousePitchSpeed = 5f;

		public float mouseBankSpeed = 2f;

		public float mouseBankAngle = 55f;

		[Tooltip("How fast the virtual joystick fills up.")]
		public float mouseSensitivity = 0.1f;

		[Tooltip("How fast the stick drifts back to center when you stop moving.")]
		public float mouseCenterSpeed = 0.5f;

		[Tooltip("How much smoothing to apply to the mouse input (Like Keyboard).")]
		public float mouseSmoothSpeed = 3f;

		public bool invertMouseY = true;

		[Header("Keyboard Stats")]
		public float keyboardTurnSpeed = 5f;

		public float keyboardPitchSpeed = 5f;

		public float keyboardBankSpeed = 2f;

		public float keyboardBankAngle = 55f;

		public float keyboardSmoothSpeed = 3f;

		private float _mouseVirtualYaw;

		private float _mouseVirtualPitch;

		private Rigidbody _rb;

		public float PitchInput { get; private set; }

		public float RollInput { get; private set; }

		public float YawInput { get; private set; }

		public float AileronAmount => (0f - RollInput) * 30f;

		public float ElevatorAmount => PitchInput * 30f;

		public float SlatAmount { get; set; }

		private void Start()
		{
			_rb = GetComponent<Rigidbody>();
			UpdateCursorState();
		}

		private void Update()
		{
			UpdateCursorState();
			GetInputs();
		}

		private void FixedUpdate()
		{
			PlaneRotations();
		}

		private void UpdateCursorState()
		{
			if (currentControlMode == ControlType.Mouse)
			{
				if (Cursor.lockState != CursorLockMode.Locked)
				{
					Cursor.lockState = CursorLockMode.Locked;
				}
				if (Cursor.visible)
				{
					Cursor.visible = false;
				}
			}
			else
			{
				if (Cursor.lockState != CursorLockMode.None)
				{
					Cursor.lockState = CursorLockMode.None;
				}
				if (!Cursor.visible)
				{
					Cursor.visible = true;
				}
			}
		}

		public virtual void GetInputs()
		{
			float deltaTime = Time.deltaTime;
			float target = 0f;
			float target2 = 0f;
			float num = 1f;
			if (currentControlMode == ControlType.Keyboard)
			{
				_mouseVirtualYaw = 0f;
				_mouseVirtualPitch = 0f;
				num = keyboardSmoothSpeed;
				Keyboard keyboard = Keyboard.current;
				if (keyboard != null)
				{
					if (keyboard.aKey.isPressed)
					{
						target2 = -1f;
					}
					if (keyboard.dKey.isPressed)
					{
						target2 = 1f;
					}
					if (keyboard.wKey.isPressed)
					{
						target = -1f;
					}
					if (keyboard.sKey.isPressed)
					{
						target = 1f;
					}
					if (keyboard.tabKey.isPressed)
					{
						SceneManager.LoadScene("mainUI");
					}
				}
			}
			else
			{
				num = mouseSmoothSpeed;
				float num2 = Input.GetAxis("Mouse X") * mouseSensitivity;
				float num3 = Input.GetAxis("Mouse Y") * mouseSensitivity;
				_mouseVirtualYaw += num2;
				if (invertMouseY)
				{
					_mouseVirtualPitch -= num3;
				}
				else
				{
					_mouseVirtualPitch += num3;
				}
				_mouseVirtualYaw = Mathf.Clamp(_mouseVirtualYaw, -1f, 1f);
				_mouseVirtualPitch = Mathf.Clamp(_mouseVirtualPitch, -1f, 1f);
				if (Mathf.Abs(num2) < 0.01f)
				{
					_mouseVirtualYaw = Mathf.MoveTowards(_mouseVirtualYaw, 0f, mouseCenterSpeed * deltaTime);
				}
				if (Mathf.Abs(num3) < 0.01f)
				{
					_mouseVirtualPitch = Mathf.MoveTowards(_mouseVirtualPitch, 0f, mouseCenterSpeed * deltaTime);
				}
				target2 = _mouseVirtualYaw;
				target = _mouseVirtualPitch;
			}
			YawInput = Mathf.MoveTowards(YawInput, target2, num * deltaTime);
			PitchInput = Mathf.MoveTowards(PitchInput, target, num * deltaTime);
			RollInput = Mathf.Clamp(YawInput, -1f, 1f);
			UpdateVisuals();
		}

		public void PlaneRotations()
		{
			float fixedDeltaTime = Time.fixedDeltaTime;
			float num;
			float num2;
			float num3;
			float num4;
			if (currentControlMode == ControlType.Mouse)
			{
				num = mouseTurnSpeed;
				num2 = mousePitchSpeed;
				num3 = mouseBankSpeed;
				num4 = mouseBankAngle;
			}
			else
			{
				num = keyboardTurnSpeed;
				num2 = keyboardPitchSpeed;
				num3 = keyboardBankSpeed;
				num4 = keyboardBankAngle;
			}
			Vector3 euler = Vector3.right * PitchInput * num2 * fixedDeltaTime;
			Vector3 euler2 = Vector3.up * YawInput * num * fixedDeltaTime;
			Quaternion rotation = _rb.rotation;
			Quaternion quaternion = Quaternion.Euler(euler2);
			Quaternion quaternion2 = Quaternion.Euler(euler);
			Quaternion quaternion3 = quaternion * rotation * quaternion2;
			float b = (0f - YawInput) * num4;
			Vector3 eulerAngles = quaternion3.eulerAngles;
			float num5 = eulerAngles.z;
			if (num5 > 180f)
			{
				num5 -= 360f;
			}
			float z = Mathf.Lerp(num5, b, num3 * fixedDeltaTime);
			_rb.MoveRotation(Quaternion.Euler(eulerAngles.x, eulerAngles.y, z));
		}

		private void UpdateVisuals()
		{
			if ((bool)aileronLeft)
			{
				aileronLeft.localRotation = Quaternion.Lerp(aileronLeft.localRotation, Quaternion.Euler(Vector3.forward * (RollInput * 30f - 90f) + Vector3.down * 90f), 10f * Time.deltaTime);
			}
			if ((bool)aileronRight)
			{
				aileronRight.localRotation = Quaternion.Lerp(aileronRight.localRotation, Quaternion.Euler(Vector3.back * (RollInput * 30f + 90f) + Vector3.down * 90f), 10f * Time.deltaTime);
			}
			if ((bool)elevator)
			{
				elevator.localRotation = Quaternion.Lerp(elevator.localRotation, Quaternion.Euler(Vector3.left * PitchInput * 30f), 10f * Time.deltaTime);
			}
		}
	}
}
