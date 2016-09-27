using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

#if UNITY_ADS
using UnityEngine.Advertisements;
#endif

public class UIController : MonoBehaviour
{
	public UnityEngine.UI.Button ShowDefaultAdButton;
	public UnityEngine.UI.Button ShowRewardedAdButton;
	public UnityEngine.UI.Button ShowCoroutineAdButton;
	public UnityEngine.UI.Text LogText;
	public GameObject ConfigPanel;
	public UnityEngine.UI.InputField RewardedAdPlacementIdInput;
	public UnityEngine.UI.Toggle DebugModeToggle;
	public UnityEngine.UI.Text ToggleAudioButtonText;
	public GameObject AdvancedModePanel;
	public UnityEngine.UI.Toggle AdvancedModeToggle;
	public UnityEngine.UI.Text FPS;
	public LoadTesting LoadTesting; // need this to call coroutines

	private static UIController instance = null;
	private float adsInitializeTime;
	private bool adsInitialized = false;

	private const string GameIdPlayerPrefsKey = "GameId";
	private const string RewardedAdPlacementIdPlayerPrefsKey = "RewardedAdPlacementId";

	void Start ()
	{
		InvokeRepeating ("UpdateFPSText", 1, 1);
		ConfigPanel.SetActive (false);
		AdvancedModePanel.SetActive (AdvancedModeToggle.isOn);
#if !UNITY_ADS
		Log ("Ads not enabled for this platform. Enable from Services window and make sure you have selected either Android or iOS");
		UpdateUI ();
		InitializeButton.interactable = false;
		ShowCoroutineAdButton.interactable = false;
		TestModeToggle.interactable = false;
		DebugModeToggle.interactable = false;
#else
		Log (string.Format ("Unity version: {0}", Application.unityVersion));

		ConfigPanel.SetActive (false);

		if (PlayerPrefs.HasKey (RewardedAdPlacementIdPlayerPrefsKey))
		{
			RewardedAdPlacementIdInput.text = PlayerPrefs.GetString (RewardedAdPlacementIdPlayerPrefsKey);
		}
		else
		{
			RewardedAdPlacementIdInput.text = "rewardedVideo";
		}
		DebugModeToggleClicked ();
#endif
	}

	public static UIController Instance
	{
		get
		{
			if (instance == null)
			{
				instance = FindObjectOfType<UIController> ();
			}
			return instance;
		}
	}

	public void Log (string message)
	{
		string text = string.Format ("{0:HH:mm:ss} {1}", DateTime.Now, message);
		Debug.Log ("=== " + text + " ===");
		LogText.text = string.Format ("{0}\n{1}", text, LogText.text);
	}

	public void ShowConfigButtonClicked ()
	{
		ConfigPanel.SetActive (true);
	}

	public void HideConfigButtonClicked ()
	{
		ConfigPanel.SetActive (false);
	}

	public void QuitButtonClicked ()
	{
		Application.Quit ();
	}

	public void ToggleAudio ()
	{
		var audio = this.GetComponent<AudioSource> ();

		if (audio.isPlaying)
		{
			audio.Stop ();
			ToggleAudioButtonText.text = "Play audio";
		}
		else
		{
			audio.Play ();
			ToggleAudioButtonText.text = "Mute audio";
		}
	}

	public void AllocateMemory()
	{
		LoadTesting.Allocate100MB ();
	}

	public void ToggleCPULoad()
	{
		LoadTesting.ToggleCPULoad();
	}

	public void DebugModeToggleClicked ()
	{
#if UNITY_ADS
#if UNITY_5_5
		// TODO: Doesn't work with SDK 1.5 - SDK specific conditional could solve this?
		Advertisement.debugMode = DebugModeToggle.isOn;
		Log ("Debug mode: " + Advertisement.debugMode);
#endif
#endif
	}

	public void AdvancedModeToggleClicked ()
	{
		AdvancedModePanel.SetActive (AdvancedModeToggle.isOn);
	}

	private void UpdateUI ()
	{
		ShowDefaultAdButton.interactable = adsInitialized && DefaultAdPlacementReady ();
		ShowRewardedAdButton.interactable = adsInitialized && (RewardedAdPlacementReady () != null);
	}

	private bool DefaultAdPlacementReady ()
	{
#if !UNITY_ADS
		return false;
#else
		return Advertisement.IsReady ();
#endif
	}

	private string RewardedAdPlacementReady ()
	{
#if !UNITY_ADS
		return null;
#else
		// default rewarded placement id has changed over time, check each of these
		string[] placementIds = { RewardedAdPlacementIdInput.text, "rewardedVideo", "rewardedVideoZone", "incentivizedZone" };

		foreach (var placementId in placementIds)
		{
			if (Advertisement.IsReady (placementId))
				return placementId;
		}

		return null;
#endif
	}

	void Update ()
	{
#if UNITY_ADS
		if (!adsInitialized && Advertisement.IsReady ())
			adsInitialized = true; // has ads been available at some point? used to see if we managed to initialize correctly
#endif

		UpdateUI ();
	}

	private void UpdateFPSText()
	{
		FPS.text = string.Format("FPS: {0:0.0}", LoadTesting.FPS);
	}

#if UNITY_ADS
	public void InitializeAdsButtonClicked ()
	{
		adsInitializeTime = Time.time;
		Invoke ("CheckForAdsInitialized", 5);
	}

	private void CheckForAdsInitialized()
	{
		if (!adsInitialized)
		{
			float timeSinceInitialize = Time.time - adsInitializeTime;
			if (timeSinceInitialize > 30)
			{
				Log("Failed to initialize ads withing 30 seconds. Please verify you entered correct game id and placement ids and/or check device log for additional information");
				return;
			}

			Log (string.Format("Initializing - {0:#} secs...", timeSinceInitialize));
			Invoke("CheckForAdsInitialized", 5);
		}
	}

	public void ShowDefaultAdButtonClicked ()
	{
		ShowAd ();  // we want to make sure this also works, as game devs might typically show ads this way
	}

	public void ShowRewardedAdButtonClicked ()
	{
		string rewardedPlacementId = RewardedAdPlacementReady ();

		if (string.IsNullOrEmpty (rewardedPlacementId))
		{
			Log ("Rewarded ad not ready");
		}
		else
		{
			ShowAd (rewardedPlacementId);
		}
	}

	public void ShowCoroutineAdButtonClicked ()
	{
		StopAllCoroutines();
		StartCoroutine(ShowAdCouroutine());
	}

	internal IEnumerator ShowAdCouroutine()
	{
		float startTime = Time.time;
		while (!Advertisement.IsReady())
		{
			float time = Time.time - startTime;
			yield return new WaitForSeconds(0.5f);

			if (time > 30.0f)
			{
				Log ("Failed to initialize ads, please verify that you entered correct game id");
				yield break;
			}
		}

		ShowOptions options = new ShowOptions();
		options.resultCallback = ShowAdResultCallback;
		Advertisement.Show(null, options);
	}

	private void InitializeAds (string gameId, bool testMode)
	{
		if (!Advertisement.isSupported)
		{
			Log ("Ads not supported on this platform");
			return;
		}

		if ((gameId == null) || (gameId.Trim ().Length == 0))
		{
			Log ("Please provide a game id");
			return;
		}

		Log (string.Format ("Initializing ads for game id {0}...", gameId));
		Advertisement.Initialize (gameId, testMode);
	}

	private void ShowAd ()
	{
		ShowAd (null);
	}

	private void ShowAd (string placementId)
	{
		if (!Advertisement.isInitialized)
		{
			Log ("Ads hasn't been initialized yet. Cannot show ad");
			return;
		}

		if (!Advertisement.IsReady (placementId))
		{
			if (placementId == null)
			{
				Log ("Ads not ready for default placement. Please wait a few seconds and try again");
			}
			else
			{
				Log (string.Format("Ads not ready for placement '{0}'. Please wait a few seconds and try again", placementId));
			}

			return;
		}

		ShowOptions options = new ShowOptions
		{
			resultCallback = ShowAdResultCallback
		};

		if (placementId == null)
		{
			Log ("Showing ad for default placement");
		}
		else
		{
			Log (string.Format ("Showing ad for placement '{0}'", placementId));
		}

		Advertisement.Show (placementId, options);
	}

	private void ShowAdResultCallback(ShowResult result)
	{
		Log ("Ad completed with result: " + result);
	}
#endif
}
