using UnityEngine;
using System.Collections;

namespace RavingBots.EdgePadding
{
	public enum PaddingMode { Simple, Diagonal }

	public static class TextureUtils
	{
		public static void ApplyPadding(this Texture2D texture, 
			PaddingMode paddingMode = PaddingMode.Simple, int trimIterations = 0, int maxPaddingIterations = 0)
		{
			var trimShader = LoadShader("Hidden/Edge Padding/Trim");
			if (!trimShader)
				return;

			var paddingShader = LoadShader("Hidden/Edge Padding/" + paddingMode);
			if (!paddingShader)
				return;

			var overrideShader = LoadShader("Hidden/Edge Padding/Override");
			if (!overrideShader)
				return;

			var material = new Material(trimShader);
			material.hideFlags = HideFlags.HideAndDontSave;
			material.SetVector("_Delta", new Vector4(1f / texture.width, 1f / texture.height, 0, 0));

			var tr1 = GetTemporaryRT(texture);
			Graphics.Blit(texture, tr1);

			for (var i = 0; i < trimIterations; i++)
			{
				var tr2 = GetTemporaryRT(texture);
				Graphics.Blit(tr1, tr2, material);
				RenderTexture.ReleaseTemporary(tr1);
				tr1 = tr2;
			}

			material.shader = paddingShader;

			var paddingInterations = Mathf.Min(Mathf.Max(texture.width, texture.height),
				maxPaddingIterations <= 0 ? int.MaxValue : maxPaddingIterations);

			for (var i = 0; i < paddingInterations; i++)
			{
				var tr2 = GetTemporaryRT(texture);
				Graphics.Blit(tr1, tr2, material);
				RenderTexture.ReleaseTemporary(tr1);
				tr1 = tr2;
			}

			material.shader = overrideShader;
			material.SetTexture("_Override", texture);

			var tr3 = GetTemporaryRT(texture);
			Graphics.Blit(tr1, tr3, material);
			RenderTexture.ReleaseTemporary(tr1);

			RenderTexture.active = tr3;
			texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
			RenderTexture.active = null;
			RenderTexture.ReleaseTemporary(tr3);
			texture.Apply();

			Object.DestroyImmediate(material);
		}

		static Shader LoadShader(string shaderName)
		{
			var result = Shader.Find(shaderName);
			if (!result)
				Debug.LogWarningFormat("Cannot find '{0}' shader (please place it in the Resources folder)", shaderName);

			return result;
		}

		static RenderTexture GetTemporaryRT(Texture2D texture)
		{
			var result = RenderTexture.GetTemporary(texture.width, texture.height);
			result.filterMode = FilterMode.Point;

			return result;
		}

		public static Texture2D Clone(this Texture2D texture)
		{
			var result = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, texture.mipmapCount > 1);
			result.filterMode = texture.filterMode;
			result.anisoLevel = texture.anisoLevel;
			result.wrapMode = texture.wrapMode;
			result.name = texture.name + " (Clone)";

			var tr = GetTemporaryRT(texture);
			Graphics.Blit(texture, tr);

			RenderTexture.active = tr;
			result.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0);
			RenderTexture.active = null;
			RenderTexture.ReleaseTemporary(tr);
			result.Apply();

			return result;
		}
	}
}
