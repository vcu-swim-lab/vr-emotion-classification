using System;
using System.Collections;
using System.IO;

using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.XR.Management;


public class CameraManager : MonoBehaviour
{
    [SerializeField] private OVRFaceExpressions faceExpressions;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private float distance = 0.5f;
    [SerializeField] private Vector3 offset = Vector3.zero;

    private bool recording = true;

    void Awake()
    {
        var movieRecorderSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieRecorderSettings.name = "Video";
        movieRecorderSettings.Enabled = true;
        movieRecorderSettings.CaptureAudio = false;
        movieRecorderSettings.ImageInputSettings = new RenderTextureInputSettings
        {
            RenderTexture = renderTexture,
            OutputWidth = renderTexture.width,
            OutputHeight = renderTexture.height
        };

        var sessionTime = DateTimeOffset.Now;
        var folderName = $"VideoRecordings/{sessionTime.Year}{sessionTime.Month:D2}{sessionTime.Day:D2}-{sessionTime.Hour:D2}{sessionTime.Minute:D2}";

        string recordName = "video.mp4";
        movieRecorderSettings.OutputFile = Path.Combine(folderName, recordName);

        StartCoroutine(StartNewRecording(movieRecorderSettings));
        StartCoroutine(LookAtViewpoint());
    }

    private IEnumerator LookAtViewpoint()
    {
        yield return null;

        while (true)
        {
            // Find the Viewpoint object
            GameObject viewpoint = GameObject.Find("Viewpoint");
            if (viewpoint != null)
            {
                while (true)
                {
                    // Position camera in front of Aura's face (negative forward direction)
                    transform.position = viewpoint.transform.position + offset + (-viewpoint.transform.forward * distance);
                    // Look at Aura's face
                    transform.LookAt(viewpoint.transform.position + offset);

                    yield return null;
                }
            }
            else
            {
                Debug.LogWarning("Could not find Viewpoint. Will try again in 0.5 seconds.");
                yield return new WaitForSeconds(0.5f); // try again after a delay
            }
        }
    }

    private IEnumerator StartNewRecording(MovieRecorderSettings settings)
    {
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.AddRecorderSettings(settings);
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 30;

        var recorderController = new RecorderController(controllerSettings);
        recorderController.PrepareRecording();

        if (!recorderController.StartRecording())
        {
            Debug.LogWarning("Not recording due to an internal error. Check previous message for more info. ");
            yield break;
        }

        yield return new WaitUntil(() => !recording);
        recorderController.StopRecording();
    }

    void OnDisable()
    {
        recording = false;
    }

    void OnApplicationQuit()
    {
        var xrManager = XRGeneralSettings.Instance.Manager;
        if (xrManager.isInitializationComplete)
        {
            xrManager.StopSubsystems();
            xrManager.DeinitializeLoader();
        }
    }
}
