using UnityEngine;

namespace GliderJam.World
{
	// Deterministic noise helpers shared by the terrain sculpt (editor) and the mesh terrain (runtime).
	// Pure C# (no Unity API) so it can run on worker threads.
	public static class WorldNoise
	{
		private static readonly int[] Perm = BuildPermutation();

		private static int[] BuildPermutation()
		{
			var p = new int[512];
			var rng = new System.Random(1337);
			var src = new int[256];
			for (int i = 0; i < 256; i++)
			{
				src[i] = i;
			}
			for (int i = 255; i > 0; i--)
			{
				int j = rng.Next(i + 1);
				(src[i], src[j]) = (src[j], src[i]);
			}
			for (int i = 0; i < 512; i++)
			{
				p[i] = src[i & 255];
			}
			return p;
		}

		private static float Grad(int hash, float x, float z)
		{
			switch (hash & 7)
			{
				case 0: return x + z;
				case 1: return -x + z;
				case 2: return x - z;
				case 3: return -x - z;
				case 4: return x;
				case 5: return -x;
				case 6: return z;
				default: return -z;
			}
		}

		// Gradient noise in roughly [-1, 1].
		public static float Perlin(float x, float z)
		{
			int xi = (int)System.Math.Floor(x), zi = (int)System.Math.Floor(z);
			float xf = x - xi, zf = z - zi;
			xi &= 255;
			zi &= 255;
			float u = xf * xf * xf * (xf * (xf * 6f - 15f) + 10f);
			float v = zf * zf * zf * (zf * (zf * 6f - 15f) + 10f);
			int aa = Perm[Perm[xi] + zi], ab = Perm[Perm[xi] + zi + 1];
			int ba = Perm[Perm[xi + 1] + zi], bb = Perm[Perm[xi + 1] + zi + 1];
			float x1 = Lerp(Grad(aa, xf, zf), Grad(ba, xf - 1f, zf), u);
			float x2 = Lerp(Grad(ab, xf, zf - 1f), Grad(bb, xf - 1f, zf - 1f), u);
			return Lerp(x1, x2, v) * 0.97f;
		}

		private static float Lerp(float a, float b, float t)
		{
			return a + (b - a) * t;
		}

		public static float Fbm(float x, float z, int octaves, float lacunarity = 2.03f, float gain = 0.5f)
		{
			float sum = 0f, amp = 1f, norm = 0f;
			for (int i = 0; i < octaves; i++)
			{
				sum += Perlin(x, z) * amp;
				norm += amp;
				amp *= gain;
				x = x * lacunarity + 17.1f;
				z = z * lacunarity - 9.7f;
			}
			return sum / norm;
		}

		// Ridged multifractal in [0,1]: sharp crests, used for mountain ranges and rock ribs.
		public static float Ridged(float x, float z, int octaves, float lacunarity = 2.1f, float gain = 0.5f)
		{
			float sum = 0f, amp = 0.5f, weight = 1f, norm = 0f;
			for (int i = 0; i < octaves; i++)
			{
				float n = 1f - Mathf.Abs(Perlin(x, z));
				n *= n;
				n *= weight;
				weight = Mathf.Clamp01(n * 2f);
				sum += n * amp;
				norm += amp;
				amp *= gain;
				x = x * lacunarity + 5.3f;
				z = z * lacunarity + 11.9f;
			}
			return sum / norm;
		}

		// Cheap 3D noise from three 2D slices; used for moss streaks that vary with height.
		public static float Perlin3(float x, float y, float z)
		{
			return (Perlin(x, y) + Perlin(y + 31.7f, z) + Perlin(z - 12.3f, x)) / 3f;
		}

		// Cellular (Worley) F1 distance in cell units plus a per-cell random value.
		public static float Cellular(float x, float z, out float cellRandom)
		{
			int cx = Mathf.FloorToInt(x), cz = Mathf.FloorToInt(z);
			float best = 10f;
			cellRandom = 0f;
			for (int dz = -1; dz <= 1; dz++)
			{
				for (int dx = -1; dx <= 1; dx++)
				{
					int ix = cx + dx, iz = cz + dz;
					float px = ix + Hash(ix, iz, 1), pz = iz + Hash(ix, iz, 2);
					float d = (px - x) * (px - x) + (pz - z) * (pz - z);
					if (d < best)
					{
						best = d;
						cellRandom = Hash(ix, iz, 3);
					}
				}
			}
			return Mathf.Sqrt(best);
		}

		public static float Hash(int x, int z, int seed)
		{
			unchecked
			{
				uint h = (uint)(x * 374761393 + z * 668265263 + seed * 2147483647);
				h = (h ^ (h >> 13)) * 1274126177u;
				h ^= h >> 16;
				return (h & 0xFFFFFF) / 16777215f;
			}
		}

		public static float SmoothStep(float edge0, float edge1, float x)
		{
			float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
			return t * t * (3f - 2f * t);
		}
	}
}
