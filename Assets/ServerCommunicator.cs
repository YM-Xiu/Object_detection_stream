
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Networking;
using UnityEngine.UI;



public class ServerCommunicator : MonoBehaviour
{
    // ! options
    public bool timeDelay = true; // Set to true to enable time delay between sending frames to the server (to reduce server load)

    // ! public variables
    public string serverURL = "http://10.237.198.206:5958"; // Your server URL
    public Text responseText;
    public Text hintText;
    private bool isDetectionRunning = false;
    private int wasDetectionRunning = 0;

    public RawImage rawImage; // For camera feed background

    public WebCamTexture webCamTexture;

    public Button Shot;
    public Button Keep;
    public Button Discard;
    public Button BacktoCamera;

    public Button startDetectionButton;
    public Button stopDetectionButton;

    public Texture2D snap; // Class-level variable

    private Texture2D reusableTexture;  // For capturing frames
    private Texture2D processedImage;   // For displaying processed frames

    private bool isCurrentlyProcessing = false;  // For avoiding overlapping coroutine executions
    private bool wasCurrentlyProcessing = false;  // For avoiding overlapping coroutine executions

    // ! This is the function that is called when the game starts
    void Start()
    {
        webCamTexture = new WebCamTexture();

        // webCamTexture.requestedFPS = 30; // Request a specific frame rate
        webCamTexture.requestedHeight = 1000; // Set desired width and height
        webCamTexture.requestedWidth = 750;
        rawImage.texture = webCamTexture;
        rawImage.material.mainTexture = webCamTexture;
        webCamTexture.Play();

        reusableTexture = new Texture2D(webCamTexture.width, webCamTexture.height);

        startDetectionButton.onClick.AddListener(StartDetection);
        stopDetectionButton.onClick.AddListener(StopDetection);

        stopDetectionButton.gameObject.SetActive(false);  // Initially hide the stop button
    }

    void StartDetection()
    {
        isDetectionRunning = true;
        wasDetectionRunning = 0;
        startDetectionButton.gameObject.SetActive(false);
        stopDetectionButton.gameObject.SetActive(true);
    }

    void StopDetection()
    {
        isDetectionRunning = false;
        startDetectionButton.gameObject.SetActive(true);
        stopDetectionButton.gameObject.SetActive(false);
        // sleep for 0.5 seconds
        rawImage.texture = webCamTexture;
        rawImage.material.mainTexture = webCamTexture;
        webCamTexture.Play();
    }

    float timer = 0f;
    void Update()
    {
        float interval = 0.1f;
        if (isDetectionRunning)
        {
            timer += Time.deltaTime;
            if (timer > interval && timeDelay)
            {
                timer = 0f;
                StartCoroutine(SendFrameForDetection());
            }
        }
        else
        {
            // // Show the camera feed again
            if (wasDetectionRunning < 3) // wait for 5 frames, otherwise sometimes the camera feed cannot be shown
            {
                rawImage.texture = webCamTexture;
                rawImage.material.mainTexture = webCamTexture;
                webCamTexture.Play();
                wasDetectionRunning += 1;
            }
        }
    }

    // ! This is a test function for tutorial purposes on how to send and recieve request; Send a request to the server to get the GPU status
    public void SendRequestToServer()
    {
        StartCoroutine(GetRequest(serverURL + "/gpu-status"));
    }

    IEnumerator GetRequest(string url)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(": Error: " + webRequest.error);
                responseText.text = "Error: " + webRequest.error;
            }
            else
            {
                Debug.Log(":\nReceived: " + webRequest.downloadHandler.text);
                string formattedResponse = webRequest.downloadHandler.text.Replace("\\n", "\n");
                responseText.text = formattedResponse;
                // responseText.text = webRequest.downloadHandler.text;
            }
        }
    }


    // ! This is the function that takes a screenshot and save it to the phone
    // For a screenshot of the game view:
    public void CaptureScreenshot()
    {
        string filename = "Screenshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        ScreenCapture.CaptureScreenshot(filename);
        Debug.Log("Screenshot saved: " + filename);
        hintText.text = "Nicely done! Camera feed captured: " + filename;
    }

    // ! This is the function that takes a picture from the camera and saves it to the phone
    // For capturing the camera feed from WebCamTexture:
    public void CaptureCameraFeed()
    {
        snap = new Texture2D(webCamTexture.width, webCamTexture.height);
        snap.SetPixels(webCamTexture.GetPixels());
        snap.Apply();


        // Show the captured image in the RawImage (static image)
        rawImage.texture = snap;

        // Show the option buttons
        Shot.gameObject.SetActive(false);
        Keep.gameObject.SetActive(true);
        Discard.gameObject.SetActive(true);

        // Rotate the texture
        // snap = RotateTexture(snap, true);

        // Encode texture into PNG
        byte[] bytes = snap.EncodeToPNG();

        // * For saving the image to the phone, use the following code. Basically this is not needed for the project
        // string filename = "/storage/emulated/0/Pictures/Yanming/" + "CameraFeed_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        // System.IO.File.WriteAllBytes(filename, bytes);
        // Debug.Log("Camera feed captured: " + filename);
        hintText.text = "Nicely done!\nCamera feed captured.";
    }


    // ! Method to handle 'Use this for detection' button
    public void OnUseButtonClicked()
    {
        // Implement what should happen when the user decides to use the image for detection
        // For example, sending it to the server for processing
        // Hide the buttons again
        Keep.gameObject.SetActive(false);
        Discard.gameObject.SetActive(false);
        BacktoCamera.gameObject.SetActive(false);
        Shot.gameObject.SetActive(false);
        hintText.text = "Image sent to server for processing. \nPlease wait...";
        StartCoroutine(UploadImage(snap));
    }

    // ! Method to handle 'Discard' and  'Back to camera' buttons
    public void OnDiscardButtonClicked()
    {
        // Possibly return to the camera feed or handle the discard action
        // Hide the buttons again
        Keep.gameObject.SetActive(false);
        Discard.gameObject.SetActive(false);
        BacktoCamera.gameObject.SetActive(false);
        Shot.gameObject.SetActive(true);
        // Show the camera feed again
        rawImage.texture = webCamTexture;
        rawImage.material.mainTexture = webCamTexture;
        webCamTexture.Play();
        hintText.text = "Camera feed is shown again. \nPlease try again.";
    }


    // ! This is the function that sends the image to the server for processing
    IEnumerator UploadImage(Texture2D image)
    {
        byte[] imageBytes = image.EncodeToPNG();

        // Create a form section and add the image bytes
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", imageBytes, "image.png", "image/png");
        UnityWebRequest www = UnityWebRequest.Post(serverURL + "/detect-objects", form);

        float startTime = Time.realtimeSinceStartup; // Start time measurement
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
        }
        else
        {
            // Handle the server's response - assume it's an image
            Texture2D processedImage = new Texture2D(2, 2);
            processedImage.LoadImage(www.downloadHandler.data);
            rawImage.texture = processedImage; // Display the processed image
            float elapsedTime = Time.realtimeSinceStartup - startTime; // Calculate elapsed time
            hintText.text = "Image processed!\nTime for processing: " + elapsedTime.ToString("F2") + "s";
            BacktoCamera.gameObject.SetActive(true);
        }

    }

    // IEnumerator SendFrameForDetection()
    // {
    //     // Ensure that the previous frame has been processed
    //     yield return new WaitForEndOfFrame();

    //     // Convert WebCamTexture to Texture2D
    //     Texture2D texture = new Texture2D(webCamTexture.width, webCamTexture.height);
    //     texture.SetPixels(webCamTexture.GetPixels());
    //     texture.Apply();

    //     texture = ResizeTexture(texture, 150, 200);

    //     // Encode texture into a PNG
    //     byte[] imageBytes = texture.EncodeToPNG();

    //     // Create a form section and add the image bytes
    //     WWWForm form = new WWWForm();
    //     form.AddBinaryData("file", imageBytes, "image.png", "image/png");
    //     UnityWebRequest www = UnityWebRequest.Post(serverURL + "/detect-objects", form);

    //     // float startTime = Time.realtimeSinceStartup; // Start time measurement
    //     yield return www.SendWebRequest();

    //     if (www.result != UnityWebRequest.Result.Success)
    //     {
    //         Debug.LogError(www.error);
    //     }
    //     else
    //     {
    //         // Handle the server's response - assume it's an image
    //         Texture2D processedImage = new Texture2D(2, 2);
    //         processedImage.LoadImage(www.downloadHandler.data);
    //         rawImage.texture = processedImage; // Display the processed image
    //         DestroyImmediate(processedImage);
    //     }
    //     DestroyImmediate(texture);
    // }

    IEnumerator SendFrameForDetection()
    {
        if (isCurrentlyProcessing)
        {
            yield break;  // Avoid overlapping coroutine executions
        }
        isCurrentlyProcessing = true;

        // Capture the current frame
        reusableTexture.SetPixels(webCamTexture.GetPixels());
        reusableTexture.Apply();

        Texture2D resizedTexture = ResizeTexture(reusableTexture, 150, 200);

        // Encode texture into a PNG
        byte[] imageBytes = resizedTexture.EncodeToPNG();
        Destroy(resizedTexture);  // Destroy the resized texture after use
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", imageBytes, "image.png", "image/png");
        UnityWebRequest www = UnityWebRequest.Post(serverURL + "/detect-objects", form);

        // float startTime = Time.realtimeSinceStartup; // Start time measurement
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
        }
        else
        {
            // Handle the server's response
            if (processedImage != null)
            {
                Destroy(processedImage);  // Clean up the previous processed image
            }
            processedImage = new Texture2D(2, 2);
            processedImage.LoadImage(www.downloadHandler.data);
            rawImage.texture = processedImage; // Display the processed image
        }

        isCurrentlyProcessing = false;
    }

    Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        source.filterMode = FilterMode.Bilinear;
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        rt.filterMode = FilterMode.Bilinear;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D nTex = new Texture2D(newWidth, newHeight);
        nTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        nTex.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return nTex;
    }

}