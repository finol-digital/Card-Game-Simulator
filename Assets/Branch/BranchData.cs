using UnityEngine;
using System.Collections;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
[InitializeOnLoad]
#endif

public class BranchData : ScriptableObject {

	// release data
	public string liveBranchKey;
	public string liveBranchUri;
	public string liveAndroidPathPrefix;
	public string[] liveAppLinks;

	// test data
	public string testBranchKey;
	public string testBranchUri;
	public string testAndroidPathPrefix;
	public string[] testAppLinks;

	// is simulation of fresh links enabled?
	public bool simulateFreshInstalls = false;

	// which set of data will we use, test or release?
	public bool testMode = false;

	private const string BDAssetPath = "Branch/Resources";
	private const string BDAssetName = "BranchData";
	private const string BDAssetExtension = ".asset";
	private static BranchData instance;

	public static BranchData Instance {
		get {
			if(instance == null) {
				LoadBranchData();
			}

			return instance;
		}
	}

	private static void LoadBranchData() {
		instance = Resources.Load(BDAssetName) as BranchData;

		if(instance == null) {
			instance = CreateInstance<BranchData>();
			
			#if UNITY_EDITOR
			
			if (!Directory.Exists(BDAssetPath)) {
				Directory.CreateDirectory(BDAssetPath);
				AssetDatabase.Refresh();
			}
			
			string fullPath = Path.Combine(Path.Combine("Assets", BDAssetPath), BDAssetName + BDAssetExtension );
			AssetDatabase.CreateAsset(instance, fullPath);
			
			#endif
		}
	}


}
