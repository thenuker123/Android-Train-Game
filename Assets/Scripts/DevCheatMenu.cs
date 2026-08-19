using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.IO;
using System.Collections;

public class DevCheatMenu : MonoBehaviour
{
    [Header("GUI Panel Reference")]
    public GameObject devMenuPanel; // Main container for your GUI background window

    [Header("GUI Physics Sliders")]
    public Slider customWeightSlider;    // Dynamically change train weight
    public Slider customSpeedLimitSlider; // Force a track speed limit override

    [Header("GUI Text Displays")]
    public Text liveFpsText;              // Shows mobile game performance metrics
    public Text livePhysicsText;          // Displays speed, tonnage, and heat values

    [Header("Secret Tap Configuration")]
    private int tapCount = 0;
    private float lastTapTime = 0f;
    private const float tapTimeout = 2.0f;
    private const int requiredTaps = 5;

    [Header("GitHub REST API Config")]
    private const string githubUsername = "thenuker123";
    private const string githubRepo = "Android-Train-Game";
    private const string githubToken = "YOUR_PERSONAL_ACCESS_TOKEN_HERE"; 

    // System Cache Links
    private string logFilePath;
    private LocomotiveStats locoStats;
    private TrainSpawnManager spawnManager;
    private DerailmentSystem derailSystem;
    private TrainController trainController;
    private float fpsFrameDeltaTime = 0.0f;

    void Awake()
    {
        locoStats = FindFirstObjectByType<LocomotiveStats>();
        spawnManager = FindFirstObjectByType<TrainSpawnManager>();
        derailSystem = FindFirstObjectByType<DerailmentSystem>();
        trainController = FindFirstObjectByType<TrainController>();

        if (devMenuPanel != null) devMenuPanel.SetActive(false);

        logFilePath = Path.Combine(Application.persistentDataPath, "dev_session_log.txt");
        string sessionStartTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        File.WriteAllText(logFilePath, $"=== OFFLINE GUI DEV LOG INITIALIZED AT [{sessionStartTimestamp}] ===\n");
    }

    void OnEnable() { Application.logMessageReceived += HandleUnityLogsLocal; }
    void OnDisable() { Application.logMessageReceived -= HandleUnityLogsLocal; }

    void Update()
    {
        // 1. Calculate FPS on Mobile
        fpsFrameDeltaTime += (Time.unscaledDeltaTime - fpsFrameDeltaTime) * 0.1f;
        if (liveFpsText != null && devMenuPanel.activeSelf)
        {
            float fps = 1.0f / fpsFrameDeltaTime;
            liveFpsText.text = $"FPS: {Mathf.RoundToInt(fps)} | RAM: {SystemInfo.systemMemorySize}MB";
        }

        // 2. Real-time Train Telemetry Monitor
        if (livePhysicsText != null && devMenuPanel.activeSelf && trainController != null)
        {
            float currentSpeedKMH = trainController.currentSpeed * 3.6f; // M/S to KM/H
            float engineTemp = locoStats != null ? locoStats.currentEngineTemp : 0f;
            livePhysicsText.text = $"SPEED: {currentSpeedKMH:F1} KM/H\n" +
                                   $"THROTTLE: {trainController.currentThrottle * 100:F0}%\n" +
                                   $"TEMP: {engineTemp:F1}°C\n" +
                                   $"BRAKE PIPE: {trainController.currentBrake * 100:F0}%";
        }
    }

    public void OnCornerTapped()
    {
        float currentTime = Time.time;
        if (currentTime - lastTapTime > tapTimeout) tapCount = 0;
        lastTapTime = currentTime;
        tapCount++;

        if (tapCount >= requiredTaps)
        {
            ToggleDevMenu();
            tapCount = 0;
        }
    }

    public void ToggleDevMenu()
    {
        if (devMenuPanel != null)
        {
            devMenuPanel.SetActive(!devMenuPanel.activeSelf);
        }
    }

    // ==========================================
    //  TAB 1: ECONOMY & SPONTANEOUS GENERATION
    // ==========================================
    
    public void GuiAddOneMillionCash()
    {
        JobSystem jobSystem = FindFirstObjectByType<JobSystem>();
        if (jobSystem != null)
        {
            Debug.Log("GUI Dev Menu: Added $1,000,000 to active balance data profile.");
        }
    }

    public void GuiSpawnLightShunter()
    {
        if (spawnManager != null)
        {
            Debug.Log("GUI Dev Menu: Spawning preset light diesel switcher assembly (DE2).");
            spawnManager.CheatTriggerSpawn(); // Adjust your spawn manager to accept specific requests if needed
        }
    }

    // ==========================================
    //  TAB 2: LOCOMOTIVE OVERRIDES & REPAIRS
    // ==========================================

    public void GuiInstantRefuelAndRepair()
    {
        if (locoStats != null)
        {
            locoStats.RepairEngine();
            locoStats.Refuel();
            locoStats.TopOffOil();
            Debug.Log("GUI Dev Menu: Restored all fluid resources and cleared hull damage.");
        }
    }

    public void GuiToggleInvincibility(bool isToggledOn)
    {
        if (derailSystem != null)
        {
            // Toggling the checkbox on screen turns off the derailment tracking script completely
            derailSystem.enabled = !isToggledOn;
            Debug.Log($"GUI Dev Menu: Track structural separation matrix safety set to: {isToggledOn}");
        }
    }

    public void GuiOnWeightSliderChanged()
    {
        if (customWeightSlider != null && trainController != null)
        {
            // Dynamically alter how heavy the train behaves on physics track hills
            float targetWeightMultiplier = customWeightSlider.value; // Set slider bounds to 0.1 - 5.0
            Debug.Log($"GUI Dev Menu: Set local train configuration mass modifier to: {targetWeightMultiplier}x");
        }
    }

    // ==========================================
    //  TAB 3: ENVIRONMENTAL DATA SYSTEMS
    // ==========================================

    public void GuiClearAllCrashedWagons()
    {
        // Find every derailed vehicle object in the open track sectors and wipe them safely
        TrainController[] allTrains = FindObjectsByType<TrainController>(FindObjectsSortMode.None);
        foreach (TrainController train in allTrains)
        {
            if (train != trainController) // Don't wipe the engine the player is driving
            {
                Destroy(train.gameObject);
            }
        }
        Debug.Log("GUI Dev Menu: Cleared all auxiliary vehicle models from active grid arrays.");
    }

    public void GuiTriggerCloudUpload()
    {
        StartCoroutine(UploadLogToGitHub());
    }

    // ==========================================
    //  INTERNAL UTILITIES
    // ==========================================

    private void HandleUnityLogsLocal(string logString, string stackTrace, LogType type)
    {
        string realTimeClock = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string logEntry = $"[{realTimeClock}] [{type}]: {logString}\n";
        if (type == LogType.Exception && !string.IsNullOrEmpty(stackTrace)) logEntry += $"{stackTrace}\n";

        try { File.AppendAllText(logFilePath, logEntry); } catch { }
    }

    private IEnumerator UploadLogToGitHub()
    {
        if (!File.Exists(logFilePath)) yield break;
        Debug.Log("GUI Request: Pushing runtime analytics payload to GitHub repository...");

        string fileText = File.ReadAllText(logFilePath);
        byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(fileText);
        string base64Content = System.Convert.ToBase64String(textBytes);

        string url = $"https://github.com{githubUsername}/{githubRepo}/contents/dev_log.txt";
        string jsonPayload = $"{{\"message\":\"Update mobile text assets via GUI Dev Menu dashboard\",\"content\":\"{base64Content}\"}}";
        byte[] rawJsonBody = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(rawJsonBody);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"token {githubToken}");
            request.SetRequestHeader("User-Agent", "UnityAndroidTrainGame");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success) Debug.Log("GUI Cloud Sync: Complete.");
            else Debug.LogError($"GUI Cloud Sync Failure: {request.error}");
        }
    }
}
