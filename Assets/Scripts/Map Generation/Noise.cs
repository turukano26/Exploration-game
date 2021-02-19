using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NoiseMapType noiseType)
	{
		float[,] noiseMap = new float[mapWidth, mapHeight];

		System.Random prng = new System.Random(seed);
		Vector2[] octaveOffsets = new Vector2[octaves];
		for (int i = 0; i < octaves; i++)
		{
			float offsetX = prng.Next(-100000, 100000) + offset.x;
			float offsetY = prng.Next(-100000, 100000) + offset.y;
			octaveOffsets[i] = new Vector2(offsetX, offsetY);
		}

		if (scale <= 0)
		{
			scale = 0.0001f;
		}

		float maxNoiseHeight = float.MinValue;
		float minNoiseHeight = float.MaxValue;

		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;


		for (int y = 0; y < mapHeight; y++)
		{
			for (int x = 0; x < mapWidth; x++)
			{

				float amplitude = 1;
				float frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < octaves; i++)
				{
					float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
					float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
					noiseHeight += perlinValue * amplitude;

					amplitude *= persistance;
					frequency *= lacunarity;
				}

				if (noiseHeight > maxNoiseHeight)
				{
					maxNoiseHeight = noiseHeight;
				}
				else if (noiseHeight < minNoiseHeight)
				{
					minNoiseHeight = noiseHeight;
				}

                if (noiseType==NoiseMapType.perlin)
                {
					noiseMap[x, y] = noiseHeight; //soft boundary at -1 to soft boundary at 1
                }
                else if (noiseType == NoiseMapType.ridged)
				{
					noiseMap[x, y] = Mathf.Abs(noiseHeight) * -1; //soft boundary at -1 to hard boundary at 0
				}
				else if (noiseType == NoiseMapType.valley)
                {
					noiseMap[x, y] = Mathf.Abs(noiseHeight); //hard boundary at 0 to soft boundary at 1
				}
			}
		}
		if (noiseType != NoiseMapType.perlin)
        {
			if (Mathf.Abs(minNoiseHeight) > maxNoiseHeight)
			{
				maxNoiseHeight = Mathf.Abs(minNoiseHeight);
			}
		}
		for (int y = 0; y < mapHeight; y++)
		{
			for (int x = 0; x < mapWidth; x++)
			{
				if (noiseType == NoiseMapType.perlin)
                {
					noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]) * 2 - 1;
				}
				if (noiseType == NoiseMapType.ridged)
				{
					noiseMap[x, y] = Mathf.InverseLerp(-maxNoiseHeight, 0, noiseMap[x, y]) * 2 - 1;
				}
				if (noiseType == NoiseMapType.valley)
				{
					noiseMap[x, y] = Mathf.InverseLerp(0, maxNoiseHeight, noiseMap[x, y]) * 2 - 1;
				}
			}
		}
		return noiseMap;
	}
}
public enum NoiseMapType
{
	perlin,
	ridged,
	valley
}
