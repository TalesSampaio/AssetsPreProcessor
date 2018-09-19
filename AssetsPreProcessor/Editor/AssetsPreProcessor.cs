using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using ReorderableListExtended;

// Processor serialized structure
public class AssetsPreProcessor : ScriptableObject {
	#region Types
	[System.Serializable]
	public struct Action {
		public string actionName;
		[Reorderable]
		public FilterList filters;
		public Settings newImportSettings;
	}

	[System.Serializable]
	public struct Filter {
		public bool not;
		public FilterType type;
		public string definition;
	}

	[System.Serializable]
	public enum FilterType { ContainsOnName, InFolder, FileType }

	[System.Serializable]
	public struct Settings {
		public bool overrideTextures;
		public TextureSettings textureSettings;
		public bool overrideModels;
		public ModelSettings modelSettings;
		public bool overrideAudios;
		public AudioSettings audioSettings;
	}

	#region Textures
	[System.Serializable]
	public struct TextureSettings {
		//	Max Size
		public enum MaxSizes {
			_32 = 32,
			_64 = 64,
			_128 = 128,
			_256 = 256,
			_512 = 512,
			_1024 = 1024,
			_2048 = 2048,
			_4096 = 4096,
			_8192 = 8192
		}

		// Texture Type
		public TextureImporterType type;
		// Texture Shape
		public TextureImporterShape shape;
		// sRGB
		//public TextureImporterRGBMMode mode;
		public bool sRgb;
		// Alpha Source
		public TextureImporterAlphaSource alphaSource;
		public bool alphaIsTransparency;
		// Advanced
		public bool readWrite;
		public bool generateMipMaps;
		public TextureImporterMipFilter mipMapFilter;
		public bool mipMapsPreserveCoverage;
		public bool fadeoutMipMaps;
		// Wrap Mode
		public TextureWrapMode wrapMode;
		// Filter Mode
		public FilterMode filterMode;
		// Aniso Level
		[Range(0,16)]
		public int anisoLevel;
		// Per Platform
		public MaxSizes maxSize;
		//	Resize Algorithm
		public TextureResizeAlgorithm resizeAlgorithm;
		//	Compression
		public TextureCompressionQuality quality;
		//	Format
		public TextureImporterCompression format;
		//	Use Crunch Compression
		public bool useCrunchCompression;
		[Range(0,100)]
		public int compressorQuality;
	}
	#endregion

	#region Models
	[System.Serializable]
	public struct ModelSettings {
		//Meshes
		public float scaleFactor;
		public bool useFileScale;
		//public float fileScale = 1;
		public ModelImporterMeshCompression meshCompression;
		public bool readWriteEnabled;
		public bool optimizeMesh;
		public bool importBlendShapes;
		public bool generateColliders;
		public bool keepQuads;
		public bool weldVertices;
		public bool swapUvs;
		public bool generateLightmapUvs;

		//Normals & Tangents
		public ModelImporterNormals normals;
		public float smoothingAngle;
		public ModelImporterTangents tangents;

		// Materials
		public bool importMaterials;

		// Rig
		public ModelImporterAnimationType animationType;

		// Animations
		public bool importAnimation;
	}
	#endregion

	#region Audio
	[System.Serializable]
	public struct AudioSettings {
		public enum SampleRates {
			_8000Hz = 8000,
			_11025Hz = 11025,
			_22050Hz = 22050,
			_44100Hz = 44100,
			_48000Hz = 48000,
			_96000Hz = 96000,
			_192000Hz = 192000,
		}

		public bool useThisSettings;
		// Force To Mono
		public bool forceToMono;
		// Normalize
		public bool normalize;
		// Load In Background
		public bool loadInBackground;
		// Ambisonic
		public bool ambisonic;
		// LoadType
		public AudioClipLoadType loadType;
		// Preload Audio Data
		public bool preloadAudioData;
		// Compression Format
		public AudioCompressionFormat compressionFormat;
		// Quality
		[Range(1, 100)]
		public int quality;
		// Sample Rate Setting
		public AudioSampleRateSetting sampleRateSetting;
		// Sample Rate
		public SampleRates sampleRate;
	}
	#endregion

	#endregion

	[System.Serializable]
	public class FilterList : ReorderableArray<Filter> { }

	[System.Serializable]
	public class ActionList : ReorderableArray<Action> { }

	[Reorderable]
	[SerializeField]
	public ActionList importActions;
}

// GUI
[CustomEditor(typeof(AssetsPreProcessor))]
public class AssetPreProcessor_Editor : Editor {
	const string path = "Assets/Editor/AssetsPreProcessor.asset";
	AssetsPreProcessor asset;

	void OnEnable () {
		asset = AssetDatabase.LoadAssetAtPath<AssetsPreProcessor>(path);
	}

	public override void OnInspectorGUI () {
		if (asset == null) {
			Create_AssetsPreProcessor_Settings ();
		}

		DrawDefaultInspector ();
	}

	[MenuItem("Tools/Assets Pre Processor")]
	public static AssetsPreProcessor Create_AssetsPreProcessor_Settings () {
		AssetsPreProcessor asset;

		if (File.Exists (path)) {
			asset = AssetDatabase.LoadAssetAtPath<AssetsPreProcessor>(path);
		} else {
			asset = ScriptableObject.CreateInstance<AssetsPreProcessor> ();
			AssetDatabase.CreateAsset (asset, path);
			AssetDatabase.SaveAssets ();
		}

		EditorGUIUtility.PingObject (asset);
		Selection.activeObject = asset;
		return asset;
	}
}
	
[CustomPropertyDrawer(typeof(AssetsPreProcessor.Filter))]
public class FilterDrawer : PropertyDrawer {
	Color initialColor = GUI.color;

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		Rect r1, r2, r3;

		r1 = position;
		r1.xMin -= 10;
		r1.width = 60;

		r2 = position;
		r2.xMin = r1.xMax + 5;
		r2.width = 110;

		r3 = position;
		r3.xMin = r2.xMax + 5;

		EditorGUI.BeginProperty(position, label, property);
		r1.yMin -= 1;

		if (GUI.Button (r1, "NOT")) {
			property.FindPropertyRelative ("not").boolValue = !property.FindPropertyRelative ("not").boolValue;
		}

		r1.xMin += 2;
		GUI.Toggle (r1, property.FindPropertyRelative ("not").boolValue, "");
		GUI.color = initialColor;
		//EditorGUI.PropertyField(r1, property.FindPropertyRelative("not"), GUIContent.none);
		EditorGUI.PropertyField(r2, property.FindPropertyRelative("type"), GUIContent.none);
		EditorGUI.PropertyField(r3, property.FindPropertyRelative("definition"), GUIContent.none);
		EditorGUI.EndProperty();
	}
}

// Processor
public class ImporterProcessor : AssetPostprocessor {
	const string path = "Assets/Editor/AssetsPreProcessor.asset";
	AssetsPreProcessor settingsAsset;

	// Returns a filtered list of actions to apply on the asset
	List<int> FilterActionsToApply (string assetPath) {
		if (settingsAsset == null || settingsAsset.importActions.Count == 0) return null;

		bool passed = true;
		int i, j;
		string pathTemp;
		List<int> actionsToAplly = new List<int>();

		for (i = 0; i < settingsAsset.importActions.Count; i ++) {
			for (j = 0; j < settingsAsset.importActions [i].filters.Count; j++) {
				switch (settingsAsset.importActions [i].filters [j].type) {
				case AssetsPreProcessor.FilterType.ContainsOnName:
					passed = passed &&
						Path.GetFileName (assetPath).ToLower ().Contains (settingsAsset.importActions [i].filters [j].definition.ToLower ()) &&
						!settingsAsset.importActions [i].filters [j].not;
					break;
				case AssetsPreProcessor.FilterType.InFolder:
					pathTemp = Path.GetDirectoryName (assetPath);
					passed = passed &&
						pathTemp.ToLower ().Contains (settingsAsset.importActions [i].filters [j].definition.ToLower ()) &&
						!settingsAsset.importActions [i].filters [j].not;
					break;
				case AssetsPreProcessor.FilterType.FileType:
					passed = passed &&
						Path.GetExtension (assetPath).ToLower ().Contains (settingsAsset.importActions [i].filters [j].definition.Replace(".", string.Empty).ToLower ()) &&
						!settingsAsset.importActions [i].filters [j].not;
					break;
				}
			}

			if (passed) {
				actionsToAplly.Add (i);
			}

			// Reseting passed state
			passed = true;
		}

		string debugString = "";

		foreach (int a in actionsToAplly) {
			debugString += settingsAsset.importActions [a].actionName + ", ";
		}

		Debug.Log("[AssetPreProcessor]: Actions to apply: " + debugString);
		return actionsToAplly;
	}

	#region Model
	void OnPreprocessModel () {
		ModelImporter importer = assetImporter as ModelImporter;
		settingsAsset = AssetDatabase.LoadAssetAtPath<AssetsPreProcessor>(path);

		if (settingsAsset == null || settingsAsset.importActions.Count == 0) return;
			
		if (File.Exists(importer.assetPath + ".meta")) {
			//Debug.Log(modelImporter.assetPath + ".meta" + " already exists");
		} else {
			List<int> actionsToApply = FilterActionsToApply (importer.assetPath);

			if (importer != null) {
				foreach (int element in actionsToApply) {
					bool useImportSettings = true;

					if (settingsAsset.importActions[element].newImportSettings.overrideModels) {
						// MESHES
						//if (settingsAsset.importActions[element].newImportSettings.modelSettings.useMeshesCustom) {
						importer.globalScale = settingsAsset.importActions[element].newImportSettings.modelSettings.scaleFactor;
						importer.useFileUnits = settingsAsset.importActions[element].newImportSettings.modelSettings.useFileScale;
						//modelImporter.fileScale = settingsAsset.actions[element].settings.models.fileScale;
						importer.meshCompression = settingsAsset.importActions[element].newImportSettings.modelSettings.meshCompression;
						importer.isReadable = settingsAsset.importActions[element].newImportSettings.modelSettings.readWriteEnabled;
						importer.optimizeMesh = settingsAsset.importActions[element].newImportSettings.modelSettings.optimizeMesh;
						importer.importBlendShapes = settingsAsset.importActions[element].newImportSettings.modelSettings.importBlendShapes;
						importer.addCollider = settingsAsset.importActions[element].newImportSettings.modelSettings.generateColliders;
						importer.keepQuads = settingsAsset.importActions[element].newImportSettings.modelSettings.keepQuads;
						importer.weldVertices = settingsAsset.importActions[element].newImportSettings.modelSettings.weldVertices;
						importer.swapUVChannels = settingsAsset.importActions[element].newImportSettings.modelSettings.swapUvs;
						importer.generateSecondaryUV = settingsAsset.importActions[element].newImportSettings.modelSettings.generateLightmapUvs;
						//}

						// NORMALS & TANGENTS
						//if (settingsAsset.importActions[element].newImportSettings.modelSettings.useNormalsCustom) {
						importer.importNormals = settingsAsset.importActions[element].newImportSettings.modelSettings.normals;
						importer.normalSmoothingAngle = settingsAsset.importActions[element].newImportSettings.modelSettings.smoothingAngle;
						importer.importTangents = settingsAsset.importActions[element].newImportSettings.modelSettings.tangents;
						//}

						// MATERIALS
						//if (settingsAsset.importActions[element].newImportSettings.modelSettings.useMaterialsCustom) {
						importer.importMaterials = settingsAsset.importActions[element].newImportSettings.modelSettings.importMaterials;
						//}

						// RIG
						//if (settingsAsset.importActions[element].newImportSettings.modelSettings.useRigCustom) {
						importer.animationType = settingsAsset.importActions[element].newImportSettings.modelSettings.animationType;
						//}

						// ANIMATIONS
						//if (settingsAsset.importActions[element].newImportSettings.modelSettings.useAnimationsCustom) {
						importer.importAnimation = settingsAsset.importActions[element].newImportSettings.modelSettings.importAnimation;
						//}
					}
				}
			}
		}
	}
	#endregion

	#region Texture
	void OnPreprocessTexture () {
		TextureImporter importer = assetImporter as TextureImporter;
		settingsAsset = AssetDatabase.LoadAssetAtPath<AssetsPreProcessor>(path);

		if (settingsAsset == null || settingsAsset.importActions.Count == 0) return;

		if (File.Exists (importer.assetPath + ".meta")) {
			//Debug.Log (textureImporter.assetPath + ".meta" + " already exists");
		} else {
			List<int> actionsToApply = FilterActionsToApply (importer.assetPath);

			if (importer != null) {
				foreach (int element in actionsToApply) {
					if (settingsAsset.importActions[element].newImportSettings.overrideTextures) {
						importer.textureType = settingsAsset.importActions[element].newImportSettings.textureSettings.type;
						importer.textureShape = settingsAsset.importActions[element].newImportSettings.textureSettings.shape;
						importer.sRGBTexture = settingsAsset.importActions[element].newImportSettings.textureSettings.sRgb;
						importer.alphaSource = settingsAsset.importActions[element].newImportSettings.textureSettings.alphaSource;
						importer.alphaIsTransparency = settingsAsset.importActions[element].newImportSettings.textureSettings.alphaIsTransparency;
						importer.isReadable = settingsAsset.importActions[element].newImportSettings.textureSettings.readWrite;
						importer.mipmapEnabled = settingsAsset.importActions[element].newImportSettings.textureSettings.generateMipMaps;
						importer.mipmapFilter = settingsAsset.importActions[element].newImportSettings.textureSettings.mipMapFilter;
						importer.mipMapsPreserveCoverage = settingsAsset.importActions[element].newImportSettings.textureSettings.mipMapsPreserveCoverage;
						importer.fadeout = settingsAsset.importActions[element].newImportSettings.textureSettings.fadeoutMipMaps;
						importer.wrapMode = settingsAsset.importActions[element].newImportSettings.textureSettings.wrapMode;
						importer.filterMode = settingsAsset.importActions[element].newImportSettings.textureSettings.filterMode;
						importer.anisoLevel = settingsAsset.importActions[element].newImportSettings.textureSettings.anisoLevel;
						importer.maxTextureSize = (int)settingsAsset.importActions[element].newImportSettings.textureSettings.maxSize;
						// ? textureImporter.resizeAlgorithm = settingsAsset.actions[element].settings.textures.resizeAlgorithm;
						//textureImporter.textureFormat = settingsAsset.actions[element].settings.textures.quality;
						importer.textureCompression = settingsAsset.importActions[element].newImportSettings.textureSettings.format;
						importer.crunchedCompression = settingsAsset.importActions[element].newImportSettings.textureSettings.useCrunchCompression;
						importer.compressionQuality = settingsAsset.importActions[element].newImportSettings.textureSettings.compressorQuality;
					}
				}
			}
		}
	}
	#endregion

	#region Audio
	void OnPreprocessAudio () {
		AudioImporter importer = assetImporter as AudioImporter;
		settingsAsset = AssetDatabase.LoadAssetAtPath<AssetsPreProcessor>(path);

		if (settingsAsset == null || settingsAsset.importActions.Count == 0) return;

		if (File.Exists (importer.assetPath + ".meta")) {
			//Debug.Log (audioImporter.assetPath + ".meta" + " already exists");
		} else {
			List<int> actionsToApply = FilterActionsToApply (importer.assetPath);

			if (importer != null) {
				foreach (int element in actionsToApply) {
					if (settingsAsset.importActions[element].newImportSettings.overrideAudios) {
						AudioImporterSampleSettings sampleSettings = new AudioImporterSampleSettings ();
						sampleSettings.loadType = settingsAsset.importActions[element].newImportSettings.audioSettings.loadType;
						sampleSettings.compressionFormat = settingsAsset.importActions[element].newImportSettings.audioSettings.compressionFormat;
						sampleSettings.quality = ((float)settingsAsset.importActions[element].newImportSettings.audioSettings.quality * 0.01f);
						sampleSettings.sampleRateSetting = settingsAsset.importActions[element].newImportSettings.audioSettings.sampleRateSetting;
						sampleSettings.sampleRateOverride = (uint)settingsAsset.importActions[element].newImportSettings.audioSettings.sampleRate;

						importer.forceToMono = settingsAsset.importActions[element].newImportSettings.audioSettings.forceToMono;
						importer.loadInBackground = settingsAsset.importActions[element].newImportSettings.audioSettings.loadInBackground;
						importer.ambisonic = settingsAsset.importActions[element].newImportSettings.audioSettings.ambisonic;
						importer.preloadAudioData = settingsAsset.importActions[element].newImportSettings.audioSettings.preloadAudioData;
						// ? audioImporter.normalize = settingsAsset.actions[element].settings.audios.normalize;
						importer.defaultSampleSettings = sampleSettings;
					}
				}
			}
		}
	}
	#endregion
}