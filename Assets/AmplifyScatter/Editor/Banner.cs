// Amplify Scatter FREE
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using UnityEditorInternal;

namespace AmplifyScatter
{
	internal class AffiliateAbout : EditorWindow
	{
		public static void Init()
		{
			AffiliateAbout window = ( AffiliateAbout )GetWindow( typeof( AffiliateAbout ), true, "Affiliate Link Information" );
			window.minSize = new Vector2( 400, 130 );
			window.maxSize = new Vector2( 400, 130 );
			window.Show();
		}

		public void OnGUI()
		{
			GUIStyle lableStyle = new GUIStyle( GUI.skin.label );
			lableStyle.wordWrap = true;
			lableStyle.alignment = TextAnchor.MiddleCenter;
			lableStyle.richText = true;

			GUIStyle textLink = new GUIStyle( GUI.skin.label );
			textLink.normal.textColor = EditorGUIUtility.isProSkin ? AffiliateBanner.LinkColorInPro : AffiliateBanner.LinkColorInPersonal;
			GUILayout.Space( 16 );
			GUILayout.Label( "By using our Publisher Affiliate link we receive a small commission for all Asset Store purchases done in the following 7 days. If you want to support free content such as the one you are using, please consider using our unique Publisher link. Thank You!", lableStyle );
			GUILayout.Space( 16 );
			GUILayout.BeginHorizontal();
			if ( GUILayout.Button( "Learn more about the Unity Affiliate program <color=#4C7DFFFF>here</color>", lableStyle ) )
			{
				Help.BrowseURL( "https://unity3d.com/affiliates" );
			}
			GUILayout.EndHorizontal();
		}
	}

	[Serializable]
	internal class AffiliateBannerInfo
	{
		public string imageURL;
		public string message;
		public string affiliateLink;

		public static AffiliateBannerInfo CreateFromJSON( string jsonString )
		{
			return JsonUtility.FromJson<AffiliateBannerInfo>( jsonString );
		}

		public AffiliateBannerInfo( string i, string m, string a )
		{
			imageURL = i;
			message = m;
			affiliateLink = a;
		}
	}

	public class AffiliateBanner
	{
		const string BannerOfflineGUID = "dd17fcfd0f1d6b540a22a6b89c9e8b06";
		const string JsonURL = "http://amplify.pt/Banner/AmplifyScatter.json";

		AffiliateBannerInfo info = new AffiliateBannerInfo(
			"https://amplify.pt/Banner/Banner_Fxaa.jpg",
			"Like Amplify Scatter? Check out Amplify Shader Editor, award-winning node-based shader editor for Unity!",
			"https://assetstore.unity.com/packages/tools/visual-scripting/amplify-shader-editor-68570"
		);

		GUIStyle linkStyle;
		GUIStyle labelStyle;
		GUIStyle textLink;
		GUIContent button = new GUIContent( "" );

		bool initialized = false;
		bool imageLoaded = false;

		int maxHeight = 105;
		float currentHeight = 105;
		float imageRatio = 0.23863f;

		Texture2D fetchedImage = null;
		Texture2D defaultImage;

		public static Color LinkColorInPro = new Color( 0.3f, 0.5f, 1 );
		public static Color LinkColorInPersonal = new Color( 0.1f, 0.3f, 0.8f );

		public void Draw( EditorWindow window )
		{
			if ( !initialized )
			{
				initialized = true;
				linkStyle = new GUIStyle( GUIStyle.none );
				linkStyle.alignment = TextAnchor.UpperCenter;

				labelStyle = new GUIStyle( GUI.skin.label );

				labelStyle.wordWrap = true;
				labelStyle.alignment = TextAnchor.MiddleLeft;
				defaultImage = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( BannerOfflineGUID ) );
				imageRatio = ( float )defaultImage.height / ( float )defaultImage.width;
				maxHeight = defaultImage.height;

				textLink = new GUIStyle( GUI.skin.label );
				textLink.normal.textColor = Color.white;
				textLink.alignment = TextAnchor.MiddleCenter;
				textLink.fontSize = 9;
			}

			if ( fetchedImage != null )
			{
				button.image = fetchedImage;
				imageRatio = ( float )fetchedImage.height / ( float )fetchedImage.width;
				maxHeight = fetchedImage.height;
			}
			else
			{
				button.image = defaultImage;
				imageRatio = ( float )defaultImage.height / ( float )defaultImage.width;
				maxHeight = defaultImage.height;

				if ( !imageLoaded )
				{
					imageLoaded = true;

					StartBackgroundTask( StartRequest( window, JsonURL, ( window, request ) =>
					{
						info = AffiliateBannerInfo.CreateFromJSON( request.downloadHandler.text );
						window.Repaint();

						StartBackgroundTask( StartTextureRequest( window, info.imageURL, ( window, request2 ) =>
						{
							Texture2D texture = DownloadHandlerTexture.GetContent( request2 );
							if ( texture != null )
							{
								fetchedImage = texture;
								window.Repaint();
							}
						} ) );
					} ) );
				}
			}

			EditorGUILayout.BeginVertical( "ObjectFieldThumb" );
			{
				currentHeight = Mathf.Min( maxHeight, ( EditorGUIUtility.currentViewWidth - 30 ) * imageRatio );
				Rect buttonRect = EditorGUILayout.GetControlRect( false, currentHeight );
				EditorGUIUtility.AddCursorRect( buttonRect, MouseCursor.Link );
				if ( GUI.Button( buttonRect, button, linkStyle ) )
				{
					Help.BrowseURL( info.affiliateLink );
				}
				GUILayout.BeginHorizontal();
				GUILayout.Label( info.message, labelStyle );
				GUILayout.FlexibleSpace();
				GUILayout.BeginVertical();
				if ( GUILayout.Button( "Learn More", GUILayout.Height( 25 ) ) )
				{
					Help.BrowseURL( info.affiliateLink );
				}
				Color cache = GUI.color;
				GUI.color = EditorGUIUtility.isProSkin ? LinkColorInPro : LinkColorInPersonal;
				if ( GUILayout.Button( "Affiliate Link", textLink ) )
				{
					AffiliateAbout.Init();
				}
				GUI.color = cache;
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();
		}

		private static string SanitizeURL( string url )
		{
			return url.Replace( "http://", "https://" );
		}

		public delegate void SuccessCall( EditorWindow window, UnityWebRequest request );

		public static IEnumerator StartRequest( EditorWindow window, string url, SuccessCall success = null )
		{
			using ( var request = UnityWebRequest.Get( SanitizeURL( url ) ) )
			{
				yield return request.SendWebRequest();
				while ( request.isDone == false )
				{
					yield return null;
				}
				if ( success != null && request.result == UnityWebRequest.Result.Success )
				{
					success( window, request );
				}
			}
		}

		public static IEnumerator StartTextureRequest( EditorWindow window, string url, SuccessCall success = null )
		{
			using ( UnityWebRequest request = UnityWebRequestTexture.GetTexture( SanitizeURL( url ) ) )
			{
				yield return request.SendWebRequest();
				while ( request.isDone == false )
				{
					yield return null;
				}
				if ( success != null && request.result == UnityWebRequest.Result.Success )
				{
					success( window, request );
				}
			}
		}

		public static void StartBackgroundTask( IEnumerator update, Action end = null )
		{
			EditorApplication.CallbackFunction closureCallback = null;
			closureCallback = () =>
			{
				try
				{
					if ( update.MoveNext() == false )
					{
						if ( end != null )
						{
							end();
						}
						EditorApplication.update -= closureCallback;
					}
				}
				catch ( Exception ex )
				{
					if ( end != null )
					{
						end();
					}
					Debug.LogException( ex );
					EditorApplication.update -= closureCallback;
				}
			};
			EditorApplication.update += closureCallback;
		}
	}
}