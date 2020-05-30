using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using PolyToolkit;
using UnityEngine.Events;

public class DD_PolyAR : MonoBehaviour {

	#region protected variables
	// a list of - key, value pairs ordered asset.name (ID), asset.displayName (name)
	[SerializeField] public List<KeyValuePair<string, string>> asset_id_name_list;
	[SerializeField] public List<KeyValuePair<string, Texture2D>> asset_thumbnail_list;
	[SerializeField] Transform m_cameraTransform; 
	int resultCount = 20;
	public GameObject importedObject;
	[SerializeField] ARPlacementIndicator ar_tap_to_place_object;
	[Header("Unity Events")]
	public UnityEvent onPolyAssetsLoaded = new UnityEvent();
	public UnityEvent onPolyThumbLoaded = new UnityEvent();
	public UnityEvent onAssetImported = new UnityEvent();
	public List<string> artist_names;
	public string featured_artist_name;
	public float distMultiplier = 1.5f;
	/* [SerializeField] ObjectControls objectManager; */
	#endregion

	#region public variables

	public string searchKeyword = "tree";

	#endregion

	#region main methods

	void Start () 
	{
		m_cameraTransform = GameObject.FindWithTag("MainCamera").transform;

		asset_id_name_list = new List<KeyValuePair<string, string>>();
		asset_thumbnail_list = new List<KeyValuePair<string, Texture2D>>();
		Debug.Log("Requesting List of Assets...");
		// list featured assets
		PolyApi.ListAssets(PolyListAssetsRequest.Featured(), FeaturedAssetListCallback);
		//PolyAssetSearchQuery(searchKeyword);
	}

	#endregion


	#region helper methods

	void FeaturedAssetListCallback(PolyStatusOr<PolyListAssetsResult> result)
	{
		if (!result.Ok)
		{
			// Handle error.
			Debug.LogError("Failed to import featured list. :( Reason: " + result.Status);
			return;
		}
		// Success. result.Value is a PolyListAssetsResult and
		// result.Value.assets is a list of PolyAssets.
		// asset_id_name_list
		asset_id_name_list.AddRange(
			result.Value.assets.Take(resultCount).Select((asset) =>
			{
				Debug.Log(asset.displayName);
				PolyApi.FetchThumbnail(asset, GetThumbnailCallback);
				return new KeyValuePair<string, string>(asset.name, asset.displayName);
			}).AsParallel()
		);

		asset_id_name_list.Sort((x, y) => string.Compare(x.Key, y.Key, StringComparison.Ordinal));

		if(onPolyAssetsLoaded != null)
			onPolyAssetsLoaded.Invoke();
	}

	private void GetThumbnailCallback(PolyAsset asset, PolyStatus status)
	{
		if (!status.ok)
		{
			Debug.LogError("Failed to import thumbnail. :( Reason: " + status);
			return;
		}
		asset_thumbnail_list.Add(new KeyValuePair<string, Texture2D>(asset.name, asset.thumbnailTexture));

		if (asset_thumbnail_list.Count == resultCount)
		{
			asset_thumbnail_list.Sort((x, y) => string.Compare(x.Key, y.Key, StringComparison.Ordinal));

			if (onPolyThumbLoaded != null)
			{
				onPolyThumbLoaded.Invoke();
			}
		}

	}

	private void GetAssetCallback(PolyStatusOr<PolyAsset> result)
	{
		if (!result.Ok)
		{
			Debug.LogError("Failed to get assets. Reason: " + result.Status);
			return;
		}
		Debug.Log("Successfully got asset!");

		// Set the import options.
		PolyImportOptions options = PolyImportOptions.Default();
		// We want to rescale the imported mesh to a specific size.
		options.rescalingMode = PolyImportOptions.RescalingMode.FIT;
		// The specific size we want assets rescaled to (fit in a 5x5x5 box):
		options.desiredSize = 0.5f;
		// We want the imported assets to be recentered such that their centroid coincides with the origin:
		options.recenter = true;

		//statusText.text = "Importing...";
		PolyApi.Import(result.Value, options, ImportAssetCallback);
	}

	// Callback invoked when an asset has just been imported.
	private void ImportAssetCallback(PolyAsset asset, PolyStatusOr<PolyImportResult> result)
	{
		if (!result.Ok)
		{
			Debug.LogError("Failed to import asset. :( Reason: " + result.Status);
			//statusText.text = "ERROR: Import failed: " + result.Status;
			return;
		}
		Debug.Log("Successfully imported asset!");

		// Show attribution (asset title and author).
		//statusText.text = asset.displayName + "\nby " + asset.authorName;

		// Here, you would place your object where you want it in your scene, and add any
		// behaviors to it as needed by your app. As an example, let's just make it
		// slowly rotate:
		float h = result.Value.gameObject.transform.GetChild(0).GetComponent<MeshRenderer>().bounds.extents.y;
		for (int i = 0; i < result.Value.gameObject.transform.childCount; i++)
		{
			if (result.Value.gameObject.transform.GetChild(i).GetComponent<MeshRenderer>() != null)
			{
				if (result.Value.gameObject.transform.GetChild(i).GetComponent<MeshRenderer>().bounds.extents.y > h)
					h = result.Value.gameObject.transform.GetChild(i).GetComponent<MeshRenderer>().bounds.extents.y;
			}
		}

		float fov = Camera.main.fieldOfView;
		float angle = Mathf.Deg2Rad * (fov / 2);
		float dist = h / Mathf.Atan(angle);
		Debug.Log("Distance " + dist);

		Vector3 objPosition = m_cameraTransform.position + (m_cameraTransform.forward.normalized * dist * distMultiplier); //place sphere 10cm in front of device
		result.Value.gameObject.transform.position = objPosition;

		importedObject = result.Value.gameObject;
		importedObject.name = asset.displayName;

		importedObject.transform.LookAt(Camera.main.transform);
		importedObject.transform.eulerAngles = new Vector3(0, importedObject.transform.eulerAngles.y + 180f, 0);

		featured_artist_name = asset.authorName;
		artist_names.Add(featured_artist_name);

		if (onAssetImported != null)
		{
			onAssetImported.Invoke();
		}
	}

	// get single asset w/ ID
	public void GetSingleAssetWithID(string modelId)
	{
		PolyApi.GetAsset(modelId, GetAssetCallback);
	}

	public void PolyAssetSearchQuery(string searchKey)
	{
		PolyListAssetsRequest req = new PolyListAssetsRequest();
		// Search by keyword:
		req.keywords = searchKey;
		// Only curated assets:
		req.curated = true;
		// Limit complexity to simple low poly.
		req.maxComplexity = PolyMaxComplexityFilter.COMPLEX;
		// Only Blocks objects.
		//req.formatFilter = PolyFormatFilter.BLOCKS;
		// Order from best to worst.
		req.orderBy = PolyOrderBy.BEST;
		// Up to 20 results per page.
		req.pageSize = 20;
		// Send the request.
		PolyApi.ListAssets(req, SearchAssetListCallback);
	}

	void SearchAssetListCallback(PolyStatusOr<PolyListAssetsResult> result)
	{
		if (!result.Ok)
		{
			// Handle error.
			Debug.LogError("Failed to import featured list. :( Reason: " + result.Status);
			return;
		}

		asset_id_name_list.Clear();
		asset_thumbnail_list.Clear();
		resultCount = result.Value.assets.Count;

		// Success. result.Value is a PolyListAssetsResult and
		// result.Value.assets is a list of PolyAssets.
		asset_id_name_list.AddRange(
		result.Value.assets.Select((asset) =>
			{
				Debug.Log(asset.displayName);
				PolyApi.FetchThumbnail(asset, GetThumbnailCallback);
				return new KeyValuePair<string, string>(asset.name, asset.displayName);
			}).AsParallel()
		);

		Debug.Log("RESULT COUNT: " + result.Value.assets.Count);

		asset_id_name_list.Sort((x, y) => string.Compare(x.Key, y.Key, StringComparison.Ordinal));

		if (onPolyAssetsLoaded != null)
			onPolyAssetsLoaded.Invoke();
	}

	#endregion

}
