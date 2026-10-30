using UnityEngine;

namespace yajaber
{
	public class GliderCamera_Controller : MonoBehaviour
	{
		public Transform target;

		[Header("Camera Settings")]
		public Vector3 offset = new Vector3(0f, 3f, -10f);

		public float followSpeed = 20f;

		public float lookSpeed = 20f;

		[Header("Zoom Settings")]
		public float zoomSpeed = 50f;

		public float minZoom = 5f;

		public float maxZoom = 30f;

		private float _currentZoomDistance;

		private void Start()
		{
			if (target == null && Singleton<GliderSurface_Controller>.Instance != null)
			{
				target = Singleton<GliderSurface_Controller>.Instance.transform;
			}
			_currentZoomDistance = Mathf.Abs(offset.z);
		}

		private void Update()
		{
			float axis = Input.GetAxis("Mouse ScrollWheel");
			if (Mathf.Abs(axis) > 0.01f)
			{
				_currentZoomDistance -= axis * zoomSpeed * Time.deltaTime * 50f;
				_currentZoomDistance = Mathf.Clamp(_currentZoomDistance, minZoom, maxZoom);
			}
		}

		private void LateUpdate()
		{
			if (!(target == null))
			{
				Vector3 position = new Vector3(offset.x, offset.y, 0f - _currentZoomDistance);
				Vector3 b = target.TransformPoint(position);
				base.transform.position = Vector3.Lerp(base.transform.position, b, followSpeed * Time.deltaTime);
				Quaternion b2 = Quaternion.LookRotation(target.position + target.forward * 30f - base.transform.position);
				base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b2, lookSpeed * Time.deltaTime);
			}
		}
	}
}
