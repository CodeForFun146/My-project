using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Builds the whole main-menu UI:
//   Main panel  -> Play, Garage, Settings, Quit
//   Play        -> map selection (one button per map)
//   Map click   -> level selection for that map
//
// Scene preview: right-click this component in the Inspector and choose
// "Build Menu Preview" to see the menu without entering Play mode.
// On Play the preview is thrown away and rebuilt so all buttons are wired.
//
// Level buttons load a scene named "<MapName>_Level<N>" when it is in
// Build Settings; otherwise they log a warning so the game never crashes.
public class MainMenuUI : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private string gameTitle = "CAR GAME";
    [SerializeField] private string[] mapNames = { "City", "Desert", "Mountain" };
    [SerializeField] private int levelsPerMap = 5;

    [Header("Garage")]
    [Tooltip("Car prefabs shown in the garage. Drag prefabs from Assets/Cars Prefabs here.")]
    [SerializeField] private GameObject[] carPrefabs;

    [Header("Style")]
    [Tooltip("Optional background picture shown behind every menu screen.")]
    [SerializeField] private Texture backgroundImage;
    [SerializeField] private Color panelColor = new Color(0.03f, 0.04f, 0.06f, 0.55f);
    [SerializeField] private Color buttonColor = new Color(0.15f, 0.45f, 0.85f, 1f);
    [SerializeField] private Color buttonHoverColor = new Color(0.25f, 0.55f, 0.95f, 1f);
    [SerializeField] private Color textColor = Color.white;

    private const string CanvasName = "MenuCanvas";

    private Font font;
    private RectTransform canvasRect;
    private GameObject mainPanel;
    private GameObject mapPanel;
    private GameObject levelPanel;
    private GameObject garagePanel;
    private GameObject settingsPanel;

    // Garage runtime state
    private const string SelectedCarKey = "SelectedCarIndex";
    private int currentCarIndex;
    private GameObject garageStage;
    private GameObject shownCar;
    private Camera garageCamera;
    private RenderTexture garageRT;
    private RawImage garageView;
    private Text carNameText;
    private Text selectButtonText;

    void Awake()
    {
        // Throw away any editor preview so runtime always builds a fresh,
        // fully wired menu (preview buttons have no serialized listeners).
        Transform preview = transform.Find(CanvasName);
        if (preview != null) Destroy(preview.gameObject);

        BuildMenu();
        EnsureEventSystem();
    }

#if UNITY_EDITOR
    [ContextMenu("Build Menu Preview")]
    private void BuildMenuPreview()
    {
        Transform old = transform.Find(CanvasName);
        if (old != null) DestroyImmediate(old.gameObject);
        BuildMenu();
    }

    [ContextMenu("Remove Menu Preview")]
    private void RemoveMenuPreview()
    {
        Transform old = transform.Find(CanvasName);
        if (old != null) DestroyImmediate(old.gameObject);
    }
#endif

    private void BuildMenu()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildCanvas();
        BuildBackground();

        mainPanel = BuildMainPanel();
        mapPanel = BuildMapPanel();
        garagePanel = BuildGaragePanel();
        settingsPanel = BuildPlaceholderPanel("SettingsPanel", "SETTINGS", "Options coming soon.");
        levelPanel = null;

        ShowOnly(mainPanel);
    }

    // ---------- navigation ----------

    private void ShowOnly(GameObject panel)
    {
        mainPanel.SetActive(panel == mainPanel);
        mapPanel.SetActive(panel == mapPanel);
        garagePanel.SetActive(panel == garagePanel);
        settingsPanel.SetActive(panel == settingsPanel);
        if (levelPanel != null) levelPanel.SetActive(panel == levelPanel);
    }

    private void OpenLevelSelect(int mapIndex)
    {
        if (levelPanel != null) Destroy(levelPanel);
        levelPanel = BuildLevelPanel(mapIndex);
        ShowOnly(levelPanel);
    }

    private void LoadLevel(int mapIndex, int level)
    {
        string sceneName = $"{mapNames[mapIndex]}_Level{level}";
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning($"MainMenuUI: scene '{sceneName}' is not in Build Settings yet. " +
                             "Create it and add it via File > Build Profiles to make this level playable.");
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- panel builders ----------

    private GameObject BuildMainPanel()
    {
        GameObject panel = CreatePanel("MainPanel");
        CreateTitle(panel, gameTitle);

        GameObject stack = CreateButtonStack(panel);
        CreateButton(stack, "PLAY", () => ShowOnly(mapPanel));
        CreateButton(stack, "GARAGE", OpenGarage);
        CreateButton(stack, "SETTINGS", () => ShowOnly(settingsPanel));
        CreateButton(stack, "QUIT", QuitGame);
        return panel;
    }

    private GameObject BuildMapPanel()
    {
        GameObject panel = CreatePanel("MapPanel");
        CreateTitle(panel, "SELECT MAP");

        GameObject stack = CreateButtonStack(panel);
        for (int i = 0; i < mapNames.Length; i++)
        {
            int mapIndex = i; // capture per-iteration copy for the closure
            CreateButton(stack, mapNames[i].ToUpper(), () => OpenLevelSelect(mapIndex));
        }
        CreateBackButton(panel, () => ShowOnly(mainPanel));
        return panel;
    }

    private GameObject BuildLevelPanel(int mapIndex)
    {
        GameObject panel = CreatePanel("LevelPanel");
        CreateTitle(panel, mapNames[mapIndex].ToUpper() + " — SELECT LEVEL");

        GameObject grid = new GameObject("LevelGrid", typeof(RectTransform));
        grid.transform.SetParent(panel.transform, false);
        var gridRect = grid.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.sizeDelta = new Vector2(660, 400);
        gridRect.anchoredPosition = new Vector2(0, -40);

        var layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(120, 120);
        layout.spacing = new Vector2(15, 15);
        layout.childAlignment = TextAnchor.MiddleCenter;

        for (int lvl = 1; lvl <= levelsPerMap; lvl++)
        {
            int level = lvl;
            CreateButton(grid, level.ToString(), () => LoadLevel(mapIndex, level), 42);
        }
        CreateBackButton(panel, () => ShowOnly(mapPanel));
        return panel;
    }

    private GameObject BuildPlaceholderPanel(string name, string title, string message)
    {
        GameObject panel = CreatePanel(name);
        CreateTitle(panel, title);

        GameObject label = CreateText(panel, message, 32, FontStyle.Normal);
        var rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(900, 80);

        CreateBackButton(panel, () => ShowOnly(mainPanel));
        return panel;
    }

    // ---------- garage ----------

    void Update()
    {
        // Slow turntable spin while a car is on display.
        if (shownCar != null && garagePanel != null && garagePanel.activeSelf)
        {
            shownCar.transform.Rotate(0f, 30f * Time.deltaTime, 0f, Space.World);
        }
    }

    void OnDestroy()
    {
        if (garageRT != null)
        {
            garageRT.Release();
            Destroy(garageRT);
        }
    }

    private GameObject BuildGaragePanel()
    {
        GameObject panel = CreatePanel("GaragePanel");
        CreateTitle(panel, "GARAGE");

        if (carPrefabs == null || carPrefabs.Length == 0)
        {
            GameObject hint = CreateText(panel, "No cars assigned. Drag car prefabs onto the MainMenu object.", 30, FontStyle.Normal);
            var hintRect = hint.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0.5f);
            hintRect.anchorMax = new Vector2(0.5f, 0.5f);
            hintRect.sizeDelta = new Vector2(1200, 80);
            CreateBackButton(panel, () => ShowOnly(mainPanel));
            return panel;
        }

        // Car display window (camera feed goes here).
        GameObject view = new GameObject("CarView", typeof(RectTransform), typeof(RawImage));
        view.transform.SetParent(panel.transform, false);
        var viewRect = view.GetComponent<RectTransform>();
        viewRect.anchorMin = new Vector2(0.5f, 0.5f);
        viewRect.anchorMax = new Vector2(0.5f, 0.5f);
        viewRect.sizeDelta = new Vector2(960, 540);
        viewRect.anchoredPosition = new Vector2(0, 30);
        garageView = view.GetComponent<RawImage>();

        // Car name under the display.
        GameObject nameGo = CreateText(panel, "", 44, FontStyle.Bold);
        var nameRect = nameGo.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 0.5f);
        nameRect.anchorMax = new Vector2(0.5f, 0.5f);
        nameRect.anchoredPosition = new Vector2(0, -300);
        nameRect.sizeDelta = new Vector2(800, 70);
        carNameText = nameGo.GetComponent<Text>();

        // Prev / next arrows beside the display.
        GameObject prev = CreateButton(panel, "<", () => CycleCar(-1), 60, inLayout: false);
        var prevRect = prev.GetComponent<RectTransform>();
        prevRect.anchorMin = new Vector2(0.5f, 0.5f);
        prevRect.anchorMax = new Vector2(0.5f, 0.5f);
        prevRect.anchoredPosition = new Vector2(-580, 30);
        prevRect.sizeDelta = new Vector2(90, 120);

        GameObject next = CreateButton(panel, ">", () => CycleCar(1), 60, inLayout: false);
        var nextRect = next.GetComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(0.5f, 0.5f);
        nextRect.anchorMax = new Vector2(0.5f, 0.5f);
        nextRect.anchoredPosition = new Vector2(580, 30);
        nextRect.sizeDelta = new Vector2(90, 120);

        // Select button.
        GameObject select = CreateButton(panel, "SELECT", SelectCurrentCar, 34, inLayout: false);
        var selectRect = select.GetComponent<RectTransform>();
        selectRect.anchorMin = new Vector2(0.5f, 0f);
        selectRect.anchorMax = new Vector2(0.5f, 0f);
        selectRect.pivot = new Vector2(0.5f, 0f);
        selectRect.anchoredPosition = new Vector2(0, 40);
        selectRect.sizeDelta = new Vector2(320, 80);
        selectButtonText = select.GetComponentInChildren<Text>();

        CreateBackButton(panel, CloseGarage);
        return panel;
    }

    private void OpenGarage()
    {
        currentCarIndex = Mathf.Clamp(PlayerPrefs.GetInt(SelectedCarKey, 0), 0, Mathf.Max(0, carPrefabs.Length - 1));
        ShowOnly(garagePanel);
        if (carPrefabs == null || carPrefabs.Length == 0) return;

        BuildStage();
        ShowCar(currentCarIndex);
    }

    private void CloseGarage()
    {
        DestroyStage();
        ShowOnly(mainPanel);
    }

    private void CycleCar(int direction)
    {
        if (carPrefabs.Length == 0) return;
        currentCarIndex = (currentCarIndex + direction + carPrefabs.Length) % carPrefabs.Length;
        ShowCar(currentCarIndex);
    }

    private void SelectCurrentCar()
    {
        PlayerPrefs.SetInt(SelectedCarKey, currentCarIndex);
        PlayerPrefs.SetString("SelectedCarName", carPrefabs[currentCarIndex].name);
        PlayerPrefs.Save();
        RefreshSelectButton();
    }

    private void RefreshSelectButton()
    {
        bool isSelected = PlayerPrefs.GetInt(SelectedCarKey, -1) == currentCarIndex;
        if (selectButtonText != null) selectButtonText.text = isSelected ? "SELECTED  ✓" : "SELECT";
    }

    private void BuildStage()
    {
        if (garageStage != null) return;

        // Hidden little studio far below the scene: camera + its own light.
        garageStage = new GameObject("GarageStage");
        garageStage.transform.position = new Vector3(0f, -500f, 0f);

        GameObject camGo = new GameObject("GarageCamera");
        camGo.transform.SetParent(garageStage.transform, false);
        garageCamera = camGo.AddComponent<Camera>();
        garageCamera.fieldOfView = 35f;
        garageCamera.clearFlags = CameraClearFlags.SolidColor;
        garageCamera.backgroundColor = new Color(0.05f, 0.06f, 0.09f, 1f);
        garageCamera.nearClipPlane = 0.05f;
        garageCamera.farClipPlane = 100f;

        if (garageRT == null)
        {
            garageRT = new RenderTexture(1024, 576, 24);
            garageRT.name = "GarageRT";
        }
        garageCamera.targetTexture = garageRT;
        if (garageView != null) garageView.texture = garageRT;

        GameObject lightGo = new GameObject("GarageLight");
        lightGo.transform.SetParent(garageStage.transform, false);
        lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.3f;
    }

    private void DestroyStage()
    {
        if (shownCar != null) Destroy(shownCar);
        if (garageStage != null) Destroy(garageStage);
        shownCar = null;
        garageStage = null;
        garageCamera = null;
    }

    private void ShowCar(int index)
    {
        if (shownCar != null) Destroy(shownCar);

        shownCar = Instantiate(carPrefabs[index], garageStage.transform.position, Quaternion.identity);

        // Display model only: no driving scripts, no physics.
        foreach (var script in shownCar.GetComponentsInChildren<MonoBehaviour>()) script.enabled = false;
        foreach (var body in shownCar.GetComponentsInChildren<Rigidbody>()) body.isKinematic = true;

        // Frame the car for the camera no matter its size.
        Bounds bounds = new Bounds(shownCar.transform.position, Vector3.one);
        Renderer[] renderers = shownCar.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        }
        float size = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        Vector3 viewDirection = new Vector3(1f, 0.4f, 1f).normalized;
        garageCamera.transform.position = bounds.center + viewDirection * size * 2.8f;
        garageCamera.transform.LookAt(bounds.center);

        if (carNameText != null) carNameText.text = carPrefabs[index].name.Replace("_", " ").ToUpper();
        RefreshSelectButton();
    }

    // ---------- low-level UI helpers ----------

    private void BuildCanvas()
    {
        GameObject canvasGo = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = canvasGo.GetComponent<RectTransform>();
    }

    private void BuildBackground()
    {
        if (backgroundImage == null) return;

        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        bg.transform.SetParent(canvasRect, false);

        var rect = bg.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var raw = bg.GetComponent<RawImage>();
        raw.texture = backgroundImage;
        raw.raycastTarget = false;

        // Fill the screen without stretching: crop the overflow instead.
        var fitter = bg.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = (float)backgroundImage.width / backgroundImage.height;
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

    private GameObject CreatePanel(string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasRect, false);

        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        panel.GetComponent<Image>().color = panelColor;
        return panel;
    }

    private void CreateTitle(GameObject panel, string text)
    {
        GameObject title = CreateText(panel, text, 72, FontStyle.Bold);
        var rect = title.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -60);
        rect.sizeDelta = new Vector2(1400, 100);
    }

    private GameObject CreateButtonStack(GameObject panel)
    {
        GameObject stack = new GameObject("Buttons", typeof(RectTransform));
        stack.transform.SetParent(panel.transform, false);

        var rect = stack.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(420, 480);
        rect.anchoredPosition = new Vector2(0, -40);

        var layout = stack.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        return stack;
    }

    private GameObject CreateButton(GameObject parent, string label, UnityEngine.Events.UnityAction onClick,
                                    int fontSize = 36, bool inLayout = true)
    {
        GameObject go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);

        if (inLayout)
        {
            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 90;
        }

        var image = go.GetComponent<Image>();
        image.color = buttonColor;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        GameObject text = CreateText(go, label, fontSize, FontStyle.Bold);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return go;
    }

    private void CreateBackButton(GameObject panel, UnityEngine.Events.UnityAction onClick)
    {
        GameObject back = CreateButton(panel, "< BACK", onClick, 28, inLayout: false);
        var rect = back.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(40, 40);
        rect.sizeDelta = new Vector2(220, 70);
    }

    private GameObject CreateText(GameObject parent, string content, int fontSize, FontStyle style)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent.transform, false);

        var text = go.GetComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = textColor;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;

        return go;
    }
}
