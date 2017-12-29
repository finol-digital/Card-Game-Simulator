using UnityEngine;
using System.Collections;

namespace RavingBots.EdgePadding
{
	public class AutoPadding : MonoBehaviour
	{
		public PaddingMode PaddingMode;
		[Range(0, 1024)] public int MaxPaddingIterations;
		[Range(0, 32)] public int TrimIterations;

		Material _sharedMaterial;
		Texture2D _originalTexture;

		void Awake()
		{
			var meshRenderer = GetComponent<MeshRenderer>();
			if (meshRenderer)
				_sharedMaterial = meshRenderer.sharedMaterial;

			if (_sharedMaterial)
				_originalTexture = _sharedMaterial.mainTexture as Texture2D;

			ApplyPadding();
		}

		public void ApplyPadding()
		{
			if (_sharedMaterial && _originalTexture)
				_sharedMaterial.mainTexture = ApplyPadding(_originalTexture);
		}

		Texture2D ApplyPadding(Texture2D texture)
		{
			texture = texture.Clone();
			texture.ApplyPadding(PaddingMode, TrimIterations, MaxPaddingIterations);

			return texture;
		}

		void OnDisable()
		{
			if (_sharedMaterial && _originalTexture)
				_sharedMaterial.mainTexture = _originalTexture;
		}
    }
}
