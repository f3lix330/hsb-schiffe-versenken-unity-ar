using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class CloudAnchorInterface : MonoBehaviour
{
    [Header("AR")]
    public ARRaycastManager raycastManager;
    public ARAnchorManager anchorManager;
    public ARCoreExtensions arCoreExtensions;

    [Header("Placement")]
    public Camera arCamera;

    private static readonly List<ARRaycastHit> _hits = new();
    private ARAnchor _currentLocalAnchor;
    private ARCloudAnchor _currentCloudAnchor;

    private const float SessionTrackingTimeoutSeconds = 10f;
    private const float CloudOpTimeoutSeconds = 30f;
    [FormerlySerializedAs("canvasManager")]
    public GameObject canvasPrefab;
    private GameObject _canvasInstance;
    
    [Header("UI (Legacy uGUI)")]
    public Text statusText;
    public Button hostButton;
    public Button resolveButton;
    
    private NetworkCommunicationHandler networkCommunicationHandler;

    private void Awake()
    {
        if (!raycastManager || !anchorManager || !arCoreExtensions || !arCamera)
        {
            Debug.LogError("CloudAnchorInterface is missing references.");
            enabled = false;
            return;
        }

        if (canvasPrefab != null && canvasPrefab.scene.IsValid())
        {
            Debug.LogWarning("CloudAnchorInterface: canvasPrefab points to a scene object. Disable it and use a prefab asset instead.");
            canvasPrefab.SetActive(false);
        }
        else if (canvasPrefab != null)
        {
            var existingCanvas = GameObject.Find(canvasPrefab.name);
            if (existingCanvas != null)
            {
                existingCanvas.SetActive(false);
            }
        }
        hostButton.onClick.RemoveAllListeners();
        resolveButton.onClick.RemoveAllListeners();

        hostButton.onClick.AddListener(CreateAndHostAnchor);
        resolveButton.onClick.AddListener(RetrieveAnchor);
    }

    private void Start()
    {
        networkCommunicationHandler = GameObject.Find("NetworkCommunicationHandler").GetComponent<NetworkCommunicationHandler>();
    }

    public async void CreateAndHostAnchor()
    {
        try
        {
            ResetAnchor();

            if (!TryGetPlacementPoseFromScreenCenter(out Pose pose))
            {
                SetStatus("No plane hit. Move phone and aim at a surface.");
                return;
            }
            
            
            var addResult = await anchorManager.TryAddAnchorAsync(pose);
            if (!addResult.status.IsSuccess() || addResult.value == null)
            {
                SetStatus($"Failed to create local anchor: {addResult.status}");
                return;
            }
            _currentLocalAnchor = addResult.value;

            _currentCloudAnchor = anchorManager.HostCloudAnchor(_currentLocalAnchor);
            if (_currentCloudAnchor == null)
            {
                SetStatus("HostCloudAnchor returned null.");
                return;
            }

            StartCoroutine(WaitForHostingResult(_currentCloudAnchor));
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public async void RetrieveAnchor()
    {
        string trimmedId = await networkCommunicationHandler.GetCurrentAnchorId();
        Debug.Log("AnchorId: " +  trimmedId);
        if (string.IsNullOrEmpty(trimmedId))
        {
            SetStatus("Cloud anchor id is empty.");
            return;
        }

        ResetAnchor();

        ARCloudAnchor resolved = anchorManager.ResolveCloudAnchorId(trimmedId);
        if (resolved == null)
        {
            SetStatus("ResolveCloudAnchorId returned null.");
            return;
        }

        _currentCloudAnchor = resolved;
        StartCoroutine(WaitForResolveResult(_currentCloudAnchor, canvasPrefab));
    }

    public void ResetAnchor()
    {
        if (_canvasInstance != null)
        {
            Destroy(_canvasInstance);
            _canvasInstance = null;
        }

        if (_currentCloudAnchor != null)
        {
            Destroy(_currentCloudAnchor.gameObject);
            _currentCloudAnchor = null;
        }

        if (_currentLocalAnchor != null)
        {
            Destroy(_currentLocalAnchor.gameObject);
            _currentLocalAnchor = null;
        }
    }

    private IEnumerator WaitForHostingResult(ARCloudAnchor cloudAnchor)
    {
        float t = SessionTrackingTimeoutSeconds;
        while (ARSession.state != ARSessionState.SessionTracking && t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        if (ARSession.state != ARSessionState.SessionTracking)
        {
            SetStatus($"AR not tracking (state: {ARSession.state})");
            yield break;
        }

        float op = CloudOpTimeoutSeconds;
        while (cloudAnchor.cloudAnchorState == CloudAnchorState.TaskInProgress && op > 0f)
        {
            op -= Time.deltaTime;
            yield return null;
        }

        if (cloudAnchor.cloudAnchorState == CloudAnchorState.TaskInProgress)
        {
            SetStatus("Hosting timed out.");
            yield break;
        }

        Debug.Log("CloudAnchorState: " + cloudAnchor.cloudAnchorState);
        if (cloudAnchor.cloudAnchorState == CloudAnchorState.Success)
        {
            Config.currentAnchorId = cloudAnchor.cloudAnchorId;
            SetStatus($"Anchor hosted: {Config.currentAnchorId}");

            Pose p = new Pose(cloudAnchor.transform.position, cloudAnchor.transform.rotation);
            _canvasInstance = CreateOrSpawnVisual(p, canvasPrefab);
            if (_canvasInstance != null)
            {
                _canvasInstance.transform.SetParent(cloudAnchor.transform, true);
            }
        }
        else
        {
            SetStatus($"Hosting anchor failed: {cloudAnchor.cloudAnchorState}");
        }
    }

    private IEnumerator WaitForResolveResult(ARCloudAnchor cloudAnchor, GameObject visualPrefab)
    {
        Debug.Log("CloudAnchorState: " + cloudAnchor.cloudAnchorState);
        float t = SessionTrackingTimeoutSeconds;
        while (ARSession.state != ARSessionState.SessionTracking && t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        if (ARSession.state != ARSessionState.SessionTracking)
        {
            SetStatus($"AR not tracking (state: {ARSession.state})");
            yield break;
        }

        float op = 2;
        while (cloudAnchor.cloudAnchorState == CloudAnchorState.TaskInProgress && op > 0f)
        {
            op -= Time.deltaTime;
            yield return null;
        }

        if (cloudAnchor.cloudAnchorState == CloudAnchorState.Success)
        {
            Pose p = new Pose(cloudAnchor.transform.position, cloudAnchor.transform.rotation);
            _canvasInstance = CreateOrSpawnVisual(p, visualPrefab);

            if (_canvasInstance != null)
            {
                _canvasInstance.transform.SetParent(cloudAnchor.transform, true);
            }

            SetStatus("Anchor resolved.");
        }
        else
        {
            SetStatus($"Resolving anchor failed: {cloudAnchor.cloudAnchorState}");
            Pose p = new Pose(transform.position, transform.rotation);
            _canvasInstance = CreateOrSpawnVisual(p, visualPrefab);
            _canvasInstance.transform.SetParent(transform, true);
        }
    }

    private bool TryGetPlacementPoseFromScreenCenter(out Pose pose)
    {
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        _hits.Clear();

        if (raycastManager.Raycast(screenCenter, _hits, TrackableType.PlaneWithinPolygon))
        {
            pose = _hits[0].pose;
            return true;
        }

        pose = default;
        return false;
    }

    private GameObject CreateOrSpawnVisual(Pose pose, GameObject visualPrefab)
    {
        if (visualPrefab != null)
        {
            var instance = Instantiate(visualPrefab, pose.position, pose.rotation);
            instance.name = visualPrefab.name;
            return instance;
        }

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.position = pose.position;
        go.transform.rotation = pose.rotation;
        go.transform.localScale = Vector3.one * 0.1f;
        return go;
    }
    
    private void SetStatus(string msg)
    {
        statusText.text = msg;
        Debug.Log(msg);
    }
}