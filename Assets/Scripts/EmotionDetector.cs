using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;

using TMPro;
using UnityEngine;

// TODO:
// - grayscale the images
// - consider batching, eg. once per second

public class EmotionDetector : MonoBehaviour
{
    public Texture faceTexture;
    public Texture2D testTexture;

    private float[] input;

    void Awake()
    {
#if !UNITY_EDITOR
    string dmlPath = $"{Application.dataPath}/Plugins/x86_64/DirectML.dll";

    string buildDir = Directory.GetParent(Application.dataPath).ToString();
    string targetPath = $"{buildDir}/DirectML.dll";

    if (!File.Exists(targetPath)) File.Copy(dmlPath, targetPath);
#endif
    }

    void Start()
    {
        input = new float[224 * 224 * 3];

        StartCoroutine(PredictAndShow());
    }

    private IEnumerator PredictAndShow()
    {
        var tex = new Texture2D(faceTexture.width, faceTexture.height, TextureFormat.RGBA32, 1, false);

        while (true)
        {
            yield return new WaitForSeconds(.1f);

            // TODO: resize the face texture from 256x256 to 224x224
            Graphics.CopyTexture(faceTexture, tex);
            tex.Apply();

            var pixels = tex.GetPixels32();
            var pixelsLength = 224 * 224;

            var im = 1.0f / 255;

            for (int p = 0; p < pixelsLength; ++p)
            {
                int pch = p * 3;

                input[pch + 0] = pixels[p][0] * im;
                input[pch + 1] = pixels[p][1] * im;
                input[pch + 2] = pixels[p][2] * im;
            }


            var in_tensor = OrtValue.CreateTensorValueFromMemory<float>(
                OrtMemoryInfo.DefaultInstance, input,
                new long[] { 1, 224, 224, 3 }//
            );

            var in_labels = new Dictionary<string, OrtValue>{
                {"inputs", in_tensor},
            };

            var path = $"{Application.streamingAssetsPath}/mobilenet_v2.onnx";

            using var session = new InferenceSession(path);
            using var opts = new RunOptions();

            using var results = session.Run(opts, in_labels, session.OutputNames);
            var output = results[0].GetTensorDataAsSpan<float>().ToArray();

            // Map predicted value to an emotion class
            // TODO: reorder the fields to match the model
            string[] emotions = { "Anger", "Disgust", "Fear", "Happiness", "Sadness", "Neutral", "Surprise" };

            // TODO: recheck this
            var maxWeight = output.Max();
            var maxIndex = Array.FindIndex(output, (w) => w == maxWeight);
            var prediction = emotions[maxIndex];

            print($"You are feeling {prediction}");

            var emotionText = GetComponent<TextMeshProUGUI>();
            emotionText.text = prediction;
        }
    }

    void OnDestroy()
    {
        Mobilenet.cleanup();
    }
}
