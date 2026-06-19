// Amplify Scatter FREE
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using System.Collections.Generic;

namespace AmplifyScatter
{
	public class AmplifyScatter : EditorWindow
	{
		// Per-project key so different Unity projects on the same machine don't share state
		// (especially the spawned-objects list, whose references are scene/project-bound).
		static string PrefsKey => "AmplifyScatter_Settings_" + Application.dataPath.GetHashCode().ToString( "X8" );
		const string VolumeIconGUID = "1072f6d1e58c84f459ccdf50dab3c087";

		// One prefab + its weight. Lower priority = picked more often.
		[System.Serializable]
		public class PrefabEntry
		{
			public GameObject prefab;
			public int priority = 0;
		}

		// All persisted state. Serialized to EditorPrefs as JSON.
		[System.Serializable]
		public class Settings
		{
			public List<PrefabEntry> prefabs = new();

			public Vector3 center = Vector3.zero;
			public Vector3 size = new Vector3( 20, 10, 20 );

			public float poissonRadius = 2f;
			public int rejectionSamples = 30;

			public int seed = 12345;

			public LayerMask raycastMask = ~0;

			public bool randomYRotation = true;
			public float minYRotation = 0f;
			public float maxYRotation = 360f;

			public float minSlopeAngle = 0f;
			public float maxSlopeAngle = 90f;

			public bool alignToNormal = false;
			public float minTiltVariation = 0f;
			public float maxTiltVariation = 0f;
			public float tiltThresholdMin = 0f;
			public float tiltThresholdMax = 90f;

			public float minScale = 1f;
			public float maxScale = 1f;

			public bool avoidOverlap = false;

			public bool randomSeed = false;
			public bool hideSpawnVolume = false;
			public bool shadeSpawnVolume = false;

			public Transform parent;

			public bool prefabListExpanded = true;

			public List<GameObject> spawned = new();
		}

		static Settings settings = new Settings();

		ReorderableList prefabList;
		Vector2 scroll;

		GUIStyle dropZoneStyle;

		AffiliateBanner affiliateBanner = new AffiliateBanner();

		static bool volumeSelected = false;
		static Texture2D volumeIcon;
		static readonly Color VolumeColor = new Color( 0xEE / 255f, 0x9D / 255f, 0x00 / 255f );

		// Hidden ScriptableObject so Unity's Undo system can track volume center/size.
		class VolumeUndoState : ScriptableObject
		{
			public Vector3 center;
			public Vector3 size;
		}

		static AmplifyScatter instance;

		// Sticky: once the user has opened the tool in this editor session we keep drawing the
		// volume through window tear-downs (shift+space maximize destroys sibling windows).
		static bool everOpened;

		static VolumeUndoState undoState;

		static void EnsureUndoState()
		{
			if ( undoState == null )
			{
				undoState = ScriptableObject.CreateInstance<VolumeUndoState>();
				undoState.hideFlags = HideFlags.HideAndDontSave;
				undoState.center = settings.center;
				undoState.size = settings.size;
			}

		}

		// Pushes Undo-restored values back into the live settings.
		static void OnUndoRedo()
		{
			if ( undoState != null )
			{
				bool changed = false;

				if ( settings.center != undoState.center )
				{
					settings.center = undoState.center;
					changed = true;
				}

				if ( settings.size != undoState.size )
				{
					settings.size = undoState.size;
					changed = true;
				}

				if ( changed )
				{
					SaveSettings();
					SceneView.RepaintAll();

					if ( instance != null )
					{
						instance.Repaint();
					}
				}
			}
		}

		[MenuItem( "Window/Amplify Scatter" )]
		static void Open()
		{
			GetWindow<AmplifyScatter>( "Amplify Scatter" );
		}

		// Subscriptions live in a static initializer so they survive window enable/disable
		// (e.g. when shift+space maximizes the Scene view and disables sibling windows).
		[InitializeOnLoadMethod]
		static void StaticInit()
		{
			LoadSettings();

			SceneView.duringSceneGui -= DuringSceneGUI;
			SceneView.duringSceneGui += DuringSceneGUI;

			Selection.selectionChanged -= OnSelectionChanged;
			Selection.selectionChanged += OnSelectionChanged;

			Undo.undoRedoPerformed -= OnUndoRedo;
			Undo.undoRedoPerformed += OnUndoRedo;

			EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
			EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
		}

		// Spawned GameObject references are tied to the open scene. Drop them on scene change
		// so "Delete Spawned Prefabs" can't act on stale refs from a previous scene.
		static void OnSceneChanged( UnityEngine.SceneManagement.Scene from, UnityEngine.SceneManagement.Scene to )
		{
			if ( settings.spawned != null && settings.spawned.Count > 0 )
			{
				settings.spawned.Clear();
				SaveSettings();
			}
		}

		void OnEnable()
		{
			instance = this;
			everOpened = true;

			LoadSettings();

			BuildPrefabList();

			LoadVolumeIcon();

			EnsureUndoState();
		}

		void OnDisable()
		{
			SaveSettings();
		}

		void OnDestroy()
		{
			if ( instance == this )
			{
				instance = null;
			}
		}

		// Deselects the volume when the user picks a regular GameObject.
		static void OnSelectionChanged()
		{
			if ( Selection.activeObject != null && volumeSelected )
			{
				volumeSelected = false;
				SceneView.RepaintAll();
			}
		}

		// Draws six translucent faces for the volume's filled look.
		static void DrawShadedVolume( Vector3 center, Vector3 size )
		{
			Vector3 h = size * 0.5f;

			Vector3 c0 = center + new Vector3( -h.x, -h.y, -h.z );
			Vector3 c1 = center + new Vector3( h.x, -h.y, -h.z );
			Vector3 c2 = center + new Vector3( h.x, h.y, -h.z );
			Vector3 c3 = center + new Vector3( -h.x, h.y, -h.z );
			Vector3 c4 = center + new Vector3( -h.x, -h.y, h.z );
			Vector3 c5 = center + new Vector3( h.x, -h.y, h.z );
			Vector3 c6 = center + new Vector3( h.x, h.y, h.z );
			Vector3 c7 = center + new Vector3( -h.x, h.y, h.z );

			Color fill = new Color( 0xA0 / 255f, 0x68 / 255f, 0x00 / 255f, 0.12f );
			Color outline = new Color( 0, 0, 0, 0 );

			Handles.DrawSolidRectangleWithOutline( new Vector3[] { c0, c1, c2, c3 }, fill, outline );
			Handles.DrawSolidRectangleWithOutline( new Vector3[] { c5, c4, c7, c6 }, fill, outline );
			Handles.DrawSolidRectangleWithOutline( new Vector3[] { c4, c0, c3, c7 }, fill, outline );
			Handles.DrawSolidRectangleWithOutline( new Vector3[] { c1, c5, c6, c2 }, fill, outline );
			Handles.DrawSolidRectangleWithOutline( new Vector3[] { c4, c5, c1, c0 }, fill, outline );
			Handles.DrawSolidRectangleWithOutline( new Vector3[] { c3, c2, c6, c7 }, fill, outline );
		}

		static void LoadVolumeIcon()
		{
			if ( volumeIcon == null )
			{
				volumeIcon = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( VolumeIconGUID ) );
			}
		}

		void EnsureStyles()
		{
			if ( dropZoneStyle == null )
			{
				dropZoneStyle = new GUIStyle() {
					alignment = TextAnchor.MiddleCenter,
					fontStyle = FontStyle.Bold,
					fontSize = 13
				};
				dropZoneStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color( 0.85f, 0.85f, 0.85f ) : Color.black;
			}

		}

		// Sets up the ReorderableList shown in the Prefabs foldout.
		void BuildPrefabList()
		{
			prefabList = new ReorderableList( settings.prefabs, typeof( PrefabEntry ), true, true, true, true );

			prefabList.drawHeaderCallback = rect =>
			{
				EditorGUI.LabelField( new Rect( rect.x, rect.y, rect.width - 60, rect.height ),
					new GUIContent(
						"Prefab",
						"Prefab to spawn." ) );

				EditorGUI.LabelField( new Rect( rect.x + rect.width - 60, rect.y, 60, rect.height ),
					new GUIContent(
						"Priority",
						"Lower = higher spawn chance (0 is most likely)." ) );
			};

			prefabList.drawElementCallback = ( rect, index, active, focused ) =>
			{
				var entry = settings.prefabs[ index ];

				rect.y += 2;
				rect.height = EditorGUIUtility.singleLineHeight;

				float priorityWidth = 55f;
				float gap = 5f;

				entry.prefab = ( GameObject )EditorGUI.ObjectField( new Rect( rect.x, rect.y, rect.width - priorityWidth - gap, rect.height ), entry.prefab,
					typeof( GameObject ), false );

				entry.priority = EditorGUI.IntField( new Rect( rect.x + rect.width - priorityWidth, rect.y, priorityWidth, rect.height ), entry.priority );
				entry.priority = Mathf.Max( 0, entry.priority );
			};

			prefabList.onAddCallback = list => { settings.prefabs.Add( new PrefabEntry() ); };
		}

		void OnGUI()
		{
			affiliateBanner.Draw( this );

			EnsureStyles();

			scroll = EditorGUILayout.BeginScrollView( scroll );

			settings.prefabListExpanded = EditorGUILayout.Foldout( settings.prefabListExpanded, "Prefabs", true, EditorStyles.foldoutHeader );

			if ( settings.prefabListExpanded )
			{
				if ( prefabList == null )
				{
					BuildPrefabList();
				}

				prefabList.DoLayoutList();

				if ( settings.prefabs.Count > 0 )
				{
					if ( GUILayout.Button(
						new GUIContent(
							"Clear Prefab List",
							"Remove all prefabs from the list." ) ) )
					{
						settings.prefabs.Clear();
						BuildPrefabList();
						GUI.changed = true;
					}

					GUILayout.Space( 8 );
				}

				HandlePrefabDragAndDrop();
			}

			EditorGUILayout.Space();

			EditorGUILayout.LabelField( "Spawn Volume", EditorStyles.boldLabel );

			settings.center = EditorGUILayout.Vector3Field(
					new GUIContent(
						"Spawn Volume Center",
						"World-space center of the spawn volume." ),
					settings.center );

			settings.size =
				EditorGUILayout.Vector3Field(
					new GUIContent(
						"Spawn Volume Size",
						"Size of the spawn volume. X/Z control the area; Y controls raycast height." ),
					settings.size );

			if ( GUILayout.Button(
				new GUIContent(
					"Locate Spawn Volume",
					"Focus the Scene view camera on the spawn volume (same as F when the volume icon is selected)." ) ) )
			{
				var sv = SceneView.lastActiveSceneView;
				if ( sv != null )
				{
					sv.Frame( new Bounds( settings.center, settings.size ), false );
				}
			}

			EditorGUILayout.Space();

			EditorGUILayout.LabelField( "Poisson Settings", EditorStyles.boldLabel );

			settings.poissonRadius =
				Mathf.Max( 0.01f, EditorGUILayout.FloatField(
					new GUIContent(
						"Poisson Radius",
						"Minimum distance between spawned points." ),
					settings.poissonRadius ) );

			settings.rejectionSamples =
				EditorGUILayout.IntField(
					new GUIContent(
						"Rejection Samples",
						"Attempts per active point before giving up. Higher = denser packing, slower." ),
					settings.rejectionSamples );

			settings.seed =
				EditorGUILayout.IntField(
					new GUIContent(
						"Seed",
						"Random seed. Same seed + same settings = same result." ),
					settings.seed );

			EditorGUILayout.Space();

			EditorGUILayout.LabelField( "Placement", EditorStyles.boldLabel );

			settings.parent =
				( Transform )EditorGUILayout.ObjectField(
					new GUIContent(
						"Spawn Parent",
						"Optional transform to parent spawned objects under." ),
					settings.parent, typeof( Transform ), true );

			settings.raycastMask =
				LayerMaskField(
					new GUIContent(
						"Raycast Mask",
						"Layers the placement raycast can hit." ),
					settings.raycastMask );

			settings.avoidOverlap =
				EditorGUILayout.Toggle(
					new GUIContent(
						"Avoid Overlap",
						"Attempts to skip placements that overlap previously-spawned prefabs, existing scene meshes, or terrains. Temporarily adds MeshColliders to collider-less scene meshes (and to spawned prefabs that lack collision) for the duration of the spawn, then removes them. Cooking MeshColliders has a cost on large scenes." ),
					settings.avoidOverlap );

			GUILayout.Space( 8 );

			MinMaxField(
				new GUIContent(
					"Slope Angle Range",
					"Only spawn where the surface slope (degrees) falls inside this range." ),
				ref settings.minSlopeAngle, ref settings.maxSlopeAngle, 0f, 90f );

			GUILayout.Space( 8 );

			MinMaxField(
				new GUIContent(
					"Scale Range",
					"Random uniform scale range applied to each spawn." ),
				ref settings.minScale, ref settings.maxScale, 0.01f, 10f );

			GUILayout.Space( 8 );

			settings.alignToNormal =
				EditorGUILayout.Toggle(
					new GUIContent(
						"Align To Normal",
						"Orient the spawned object so its up axis matches the surface normal." ),
					settings.alignToNormal );

			if ( settings.alignToNormal )
			{
				EditorGUILayout.BeginVertical( EditorStyles.helpBox );
				GUILayout.Space( 4 );

				MinMaxField(
					new GUIContent(
						"Tilt Variation",
						"Random extra tilt (degrees) added on top of normal alignment, applied around a random horizontal axis." ),
					ref settings.minTiltVariation, ref settings.maxTiltVariation, 0f, 90f );

				GUILayout.Space( 2 );

				MinMaxField(
					new GUIContent(
						"Variation Threshold",
						"Slope-angle range (degrees) in which Tilt Variation is applied. Defaults to 0â€“90 (everything)." ),
					ref settings.tiltThresholdMin, ref settings.tiltThresholdMax, 0f, 90f );

				GUILayout.Space( 4 );
				EditorGUILayout.EndVertical();
			}

			GUILayout.Space( 8 );

			settings.randomYRotation =
				EditorGUILayout.Toggle(
					new GUIContent(
						"Random Y Rotation",
						"Apply a random rotation around the Y axis." ),
					settings.randomYRotation );

			if ( settings.randomYRotation )
			{
				EditorGUILayout.BeginVertical( EditorStyles.helpBox );
				GUILayout.Space( 4 );

				MinMaxField(
					new GUIContent(
						"Y Rotation Range",
						"Range (degrees) for the random Y rotation." ),
					ref settings.minYRotation, ref settings.maxYRotation, 0f, 360f );

				GUILayout.Space( 4 );
				EditorGUILayout.EndVertical();
			}

			GUILayout.Space( 20 );

			if ( GUILayout.Button(
				new GUIContent(
					"Spawn",
					"Generate Poisson points and place prefabs." ) ) )
				Spawn();

			if ( GUILayout.Button(
				new GUIContent(
					"Delete Spawned Prefabs",
					"Remove only the objects spawned by this tool." ) ) )
				Clear();

			DrawDivider();

			settings.randomSeed =
				EditorGUILayout.Toggle(
					new GUIContent(
						"Random Seed",
						"When ON, pick a new random seed on each Spawn." ),
					settings.randomSeed );

			settings.hideSpawnVolume =
				EditorGUILayout.Toggle(
					new GUIContent(
						"Hide Spawn Volume",
						"Hide the spawn volume gizmo in the Scene view." ),
					settings.hideSpawnVolume );

			settings.shadeSpawnVolume =
				EditorGUILayout.Toggle(
					new GUIContent(
						"Shade Spawn Volume",
						"Fill the spawn volume with a translucent darker-orange tint." ),
					settings.shadeSpawnVolume );

			GUILayout.Space( 4 );

			if ( GUILayout.Button(
				new GUIContent(
					"Reset Saved Settings",
					"Wipe persisted settings and reset to defaults." ) ) )
			{
				EditorPrefs.DeleteKey( PrefsKey );
				settings = new Settings();
				BuildPrefabList();
			}

			EditorGUILayout.EndScrollView();

			if ( GUI.changed )
			{
				SaveSettings();
				SceneView.RepaintAll();
			}
		}

		static void DrawDivider()
		{
			GUILayout.Space( 6 );

			Rect r = GUILayoutUtility.GetRect( GUIContent.none, GUIStyle.none, GUILayout.Height( 1 ), GUILayout.ExpandWidth( true ) );

			EditorGUI.DrawRect( r, new Color( 0.3f, 0.3f, 0.3f, 1f ) );

			GUILayout.Space( 6 );
		}

		static void MinMaxField( GUIContent label, ref float min, ref float max, float limitMin, float limitMax )
		{
			EditorGUILayout.LabelField( label );

			EditorGUILayout.BeginHorizontal();

			float fieldWidth = 55f;

			min = EditorGUILayout.FloatField( min, GUILayout.Width( fieldWidth ) );
			EditorGUILayout.MinMaxSlider( ref min, ref max, limitMin, limitMax );
			max = EditorGUILayout.FloatField( max, GUILayout.Width( fieldWidth ) );

			EditorGUILayout.EndHorizontal();

			min = Mathf.Clamp( min, limitMin, limitMax );
			max = Mathf.Clamp( max, min, limitMax );
		}

		// "Drop Prefabs Here!" zone: draws the visual and accepts dragged GameObjects.
		void HandlePrefabDragAndDrop()
		{
			Rect dropRect = GUILayoutUtility.GetRect( 0, 56, GUILayout.ExpandWidth( true ) );

			GUI.Box( dropRect, GUIContent.none, EditorStyles.helpBox );

			var icon = EditorGUIUtility.IconContent( "Prefab Icon" );

			float iconSize = 22f;
			float gap = 2f;

			float labelHeight = 18f;
			float blockHeight = ( icon != null && icon.image != null ) ? iconSize + gap + labelHeight : labelHeight;
			float startY = dropRect.y + ( dropRect.height - blockHeight ) * 0.5f;

			if ( icon != null && icon.image != null )
			{
				Rect iconRect = new Rect( dropRect.x + ( dropRect.width - iconSize ) * 0.5f, startY, iconSize, iconSize );
				GUI.DrawTexture( iconRect, icon.image, ScaleMode.ScaleToFit );
				startY += iconSize + gap;
			}

			Rect labelRect = new Rect( dropRect.x, startY, dropRect.width, labelHeight );
			GUI.Label( labelRect, "Drop Prefabs Here!", dropZoneStyle );

			Event evt = Event.current;

			if ( dropRect.Contains( evt.mousePosition ) )
			{
				if ( evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform )
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

					if ( evt.type == EventType.DragPerform )
					{
						DragAndDrop.AcceptDrag();

						foreach ( var obj in DragAndDrop.objectReferences )
						{
							if ( obj is GameObject go )
							{
								settings.prefabs.Add( new PrefabEntry { prefab = go } );
							}
						}

						SaveSettings();
						GUI.changed = true;
					}

					evt.Use();
				}
			}
		}

		// Weighted random pick. Weight = 1 / (priority + 1).
		static PrefabEntry PickWeightedPrefab()
		{
			float total = 0f;
			var weights = new float[ settings.prefabs.Count ];

			for ( int i = 0; i < settings.prefabs.Count; i++ )
			{
				if ( settings.prefabs[ i ].prefab != null )
				{
					float w = 1f / ( settings.prefabs[ i ].priority + 1f );
					weights[ i ] = w;
					total += w;
				}
			}

			if ( total > 0f )
			{
				float r = Random.value * total;
				float acc = 0f;

				for ( int i = 0; i < settings.prefabs.Count; i++ )
				{
					acc += weights[ i ];

					if ( r <= acc && settings.prefabs[ i ].prefab != null )
					{
						return settings.prefabs[ i ];
					}
				}
			}
			return null;
		}

		// Combined renderer bounds in world space; unit cube fallback if there are no renderers.
		static Bounds GetWorldBounds( GameObject go )
		{
			var renderers = go.GetComponentsInChildren<Renderer>();
			if ( renderers.Length > 0 )
			{
				Bounds b = renderers[ 0 ].bounds;
				for ( int i = 1; i < renderers.Length; i++ )
				{
					b.Encapsulate( renderers[ i ].bounds );
				}
				return b;
			}
			return new Bounds( go.transform.position, Vector3.one );
		}

		// Main scatter pass: generates Poisson points, raycasts each onto the surface,
		// filters by slope, picks a weighted prefab, applies rotation/scale, then (optionally)
		// rejects placements that overlap existing geometry.
		static void Spawn()
		{
			if ( settings.randomSeed )
			{
				settings.seed = Random.Range( int.MinValue, int.MaxValue );
			}
			Random.InitState( settings.seed );

			if ( settings.prefabs == null || settings.prefabs.Count == 0 )
			{
				return;
			}

			if ( settings.poissonRadius <= 0.001f || settings.size.x <= 0f || settings.size.z <= 0f )
			{
				return;
			}

			var samples = GeneratePoissonPoints( settings.poissonRadius, new Vector2( settings.size.x, settings.size.z ), settings.rejectionSamples );

			var tempColliders = new List<Collider>();
			// Grown adaptively below if a query saturates it, so dense scenes don't truncate.
			var overlapBuffer = new Collider[ 32 ];

			// Give every collider-less scene mesh inside the spawn volume a temporary MeshCollider
			// so both the placement raycast and the overlap test can see real triangle geometry.
			// Terrains already have a TerrainCollider by default. Skips meshes belonging to our
			// previously-spawned prefabs so we don't re-cook them on every Spawn, and skips
			// meshes whose renderer bounds don't intersect the volume region.
			{
				HashSet<GameObject> spawnedSet = null;
				if ( settings.spawned != null && settings.spawned.Count > 0 )
				{
					spawnedSet = new HashSet<GameObject>();
					foreach ( var g in settings.spawned )
					{
						if ( g != null )
						{
							spawnedSet.Add( g );
						}
					}
				}

				// Volume expanded 2x to keep meshes near the edge eligible for overlap tests.
				Bounds prepassBounds = new Bounds( settings.center, settings.size * 2f );

				var meshFilters = Object.FindObjectsByType<MeshFilter>( FindObjectsSortMode.None );

				foreach ( var mf in meshFilters )
				{
					if ( mf == null || mf.sharedMesh == null )
					{
						continue;
					}

					// MeshCollider cooking requires Read/Write enabled on the source mesh, which
					// is off by default for imported assets. Cooking fails noisily and breaks the
					// spawn, so skip those here.
					if ( !mf.sharedMesh.isReadable )
					{
						continue;
					}

					if ( !mf.gameObject.activeInHierarchy )
					{
						continue;
					}

					if ( mf.GetComponent<Collider>() != null )
					{
						continue;
					}

					var rend = mf.GetComponent<Renderer>();
					if ( rend != null && !prepassBounds.Intersects( rend.bounds ) )
					{
						continue;
					}

					if ( spawnedSet != null )
					{
						Transform t = mf.transform;
						bool belongsToSpawned = false;
						while ( t != null )
						{
							if ( spawnedSet.Contains( t.gameObject ) )
							{
								belongsToSpawned = true;
								break;
							}
							t = t.parent;
						}
						if ( belongsToSpawned )
						{
							continue;
						}
					}

					var mc = mf.gameObject.AddComponent<MeshCollider>();
					mc.sharedMesh = mf.sharedMesh;
					mc.convex = false;
					tempColliders.Add( mc );
				}

				Physics.SyncTransforms();
			}

			foreach ( var p in samples )
			{
				// Ray fires from the top of the volume and travels exactly its Y height, so hits
				// are confined to surfaces inside the volume (no roofs above, no floors below).
				Vector3 rayStart = settings.center + new Vector3( p.x - settings.size.x * 0.5f, settings.size.y * 0.5f, p.y - settings.size.z * 0.5f );

				if ( !Physics.Raycast( rayStart, Vector3.down, out RaycastHit hit, settings.size.y, settings.raycastMask ) )
				{
					continue;
				}

				float slope = Vector3.Angle( hit.normal, Vector3.up );
				if ( slope < settings.minSlopeAngle || slope > settings.maxSlopeAngle )
				{
					continue;
				}

				var entry = PickWeightedPrefab();
				if ( entry == null )
				{
					continue;
				}

				GameObject obj = ( GameObject )PrefabUtility.InstantiatePrefab( entry.prefab );

				Undo.RegisterCreatedObjectUndo( obj, "Spawn Prefab" );

				obj.transform.position = hit.point;

				Quaternion rot = Quaternion.identity;

				if ( settings.alignToNormal )
				{
					rot = Quaternion.FromToRotation( Vector3.up, hit.normal );

					bool inThreshold = slope >= settings.tiltThresholdMin && slope <= settings.tiltThresholdMax;
					if ( inThreshold )
					{
						float tilt = Random.Range( settings.minTiltVariation, settings.maxTiltVariation );

						if ( Mathf.Abs( tilt ) > 0.001f )
						{
							float tiltAngle = Random.Range( 0f, 360f );

							Vector3 horiz = Vector3.Cross( hit.normal, Vector3.up ).normalized;
							horiz = ( horiz.sqrMagnitude < 0.0001f ) ? Vector3.right : horiz;

							Vector3 tiltAxis = Quaternion.AngleAxis( tiltAngle, hit.normal ) * horiz;
							rot = Quaternion.AngleAxis( tilt, tiltAxis ) * rot;
						}
					}
				}

				if ( settings.randomYRotation )
				{
					rot *= Quaternion.Euler( 0, Random.Range( settings.minYRotation, settings.maxYRotation ), 0 );
				}

				obj.transform.rotation = rot;

				float s = Random.Range( settings.minScale, settings.maxScale );

				obj.transform.localScale = Vector3.one * s;

				if ( settings.parent )
				{
					obj.transform.SetParent( settings.parent, true );
				}

				if ( settings.avoidOverlap )
				{
					Physics.SyncTransforms();

					Bounds b = GetWorldBounds( obj );

					Vector3 testExtents = b.extents * 0.9f;
					bool overlaps = false;

					int count;
					while ( true )
					{
						count = Physics.OverlapBoxNonAlloc( b.center, testExtents, overlapBuffer, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore );
						if ( count < overlapBuffer.Length )
						{
							break;
						}
						overlapBuffer = new Collider[ overlapBuffer.Length * 2 ];
					}

					for ( int i = 0; i < count; i++ )
					{
						var c = overlapBuffer[ i ];
						if ( c != null && c != hit.collider && c.transform != obj.transform && !c.transform.IsChildOf( obj.transform ) )
						{
							overlaps = true;
							break;
						}
					}

					if ( overlaps )
					{
						Undo.DestroyObjectImmediate( obj );
						continue;
					}

					// Ensure the new prefab has collision so this and future spawns' OverlapBox
					// see it. Kept permanent (NOT tracked in tempColliders) so we don't re-cook
					// it on every subsequent Spawn — the pre-pass also skips spawned prefabs.
					// Prefer a MeshCollider over a BoxCollider when a mesh is available.
					if ( obj.GetComponentInChildren<Collider>() == null )
					{
						var mf = obj.GetComponentInChildren<MeshFilter>();
						if ( mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable )
						{
							var mc = mf.gameObject.AddComponent<MeshCollider>();
							mc.sharedMesh = mf.sharedMesh;
							mc.convex = false;
						}
						else
						{
							var bc = obj.AddComponent<BoxCollider>();
							bc.center = obj.transform.InverseTransformPoint( b.center );

							Vector3 ls = obj.transform.lossyScale;
							bc.size = new Vector3(
									b.size.x / Mathf.Max( 0.0001f, Mathf.Abs( ls.x ) ),
									b.size.y / Mathf.Max( 0.0001f, Mathf.Abs( ls.y ) ),
									b.size.z / Mathf.Max( 0.0001f, Mathf.Abs( ls.z ) ) );
						}
					}

					Physics.SyncTransforms();
				}

				settings.spawned.Add( obj );
			}

			foreach ( var c in tempColliders )
			{
				if ( c != null )
				{
					Object.DestroyImmediate( c );
				}
			}

			SaveSettings();
		}

		// Removes only the objects this tool spawned (tracked in settings.spawned).
		static void Clear()
		{
			for ( int i = settings.spawned.Count - 1; i >= 0; i-- )
			{
				var go = settings.spawned[ i ];
				if ( go != null )
				{
					Undo.DestroyObjectImmediate( go );
				}
			}

			settings.spawned.Clear();

			SaveSettings();
		}

		// Fast Poisson-disc sampling in 2D (X/Z plane of the volume).
		static List<Vector2> GeneratePoissonPoints( float radius, Vector2 regionSize, int rejectionSamples )
		{
			float cellSize = radius / Mathf.Sqrt( 2 );

			int[,] grid = new int[ Mathf.CeilToInt( regionSize.x / cellSize ), Mathf.CeilToInt( regionSize.y / cellSize ) ];

			List<Vector2> points = new();
			List<Vector2> spawnPoints = new() { regionSize / 2 };

			while ( spawnPoints.Count > 0 )
			{
				int spawnIndex = Random.Range( 0, spawnPoints.Count );
				Vector2 spawnCentre = spawnPoints[ spawnIndex ];
				bool accepted = false;

				for ( int i = 0; i < rejectionSamples; i++ )
				{
					float angle = Random.value * Mathf.PI * 2f;
					Vector2 dir = new Vector2( Mathf.Sin( angle ), Mathf.Cos( angle ) );

					Vector2 candidate = spawnCentre + dir * Random.Range( radius, radius * 2f );

					if ( IsValid( candidate, regionSize, cellSize, radius, points, grid ) )
					{
						points.Add( candidate );
						spawnPoints.Add( candidate );

						int x = ( int )( candidate.x / cellSize );
						int y = ( int )( candidate.y / cellSize );

						grid[ x, y ] = points.Count;
						accepted = true;
						break;
					}
				}

				if ( !accepted )
				{
					spawnPoints.RemoveAt( spawnIndex );
				}
			}
			return points;
		}

		// Candidate point passes if it's inside the region and no neighbor in the grid is within radius.
		static bool IsValid( Vector2 candidate, Vector2 regionSize, float cellSize, float radius, List<Vector2> points, int[,] grid )
		{
			if ( candidate.x < 0 || candidate.x >= regionSize.x || candidate.y < 0 || candidate.y >= regionSize.y )
			{
				return false;
			}

			int cellX = ( int )( candidate.x / cellSize );
			int cellY = ( int )( candidate.y / cellSize );

			int searchStartX = Mathf.Max( 0, cellX - 2 );
			int searchEndX = Mathf.Min( cellX + 2, grid.GetLength( 0 ) - 1 );

			int searchStartY = Mathf.Max( 0, cellY - 2 );
			int searchEndY = Mathf.Min( cellY + 2, grid.GetLength( 1 ) - 1 );

			for ( int x = searchStartX; x <= searchEndX; x++ )
			{
				for ( int y = searchStartY; y <= searchEndY; y++ )
				{
					int pointIndex = grid[ x, y ] - 1;
					if ( pointIndex != -1 )
					{
						float sqrDst = ( candidate - points[ pointIndex ] ).sqrMagnitude;
						if ( sqrDst < radius * radius )
						{
							return false;
						}
					}
				}
			}
			return true;
		}

		// Unity's MaskField uses the compact named-layers array as its display source, but the
		// returned bit indices don't correspond to actual layer numbers. Convert through the
		// concatenated-mask helpers so what we store in settings is a real LayerMask.
		static LayerMask LayerMaskField(
			GUIContent label,
			LayerMask mask )
		{
			int displayMask = InternalEditorUtility.LayerMaskToConcatenatedLayersMask( mask );
			int newDisplayMask = EditorGUILayout.MaskField( label, displayMask, InternalEditorUtility.layers );
			return InternalEditorUtility.ConcatenatedLayersMaskToLayerMask( newDisplayMask );
		}

		// Persistence is per-user, per-machine via EditorPrefs (shared across all Unity projects).
		static void SaveSettings()
		{
			string json = EditorJsonUtility.ToJson( settings );
			EditorPrefs.SetString( PrefsKey, json );
		}

		static void LoadSettings()
		{
			if ( EditorPrefs.HasKey( PrefsKey ) )
			{
				string json = EditorPrefs.GetString( PrefsKey );
				EditorJsonUtility.FromJsonOverwrite( json, settings );
			}
		}

		// Scene-view drawing + the volume's "fake selection" model: click the icon to take over
		// the gizmo (and clear Unity's selection), click elsewhere to release it.
		static void DuringSceneGUI( SceneView sceneView )
		{
			if ( settings.hideSpawnVolume || !everOpened )
			{
				return;
			}

			if ( settings.shadeSpawnVolume )
			{
				DrawShadedVolume( settings.center, settings.size );
			}

			Handles.color = VolumeColor;
			Handles.DrawWireCube( settings.center, settings.size );

			LoadVolumeIcon();

			Event e = Event.current;

			bool mouseDownBefore = e.type == EventType.MouseDown && e.button == 0 && !e.alt && !e.control && !e.command && !e.shift;
			bool iconClicked = false;

			if ( volumeIcon != null )
			{
				Handles.BeginGUI();

				Vector2 gui = HandleUtility.WorldToGUIPoint( settings.center );

				float iconSize = volumeSelected ? 40f : 32f;
				Rect iconRect = new Rect( gui.x - iconSize * 0.5f, gui.y - iconSize * 0.5f, iconSize, iconSize );

				if ( volumeSelected )
				{
					GUI.DrawTexture( iconRect, volumeIcon, ScaleMode.ScaleToFit );
				}
				else if ( GUI.Button( iconRect, volumeIcon, GUIStyle.none ) )
				{
					iconClicked = true;

					if ( Selection.activeObject != null )
					{
						Selection.activeObject = null;
					}

					volumeSelected = true;
					GUI.changed = true;
				}

				Handles.EndGUI();
			}

			if ( volumeSelected )
			{
				EnsureUndoState();

				if ( Tools.current == Tool.Scale )
				{
					EditorGUI.BeginChangeCheck();

					Vector3 newSize = Handles.ScaleHandle( settings.size, settings.center, Quaternion.identity, HandleUtility.GetHandleSize( settings.center ) );

					if ( EditorGUI.EndChangeCheck() )
					{
						undoState.size = settings.size;
						Undo.RecordObject( undoState, "Scale Spawn Volume" );
						undoState.size = new Vector3( Mathf.Max( 0.01f, newSize.x ), Mathf.Max( 0.01f, newSize.y ), Mathf.Max( 0.01f, newSize.z ) );
						settings.size = undoState.size;
						SaveSettings();

						if ( instance != null )
						{
							instance.Repaint();
						}
					}
				}
				else
				{
					EditorGUI.BeginChangeCheck();
					Vector3 newPos = Handles.PositionHandle( settings.center, Quaternion.identity );
					if ( EditorGUI.EndChangeCheck() )
					{
						undoState.center = settings.center;
						Undo.RecordObject( undoState, "Move Spawn Volume" );
						undoState.center = newPos;
						settings.center = newPos;
						SaveSettings();

						if ( instance != null )
						{
							instance.Repaint();
						}
					}
				}
			}

			if ( volumeSelected && !iconClicked && mouseDownBefore && e.type == EventType.MouseDown )
			{
				volumeSelected = false;
				SceneView.RepaintAll();
			}

			if ( volumeSelected && e.type == EventType.KeyDown && e.keyCode == KeyCode.F )
			{
				sceneView.Frame( new Bounds( settings.center, settings.size ), false );
				e.Use();
			}
		}
	}
}