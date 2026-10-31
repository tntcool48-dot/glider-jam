using GliderJam.Flight;
using UnityEngine;

namespace GliderJam.FX
{
	// Cinematic crash: the glider breaks into its real parts as physical debris, with a fireball,
	// rolling lit smoke, sparks, a light flash and camera shake.
	public static class CrashFX
	{
		public static void Spawn(FxLibrary fx, Vector3 point, Vector3 velocity, Transform visual, FollowCamera[] cams)
		{
			var root = new GameObject("CrashFX");
			root.transform.position = point;
			Object.Destroy(root, 14f);

			SpawnDebris(fx, root.transform, visual, velocity);
			// Initial flash, then the rolling fireball, afterburn, and smoke that only thickens once the fire has bloomed.
			Burst(root.transform, fx.fire, "Flash", 3, new Vector2(30f, 45f), new Vector2(0.18f, 0.28f), new Vector2(0f, 2f), 0.5f,
				FlashGradient(), 0f, ParticleSystemRenderMode.Billboard, 0f);
			Burst(root.transform, fx.fire, "Fireball", 44, new Vector2(8f, 20f), new Vector2(0.7f, 1.5f), new Vector2(8f, 34f), 3f,
				FireGradient(), 0f, ParticleSystemRenderMode.Billboard, 0f);
			Burst(root.transform, fx.fire, "Afterburn", 18, new Vector2(5f, 11f), new Vector2(0.9f, 2f), new Vector2(2f, 12f), 4f,
				FireGradient(), 0.3f, ParticleSystemRenderMode.Billboard, -0.02f);
			Burst(root.transform, fx.smoke, "Smoke", 26, new Vector2(12f, 26f), new Vector2(4f, 8f), new Vector2(3f, 14f), 5f,
				SmokeGradient(), 0.35f, ParticleSystemRenderMode.Billboard, -0.06f);
			Burst(root.transform, fx.spark, "Sparks", 90, new Vector2(0.25f, 0.6f), new Vector2(0.6f, 1.8f), new Vector2(25f, 75f), 1.5f,
				SparkGradient(), 0f, ParticleSystemRenderMode.Stretch, 1.1f);

			var lightGo = new GameObject("Flash");
			lightGo.transform.SetParent(root.transform, false);
			var light = lightGo.AddComponent<Light>();
			light.type = LightType.Point;
			light.color = new Color(1f, 0.55f, 0.2f);
			light.range = 450f;
			light.lightUnit = UnityEngine.Rendering.LightUnit.Lumen;
			light.intensity = 0f;
			lightGo.AddComponent<FlashFade>().peak = 6e7f;

			foreach (FollowCamera cam in cams)
			{
				if (cam == null) continue;
				float d = Vector3.Distance(cam.transform.position, point);
				cam.AddShake(Mathf.Lerp(2.4f, 0f, d / 700f));
			}
			if (fx.explosionClip != null)
			{
				var a = root.AddComponent<AudioSource>();
				a.clip = fx.explosionClip;
				a.spatialBlend = 0.7f;
				a.minDistance = 60f;
				a.maxDistance = 2000f;
				a.volume = 1f;
				a.Play();
			}
		}

		private static void SpawnDebris(FxLibrary fx, Transform root, Transform visual, Vector3 velocity)
		{
			int k = 0;
			foreach (MeshRenderer mr in visual.GetComponentsInChildren<MeshRenderer>(true))
			{
				var mf = mr.GetComponent<MeshFilter>();
				if (mf == null || mf.sharedMesh == null || mr.name.StartsWith("Nav") || mr.name == "Strobe" || mr.name == "Canopy") continue;
				var go = new GameObject("Debris_" + mr.name);
				go.transform.SetPositionAndRotation(mr.transform.position, mr.transform.rotation);
				go.transform.localScale = mr.transform.lossyScale;
				go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
				go.AddComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials;
				var box = go.AddComponent<BoxCollider>();
				box.size = Vector3.Max(mf.sharedMesh.bounds.size, Vector3.one * 0.2f);
				box.center = mf.sharedMesh.bounds.center;
				var rb = go.AddComponent<Rigidbody>();
				rb.mass = 40f;
				rb.linearDamping = 0.25f;
				rb.angularDamping = 0.4f;
				rb.linearVelocity = velocity * 0.3f + Random.insideUnitSphere * 26f + Vector3.up * 14f;
				rb.angularVelocity = Random.insideUnitSphere * 8f;
				Object.Destroy(go, 12f + Random.value * 3f);
				// The two largest pieces trail fire and smoke.
				if (k < 2 && mf.sharedMesh.bounds.size.magnitude > 3f)
				{
					k++;
					var trail = Burst(go.transform, fx.smoke, "DebrisSmoke", 0, new Vector2(3f, 6f), new Vector2(1.5f, 3f), new Vector2(0f, 1f), 0.5f, SmokeGradient(), 0f, ParticleSystemRenderMode.Billboard, -0.03f);
					trail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
					var em = trail.emission;
					em.rateOverTime = 28f;
					var main = trail.main;
					main.loop = true;
					main.duration = 4f;
					trail.Play();
				}
			}
		}

		private static ParticleSystem Burst(Transform parent, Material mat, string name, int count, Vector2 size, Vector2 life, Vector2 speed,
			float radius, Gradient color, float delay, ParticleSystemRenderMode mode, float gravity)
		{
			var go = new GameObject(name);
			go.transform.SetParent(parent, false);
			var ps = go.AddComponent<ParticleSystem>();
			ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			var main = ps.main;
			main.duration = 1f;
			main.loop = false;
			main.startDelay = delay;
			main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
			main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
			main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
			main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
			main.gravityModifier = gravity;
			main.maxParticles = Mathf.Max(count, 200);
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			var em = ps.emission;
			em.rateOverTime = 0f;
			if (count > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
			var shape = ps.shape;
			shape.shapeType = ParticleSystemShapeType.Sphere;
			shape.radius = radius;
			var col = ps.colorOverLifetime;
			col.enabled = true;
			col.color = color;
			var sol = ps.sizeOverLifetime;
			sol.enabled = true;
			sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.25f, 0.9f), new Keyframe(1f, 1.35f)));
			var limit = ps.limitVelocityOverLifetime;
			limit.enabled = mode != ParticleSystemRenderMode.Stretch;
			limit.limit = 4f;
			limit.dampen = 0.12f;
			var rot = ps.rotationOverLifetime;
			rot.enabled = true;
			rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
			var r = go.GetComponent<ParticleSystemRenderer>();
			r.sharedMaterial = mat;
			r.renderMode = mode;
			if (mode == ParticleSystemRenderMode.Stretch)
			{
				r.velocityScale = 0.04f;
				r.lengthScale = 1f;
			}
			r.sortMode = ParticleSystemSortMode.Distance;
			r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			ps.Play();
			return ps;
		}

		private static Gradient FireGradient()
		{
			var g = new Gradient();
			g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0f), new GradientColorKey(new Color(1f, 0.62f, 0.18f), 0.22f), new GradientColorKey(new Color(0.85f, 0.22f, 0.05f), 0.55f), new GradientColorKey(new Color(0.15f, 0.08f, 0.05f), 1f) },
				new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.06f), new GradientAlphaKey(0.85f, 0.5f), new GradientAlphaKey(0f, 1f) });
			return g;
		}

		private static Gradient FlashGradient()
		{
			var g = new Gradient();
			g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.7f, 0.35f), 1f) },
				new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
			return g;
		}

		private static Gradient SmokeGradient()
		{
			var g = new Gradient();
			g.SetKeys(new[] { new GradientColorKey(new Color(0.12f, 0.1f, 0.09f), 0f), new GradientColorKey(new Color(0.32f, 0.3f, 0.29f), 0.5f), new GradientColorKey(new Color(0.55f, 0.53f, 0.52f), 1f) },
				new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.12f), new GradientAlphaKey(0.5f, 0.6f), new GradientAlphaKey(0f, 1f) });
			return g;
		}

		private static Gradient SparkGradient()
		{
			var g = new Gradient();
			g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 0.6f), 0f), new GradientColorKey(new Color(1f, 0.45f, 0.1f), 1f) },
				new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
			return g;
		}

		private class FlashFade : MonoBehaviour
		{
			public float peak;
			private float _t;
			private Light _l;

			private void Start()
			{
				_l = GetComponent<Light>();
			}

			private void Update()
			{
				_t += Time.deltaTime;
				_l.intensity = peak * Mathf.Exp(-_t * 5f) * (_t < 0.05f ? _t / 0.05f : 1f);
				if (_t > 1.5f) Destroy(gameObject);
			}
		}
	}
}
