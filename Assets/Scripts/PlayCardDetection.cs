using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlayCardDetection : MonoBehaviour
{
    private ARTrackedImageManager imageManager;
    private NetworkCommunicationHandler networkCommunicationHandler;
    private Dictionary<string, (DateTime, Vector3, bool)> cardDetectionTime = new Dictionary<string, (DateTime, Vector3, bool)>();

    void Awake()
    {
        imageManager = GameObject.Find("XR Origin (Mobile AR)").GetComponent<ARTrackedImageManager>();
        networkCommunicationHandler = GameObject.Find("NetworkCommunicationHandler").GetComponent<NetworkCommunicationHandler>();
    }

    void OnEnable()
    {
        imageManager.trackedImagesChanged += OnImagesChanged;
    }

    void OnDisable()
    {
        imageManager.trackedImagesChanged -= OnImagesChanged;
    }

    private async void OnImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var trackedImage in args.added)
        {
            trackedImage.transform.SetParent(GameObject.Find("GridCanvas").transform);
            cardDetectionTime.Add(trackedImage.name, (DateTime.Now, trackedImage.transform.localPosition, false));
        }

        foreach (var trackedImage in args.updated)
        {
            if (trackedImage.trackingState != TrackingState.Tracking) continue;
            if (cardDetectionTime.TryGetValue(trackedImage.name, out var cardDetection) && cardDetection.Item3) continue;
            if (CardReady(trackedImage.name, trackedImage.transform.localPosition))
            {
                string cardName = trackedImage.referenceImage.name;
                (int x, int y) = CanvasManager.GetTileXAndY(trackedImage.transform.localPosition.x, trackedImage.transform.localPosition.z, 0.15f);
                Debug.Log("Card detected on x: " + x + " y: " + y);

                networkCommunicationHandler.UseAbility(cardName, Config.PlayerId, x, y);
            }
        }
    }

    private bool CardReady(string cardName, Vector3 localPosition)
    {
        if (cardDetectionTime.TryGetValue(cardName, out var cardDetection))
        {
            if (CardIsMoved(cardDetection.Item2, localPosition))
            {
                cardDetectionTime[cardName] = (DateTime.Now, localPosition, false);
            }
            else
            {
                if (cardDetection.Item1.Add(TimeSpan.FromSeconds(2)) <= DateTime.Now)
                {
                    var data = cardDetectionTime[cardName];
                    data.Item3 = true;
                    cardDetectionTime[cardName] = data;   
                    return true;
                } 
            }
            
        }
        return false;
    }

    private bool CardIsMoved(Vector3 oldCardPosition, Vector3 newCardPosition)
    {
        return Math.Abs(Vector3.Distance(oldCardPosition, newCardPosition)) > 0.1;
    }
}
