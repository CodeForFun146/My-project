using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Parking mission: park the car inside this zone within the time limit.
// Put this on an object with a BoxCollider set to "Is Trigger", place it
// over the parking spot, press Play. Everything else (green marker on the
// ground, timer UI, win/lose screens) is created automatically.
//
// The car must stay inside the zone, slower than Max Speed To Count,
// for Required Stop Time seconds -> mission complete.
[RequireComponent(typeof(BoxCollider))]
public class ParkingZone : MonoBehaviour
{
    [Header("Mission")]
    [Tooltip("Seconds the player has to park. 0 = no time limit.")]
    [SerializeField] private float timeLimit = 60f;
    [Tooltip("Car must sit still inside the zone this many seconds to win.")]
    [SerializeField] private float requiredStopTime = 2f;
    [Tooltip("Car counts as 'stopped' below this speed (metres/second).")]
    [SerializeField] private float maxSpeedToCount = 0.5f;

    [Header("Car (optional)")]
    [Tooltip("Drag the player's car here. Empty = any object with a CarController or the 'Player' tag counts.")]
    [SerializeField] private Transform playerCar;

    [Header("Marker")]
    [SerializeField] private Color zoneColor = new Color(0.2f, 0.9f, 0.3f, 0.35f);
    [SerializeField] private Color zoneColorCarInside = new Color(1f, 0.85f, 0.2f, 0.5f);

    private BoxCollider zone;
    private Renderer markerRenderer;
    private float timeLeft;
    private float stillTime;
    private Rigidbody carInside;
    private bool missionOver;

    private Font font;
    private Text timerText;
    private Text hintText;
    private GameObject endPanel;

    void Awake()
    {
        zone = GetComponent<BoxCollider>();
        zone.isTrigger = true;
        timeLeft = timeLimit;

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildMarker();
        BuildUI();
        EnsureEventSystem();
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        GameObject esGo = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        esGo.AddComponent<StandaloneInputModule>();
#endif
    }

    void Update()
    {
        if (missionOver) return;

        // Countdown.
        if (timeLimit > 0f)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                Fail();
            }
            UpdateTimerText();
        }

        // Park check: inside + slow, for long enough.
        if (carInside != null && carInside.linearVelocity.magnitude <= maxSpeedToCount)
        {
            stillTime += Time.deltaTime;
            if (hintText != null)
                hintText.text = $"HOLD... {Mathf.Max(0f, requiredStopTime - stillTime):0.0}s";
            if (stillTime >= requiredStopTime) Win();
        }
        else
        {
            stillTime = 0f;
            if (hintText != null)
                hintText.text = carInside != null ? "STOP THE CAR" : "PARK IN THE GREEN ZONE";
        }
    }

    // ---------- trigger detection ----------

    void OnTriggerEnter(Collider other)
    {
        if (missionOver) return;
        Rigidbody body = other.attachedRigidbody;
        if (body == null || !IsPlayerCar(body)) return;
        carInside = body;
        SetMarkerColor(zoneColorCarInside);
    }

    void OnTriggerExit(Collider other)
    {
        if (carInside != null && other.attachedRigidbody == carInside)
        {
            carInside = null;
            stillTime = 0f;
            SetMarkerColor(zoneColor);
        }
    }

    private bool IsPlayerCar(Rigidbody body)
    {
        if (playerCar != null) return body.transform.root == playerCar.root;
        if (body.GetComponentInParent<CarController>() != null) return true;
        return body.transform.root.CompareTag("Player");
    }

    // ---------- win / lose ----------

    private void Win()
    {
        missionOver = true;
        ShowEndScreen("PARKED!", $"Time left: {timeLeft:0.0}s", new Color(0.2f, 0.75f, 0.3f, 0.97f));
    }

    private void Fail()
    {
        missionOver = true;
        ShowEndScreen("TIME UP!", "You did not park in time.", new Color(0.75f, 0.2f, 0.2f, 0.97f));
    }

    private void Restart()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (Application.CanStreamedLevelBeLoaded(scene))
        {
            SceneManager.LoadScene(scene);
        }
        else
        {
            Debug.LogWarning("ParkingZone: add this scene to Build Settings so RETRY can reload it.");
        }
    }

    // ---------- visuals ----------

    private void BuildMarker()
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
        marker.name = "ZoneMarker";
        Destroy(marker.GetComponent<Collider>());

        marker.transform.SetParent(transform, false);
        marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        marker.transform.localPosition = new Vector3(zone.center.x, 0.03f, zone.center.z);
        marker.transform.localScale = new Vector3(zone.size.x, zone.size.z, 1f);

        markerRenderer = marker.GetComponent<Renderer>();
        markerRenderer.material = new Material(Shader.Find("Sprites/Default"));
        SetMarkerColor(zoneColor);
    }

    private void SetMarkerColor(Color c)
    {
        if (markerRenderer != null) markerRenderer.material.color = c;
    }

    void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.4f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
    }

    // ---------- UI ----------

    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("ParkingUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var canvasRect = canvasGo.GetComponent<RectTransform>();

        timerText = MakeText(canvasRect, 56, FontStyle.Bold);
        var timerRect = timerText.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 1f);
        timerRect.anchorMax = new Vector2(0.5f, 1f);
        timerRect.pivot = new Vector2(0.5f, 1f);
        timerRect.anchoredPosition = new Vector2(0, -30);
        timerRect.sizeDelta = new Vector2(500, 80);
        UpdateTimerText();

        hintText = MakeText(canvasRect, 34, FontStyle.Bold);
        var hintRect = hintText.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0, 40);
        hintRect.sizeDelta = new Vector2(900, 60);
        hintText.text = "PARK IN THE GREEN ZONE";
    }

    private void UpdateTimerText()
    {
        if (timerText == null) return;
        if (timeLimit <= 0f) { timerText.text = ""; return; }
        timerText.text = $"TIME  {timeLeft:0.0}";
        timerText.color = timeLeft <= 10f ? new Color(1f, 0.3f, 0.25f) : Color.white;
    }

    private void ShowEndScreen(string title, string detail, Color background)
    {
        if (endPanel != null) return;
        if (hintText != null) hintText.text = "";

        Transform canvas = timerText.canvas.transform;
        endPanel = new GameObject("EndPanel", typeof(RectTransform), typeof(Image));
        endPanel.transform.SetParent(canvas, false);
        var rect = endPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(760, 420);
        endPanel.GetComponent<Image>().color = background;

        Text titleText = MakeText(rect, 76, FontStyle.Bold);
        var titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -50);
        titleRect.sizeDelta = new Vector2(700, 100);
        titleText.text = title;

        Text detailText = MakeText(rect, 34, FontStyle.Normal);
        var detailRect = detailText.GetComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0.5f, 0.5f);
        detailRect.anchorMax = new Vector2(0.5f, 0.5f);
        detailRect.anchoredPosition = new Vector2(0, -10);
        detailRect.sizeDelta = new Vector2(700, 60);
        detailText.text = detail;

        // RETRY button.
        GameObject retry = new GameObject("Button_RETRY", typeof(RectTransform), typeof(Image), typeof(Button));
        retry.transform.SetParent(rect, false);
        var retryRect = retry.GetComponent<RectTransform>();
        retryRect.anchorMin = new Vector2(0.5f, 0f);
        retryRect.anchorMax = new Vector2(0.5f, 0f);
        retryRect.pivot = new Vector2(0.5f, 0f);
        retryRect.anchoredPosition = new Vector2(0, 45);
        retryRect.sizeDelta = new Vector2(300, 85);
        retry.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 1f);
        retry.GetComponent<Button>().onClick.AddListener(Restart);

        Text retryText = MakeText(retryRect, 38, FontStyle.Bold);
        var retryTextRect = retryText.GetComponent<RectTransform>();
        retryTextRect.anchorMin = Vector2.zero;
        retryTextRect.anchorMax = Vector2.one;
        retryTextRect.offsetMin = Vector2.zero;
        retryTextRect.offsetMax = Vector2.zero;
        retryText.text = "RETRY";
    }

    private Text MakeText(Transform parent, int size, FontStyle style)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }
}
