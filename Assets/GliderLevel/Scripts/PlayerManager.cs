using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
	[Header("Death Settings")]
	public GameObject explosionPrefab;

	public Rigidbody playerRb;

	public AudioClip deathSound;

	public AudioSource sfxAudioSource;

	[Header("Looping Sounds")]
	public AudioSource engineSource;

	public AudioSource windSource;

	[Header("Win Settings")]
	public GameObject winPanel;

	private bool _isDead;

	private void OnCollisionEnter(Collision collision)
	{
		if (_isDead)
		{
			return;
		}
		if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.name == "Terrain")
		{
			_isDead = true;
			StartCoroutine(DieSequence());
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!_isDead && other.CompareTag("Finish"))
		{
			WinGame();
			Debug.Log("You Win!");
		}
	}

	private IEnumerator DieSequence()
	{
		if (engineSource != null)
		{
			engineSource.Stop();
		}
		if (windSource != null)
		{
			windSource.Stop();
		}
		if (deathSound != null && sfxAudioSource != null)
		{
			sfxAudioSource.PlayOneShot(deathSound);
		}
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		playerRb.isKinematic = true;
		if (explosionPrefab != null)
		{
			Object.Instantiate(explosionPrefab, base.transform.position, Quaternion.identity);
		}
		yield return new WaitForSeconds(1f);
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}

	private void WinGame()
	{
		if (engineSource != null)
		{
			engineSource.Stop();
		}
		if (windSource != null)
		{
			windSource.Stop();
		}
		SceneManager.LoadScene("end");
	}
}
