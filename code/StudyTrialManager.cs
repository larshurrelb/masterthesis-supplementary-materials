// edited with help of github copilot
// Also a bit messy because this was built in many iterations while designing the study and changing things around, but it works :)
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

using UnityEngine.UI;
using UnityEngine.Video;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum StudyMode
{
    RealTime,
    Study
}

public enum RobotPhase
{
    Idle,
    Thinking,
    YourTurn,
    Acting
}

public enum StudyCondition
{
    Tutorial,
    RecipeA_Visual,
    RecipeA_Textual,
    RecipeB_Visual,
    RecipeB_Textual
}

public class StudyTrialManager : MonoBehaviour
{
    #region Segment Configuration

    [Serializable]
    public class SegmentData
    {
        [Tooltip("Unique identifier for this segment")]
        public string segmentId;

        [Tooltip("The prompt to display/send for this segment")]
        [TextArea(2, 5)]
        public string prompt;

        [Tooltip("Ordered list of freeze-frame GameObjects to show sequentially for this segment")]
        public List<GameObject> freezeFrames = new List<GameObject>();

        [Tooltip("Pre-recorded video for Visual study conditions")]
        public VideoClip preRecordedVisual;

        [Tooltip("How long to show the visualization before options appear (seconds). Used in Study mode only.")]
        public float visualizationDuration = 8f;

        [Header("Options (display text only)")]
        [Tooltip("Display text for Option A")]
        public string optionA_Text;

        [Tooltip("Display text for Option B")]
        public string optionB_Text;

        [Tooltip("Display text for Option C")]
        public string optionC_Text;

        [Tooltip("Display text for Option D")]
        public string optionD_Text;
    }

    [Header("=== SEGMENT CONFIGURATION ===")]

    [Tooltip("Segments for RealTime mode (live API demos) - uses Recipe A by default")]
    [SerializeField] private List<SegmentData> realTimeSegment = new List<SegmentData>()
    {
        new SegmentData
        {
            segmentId = "realTime_step1",
            prompt = "chopping the cranberries",
            visualizationDuration = 8f,
            optionA_Text = "Set out cutting board and knife",
            optionB_Text = "Place a pot on the stove",
            optionC_Text = "Hold the bowl steady",
            optionD_Text = "Peel the cranberries"
        },
        new SegmentData
        {
            segmentId = "realTime_step2",
            prompt = "portioning the sugar",
            visualizationDuration = 8f,
            optionA_Text = "Fetch the flour",
            optionB_Text = "Get sugar and a measuring cup",
            optionC_Text = "Heat up the cranberries",
            optionD_Text = "Bring the salt to the counter"
        },
        new SegmentData
        {
            segmentId = "realTime_step3",
            prompt = "draining the pineapple",
            visualizationDuration = 8f,
            optionA_Text = "Place a colander in the sink",
            optionB_Text = "Pour pineapple into a bowl",
            optionC_Text = "Turn on the stove",
            optionD_Text = "Swap the salt the robot grabbed for sugar"
        },
        new SegmentData
        {
            segmentId = "realTime_step4",
            prompt = "combining and resting mixture in room",
            visualizationDuration = 8f,
            optionA_Text = "Put the mixture in the fridge to rest",
            optionB_Text = "Get the whisk and beat the mixture",
            optionC_Text = "Place the mixing bowl on the counter",
            optionD_Text = "Heat the mixture on the stove"
        },
        new SegmentData
        {
            segmentId = "realTime_step5",
            prompt = "portioning marshmallows",
            visualizationDuration = 8f,
            optionA_Text = "Get the heavy cream from the fridge",
            optionB_Text = "Fetch marshmallows and a scale",
            optionC_Text = "Fetch the flour",
            optionD_Text = "Fetch powdered sugar to coat them"
        },
        new SegmentData
        {
            segmentId = "realTime_step6",
            prompt = "whipping the cream",
            visualizationDuration = 8f,
            optionA_Text = "Melt butter in a pan",
            optionB_Text = "Stir the resting mixture",
            optionC_Text = "Pour the cream into the mixture bowl",
            optionD_Text = "Hand the robot a whisk"
        },
        new SegmentData
        {
            segmentId = "realTime_step7",
            prompt = "get walnuts, finishing cream and mixing all",
            visualizationDuration = 8f,
            optionA_Text = "Heat sugar to caramelize",
            optionB_Text = "Hold the container steady so cream whips properly",
            optionC_Text = "Get walnuts and a knife",
            optionD_Text = "Get more pineapple to add"
        },
        new SegmentData
        {
            segmentId = "realTime_step8",
            prompt = "refrigerating the salad",
            visualizationDuration = 8f,
            optionA_Text = "Open the fridge door",
            optionB_Text = "Cover the bowl with plastic wrap",
            optionC_Text = "Place the bowl on the stove",
            optionD_Text = "Scoop portions onto plates to serve"
        }
    };

    [Tooltip("Segments for Tutorial condition - single practice step")]
    [SerializeField] private List<SegmentData> tutorialSegment = new List<SegmentData>()
    {
        new SegmentData
        {
            segmentId = "tutorial_step1",
            prompt = "tutorial demonstration",
            visualizationDuration = 8f,
            optionA_Text = "Tutorial Option 1",
            optionB_Text = "Tutorial Option 2",
            optionC_Text = "Tutorial Option 3",
            optionD_Text = "Tutorial Option 4"
        }
    };

    [Tooltip("Baseline prediction segments (no thought bubble) - 3 steps shown after tutorial Part A")]
    [SerializeField] private List<SegmentData> tutorialBaselineSegments = new List<SegmentData>()
    {
        new SegmentData
        {
            segmentId = "baseline_step1",
            prompt = "baseline step 1",
            visualizationDuration = 0f,
            optionA_Text = "Baseline 1A",
            optionB_Text = "Baseline 1B",
            optionC_Text = "Baseline 1C",
            optionD_Text = "Baseline 1D"
        },
        new SegmentData
        {
            segmentId = "baseline_step2",
            prompt = "baseline step 2",
            visualizationDuration = 0f,
            optionA_Text = "Baseline 2A",
            optionB_Text = "Baseline 2B",
            optionC_Text = "Baseline 2C",
            optionD_Text = "Baseline 2D"
        },
        new SegmentData
        {
            segmentId = "baseline_step3",
            prompt = "baseline step 3",
            visualizationDuration = 0f,
            optionA_Text = "Baseline 3A",
            optionB_Text = "Baseline 3B",
            optionC_Text = "Baseline 3C",
            optionD_Text = "Baseline 3D"
        }
    };

    [Tooltip("Segments for Recipe A (Cranberry Fluff Salad) - used by both Visual and Textual conditions")]
    [SerializeField] private List<SegmentData> recipeASegment = new List<SegmentData>()
    {
        new SegmentData
        {
            segmentId = "recipeA_step1",
            prompt = "chopping the cranberries",
            visualizationDuration = 8f,
            optionA_Text = "Set out cutting board and knife",
            optionB_Text = "Place a pot on the stove",
            optionC_Text = "Hold the bowl steady",
            optionD_Text = "Peel the cranberries"
        },
        new SegmentData
        {
            segmentId = "recipeA_step2",
            prompt = "portioning the sugar",
            visualizationDuration = 8f,
            optionA_Text = "Fetch the flour",
            optionB_Text = "Get sugar and a measuring cup",
            optionC_Text = "Heat up the cranberries",
            optionD_Text = "Bring the salt to the counter"
        },
        new SegmentData
        {
            segmentId = "recipeA_step3",
            prompt = "draining the pineapple",
            visualizationDuration = 8f,
            optionA_Text = "Place a colander in the sink",
            optionB_Text = "Pour pineapple into a bowl",
            optionC_Text = "Turn on the stove",
            optionD_Text = "Swap the salt the robot grabbed for sugar"
        },
        new SegmentData
        {
            segmentId = "recipeA_step4",
            prompt = "combining and resting mixture in room",
            visualizationDuration = 8f,
            optionA_Text = "Put the mixture in the fridge to rest",
            optionB_Text = "Get the whisk and beat the mixture",
            optionC_Text = "Place the mixing bowl on the counter",
            optionD_Text = "Heat the mixture on the stove"
        },
        new SegmentData
        {
            segmentId = "recipeA_step5",
            prompt = "portioning marshmallows",
            visualizationDuration = 8f,
            optionA_Text = "Get the heavy cream from the fridge",
            optionB_Text = "Fetch marshmallows and a scale",
            optionC_Text = "Fetch the flour",
            optionD_Text = "Fetch powdered sugar to coat them"
        },
        new SegmentData
        {
            segmentId = "recipeA_step6",
            prompt = "whipping the cream",
            visualizationDuration = 8f,
            optionA_Text = "Melt butter in a pan",
            optionB_Text = "Stir the resting mixture",
            optionC_Text = "Pour the cream into the mixture bowl",
            optionD_Text = "Hand the robot a whisk"
        },
        new SegmentData
        {
            segmentId = "recipeA_step7",
            prompt = "get walnuts, finishing cream and mixing all",
            visualizationDuration = 8f,
            optionA_Text = "Heat sugar to caramelize",
            optionB_Text = "Hold the container steady so cream whips properly",
            optionC_Text = "Get walnuts and a knife",
            optionD_Text = "Get more pineapple to add"
        },
        new SegmentData
        {
            segmentId = "recipeA_step8",
            prompt = "refrigerating the salad",
            visualizationDuration = 8f,
            optionA_Text = "Open the fridge door",
            optionB_Text = "Cover the bowl with plastic wrap",
            optionC_Text = "Place the bowl on the stove",
            optionD_Text = "Scoop portions onto plates to serve"
        }
    };

    [Tooltip("Segments for Recipe B (Mexican Wedding Cookies) - used by both Visual and Textual conditions")]
    [SerializeField] private List<SegmentData> recipeBSegment = new List<SegmentData>()
    {
        new SegmentData
        {
            segmentId = "recipeB_step1",
            prompt = "softening the butter",
            visualizationDuration = 8f,
            optionA_Text = "Melt the butter in a pan",
            optionB_Text = "Take butter out of the fridge",
            optionC_Text = "Turn on the oven",
            optionD_Text = "Get the flour and start sifting"
        },
        new SegmentData
        {
            segmentId = "recipeB_step2",
            prompt = "portioning powdered sugar",
            visualizationDuration = 8f,
            optionA_Text = "Get powdered sugar and a measuring cup",
            optionB_Text = "Mash the butter",
            optionC_Text = "Sprinkle sugar over the butter to cream them",
            optionD_Text = "Fetch maple syrup from the pantry"
        },
        new SegmentData
        {
            segmentId = "recipeB_step3",
            prompt = "sifting the flour",
            visualizationDuration = 8f,
            optionA_Text = "Get the sifter and a clean bowl",
            optionB_Text = "Measure the butter",
            optionC_Text = "Knead flour into the butter",
            optionD_Text = "Swap flour with Corn Starch"
        },
        new SegmentData
        {
            segmentId = "recipeB_step4",
            prompt = "grinding the walnuts",
            visualizationDuration = 8f,
            optionA_Text = "Turn on the stove",
            optionB_Text = "Fetch milk",
            optionC_Text = "Get fruits and chop them up",
            optionD_Text = "Fetch walnuts and the grinder"
        },
        new SegmentData
        {
            segmentId = "recipeB_step5",
            prompt = "mixing the dough",
            visualizationDuration = 8f,
            optionA_Text = "Get vanilla and place all ingredients by the bowl",
            optionB_Text = "Roll the dough out flat",
            optionC_Text = "Add water to help the mixture combine",
            optionD_Text = "Swap the fruit the robot grabbed for walnuts"
        },
        new SegmentData
        {
            segmentId = "recipeB_step6",
            prompt = "shaping dough balls",
            visualizationDuration = 8f,
            optionA_Text = "Flatten the dough balls with a fork",
            optionB_Text = "Help portion dough into equal pieces",
            optionC_Text = "Place the dough into the oven",
            optionD_Text = "Roll the dough balls in powdered sugar"
        },
        new SegmentData
        {
            segmentId = "recipeB_step7",
            prompt = "reshaping and baking cookies",
            visualizationDuration = 8f,
            optionA_Text = "Get the cookie sheet and preheat the oven",
            optionB_Text = "Turn the oven up higher for faster baking",
            optionC_Text = "Reshape the uneven dough balls to equal size",
            optionD_Text = "Add a glaze on top of the dough balls"
        },
        new SegmentData
        {
            segmentId = "recipeB_step8",
            prompt = "rolling cookies in powdered sugar",
            visualizationDuration = 8f,
            optionA_Text = "Drizzle melted chocolate over cookies",
            optionB_Text = "Cut cookies in half",
            optionC_Text = "Prepare a bowl of powdered sugar",
            optionD_Text = "Put maple syrup on top of cookies"
        }
    };

    #endregion

    #region Mode Configuration

    [Header("=== MODE CONFIGURATION ===")]
    [Tooltip("App mode: RealTime (live Daydream API) or Study (pre-recorded visuals, 5 conditions)")]
    public StudyMode appMode = StudyMode.Study;

    [Tooltip("Study condition to run (only used in Study mode)")]
    public StudyCondition currentCondition = StudyCondition.Tutorial;

    [Header("=== REALTIME PROMPT (RealTime mode only) ===")]
    [Tooltip("Prompt to send to the Daydream API in RealTime mode")]
    [TextArea(2, 5)]
    public string realTimePrompt = "a robot cooking in a kitchen";

    [Tooltip("Scene objects to show in Visual conditions only")]
    [SerializeField] private List<GameObject> visualObjects = new List<GameObject>();

    [Tooltip("Scene objects to show in Textual conditions only")]
    [SerializeField] private List<GameObject> textualObjects = new List<GameObject>();

    #endregion

    #region References

    [Header("=== REFERENCES ===")]
    [Tooltip("Reference to the OptionPanelController UI")]
    [SerializeField] private OptionPanelController optionPanel;

    [Tooltip("Reference to DaydreamAPIManager for Visual mode prompts (RealTime mode)")]
    [SerializeField] private DaydreamAPIManager daydreamManager;

    [Tooltip("Reference to TextualModeManager for Textual mode prompts")]
    [SerializeField] private TextualModeManager textualModeManager;

    [Tooltip("Reference to ThoughtBubbleAnimator for bubble display")]
    [SerializeField] private ThoughtBubbleAnimator bubbleAnimator;

    [Tooltip("UI panel shown when study is complete (e.g. 'You may remove the headset')")]
    [SerializeField] private GameObject studyCompleteUI;

    [Header("=== CONFIDENCE UI ===")]
    [Tooltip("Root GameObject of the confidence rating panel (assign in Inspector)")]
    [SerializeField] private GameObject confidencePanel;

    [Tooltip("Button for confidence level 1 (Not at all)")]
    [SerializeField] private Button confidenceButton1;

    [Tooltip("Button for confidence level 2")]
    [SerializeField] private Button confidenceButton2;

    [Tooltip("Button for confidence level 3")]
    [SerializeField] private Button confidenceButton3;

    [Tooltip("Button for confidence level 4")]
    [SerializeField] private Button confidenceButton4;

    [Tooltip("Button for confidence level 5 (Very confident)")]
    [SerializeField] private Button confidenceButton5;

    [Header("=== STEP ASSESSMENT UI ===")]
    [Tooltip("Root GameObject of the step assessment panel (assign in Inspector)")]
    [SerializeField] private GameObject stepAssessmentPanel;

    [Tooltip("Button for 'Yes' — robot performed the step correctly")]
    [SerializeField] private Button stepAssessmentYesButton;

    [Tooltip("Button for 'No' — robot did NOT perform the step correctly")]
    [SerializeField] private Button stepAssessmentNoButton;

    [Header("=== PRE-RECORDED VISUALS ===")]
    [Tooltip("Output RenderTexture for pre-recorded visuals (same texture the thought bubble displays)")]
    [SerializeField] private RenderTexture visualOutputTexture;

    [Header("=== ROBOT ANCHOR ===")]
    [Tooltip("Persistent empty Transform used as the LazyFollow target for the thought bubble. Updated to the active robot's position on each freeze frame.")]
    [SerializeField] private Transform robotAnchor;

    #endregion

    #region Settings

    [Header("=== SETTINGS ===")]
    [Tooltip("Participant ID for result tracking")]
    [SerializeField] private string participantId = "P001";

    [Tooltip("How long each freeze frame is shown before transitioning to the next one (seconds)")]
    [SerializeField] private float freezeFrameDuration = 2f;

    [Tooltip("The base-scene GameObject shown before the study starts (hidden once the first segment begins)")]
    [SerializeField] private GameObject baseSceneObject;

    [Tooltip("Delay before showing options after animation/visualization (seconds)")]
    [SerializeField] private float delayBeforeOptions = 1.0f;

    [Tooltip("Delay after user selection before next segment (seconds)")]
    [SerializeField] private float delayAfterSelection = 1.0f;

    [Tooltip("Delay before starting API stream in RealTime Visual mode (seconds)")]
    [SerializeField] private float visualModeAPIDelay = 5f;

    #endregion

    #region Run Metadata

    [Header("=== GAZE TRACKING ===")]
    [Tooltip("Tracks how long the user looks at the thought bubble during each segment")]
    [SerializeField] private GazeTracker gazeTracker;

    [Header("=== PHASE STATUS LABEL ===")]
    [Tooltip("In-world TextMeshPro label that displays the current robot phase (Thinking / Your Turn / Acting)")]
    [SerializeField] private TextMeshPro phaseLabel;

    [Header("=== RUN METADATA ===")]
    [Tooltip("Which condition run this is (1 = first condition, 2 = second condition)")]
    [SerializeField] private int conditionOrder = 1;

    [Tooltip("Counterbalancing group (1-4)")]
    [SerializeField] private int counterbalancingGroup = 1;

    #endregion

    #region Status (Read-Only)

    [Header("=== STATUS (Read-Only) ===")]
    [SerializeField] private bool isRunning = false;
    [SerializeField] private int currentSegmentIndex = -1;
    [SerializeField] private string currentStatus = "Idle";

    [Tooltip("The current prompt being displayed (read-only)")]
    public string currentExecutingPrompt = "";

    #endregion

    #region Private Fields

    private List<int> segmentOrder;
    private List<SegmentResult> results = new List<SegmentResult>();
    private float selectionStartTime;
    private Coroutine visualModeCoroutine;
    private Coroutine studyCoroutine;

    // Confidence UI state
    private int selectedConfidenceLevel = -1;
    private bool hasConfidenceSelection = false;

    // Step assessment UI state
    private bool hasStepAssessment = false;
    private bool stepAssessmentAnswer = false; // true = Yes, false = No

    
    private RobotPhase currentPhase = RobotPhase.Idle;

    // Tracks the last freeze frame left active so it can be hidden when the next segment starts
    private GameObject lastActiveFreezeFrame;

    // VideoPlayer for pre-recorded visuals
    private VideoPlayer preRecordedPlayer;

    [Serializable]
    private class SegmentResult
    {
        public string segmentId;
        public string prompt;
        public int selectedOption; // 0 = A, 1 = B, 2 = C, 3 = D
        public float reactionTimeSeconds;
        public string timestamp;
        public string selectedText;
        public float gazeTimeSeconds;
        public float visualizationWindowSeconds;
        public float gazePercentage;
        public int confidenceLevel;
        public bool stepAssessmentCorrect; // true = Yes (correct), false = No (incorrect)
    }

    [Serializable]
    private class StudyResults
    {
        public string participantId;
        public string condition;
        public string recipe;
        public string modality;
        public int conditionOrder;
        public int counterbalancingGroup;
        public string startTime;
        public string endTime;
        public List<SegmentResult> segments;
    }

    #endregion

    #region Derived Properties

    private bool IsVisualCondition => appMode == StudyMode.RealTime
        || currentCondition == StudyCondition.RecipeA_Visual
        || currentCondition == StudyCondition.RecipeB_Visual
        || currentCondition == StudyCondition.Tutorial;

    private bool IsTextualCondition => appMode == StudyMode.Study
        && (currentCondition == StudyCondition.RecipeA_Textual
            || currentCondition == StudyCondition.RecipeB_Textual);


    private bool IsTutorialCondition => appMode == StudyMode.Study
        && currentCondition == StudyCondition.Tutorial; // no thought bubble for tutorial


    private List<SegmentData> ActiveSegments => appMode == StudyMode.RealTime
        ? realTimeSegment
        : currentCondition switch
        {
            StudyCondition.Tutorial => tutorialSegment,
            StudyCondition.RecipeA_Visual or StudyCondition.RecipeA_Textual => recipeASegment,
            StudyCondition.RecipeB_Visual or StudyCondition.RecipeB_Textual => recipeBSegment,
            _ => tutorialSegment
        };

    private string CurrentRecipeName => appMode == StudyMode.RealTime
        ? "RealTime"
        : currentCondition switch
        {
            StudyCondition.Tutorial => "Tutorial",
            StudyCondition.RecipeA_Visual or StudyCondition.RecipeA_Textual => "RecipeA",
            StudyCondition.RecipeB_Visual or StudyCondition.RecipeB_Textual => "RecipeB",
            _ => "Unknown"
        };

    private string CurrentModalityName => IsTextualCondition ? "Textual" : "Visual";

    #endregion

    #region Unity Lifecycle


    // START
    private void Start()
    {
        // Validate references
        if (optionPanel != null)
        {
            optionPanel.OnOptionSelected += HandleOptionSelected;
            optionPanel.HidePanel();
        }
        else
        {
            Debug.LogWarning("[StudyTrialManager] OptionPanelController not assigned!");
        }

        // Hide study complete UI at start
        if (studyCompleteUI != null)
        {
            studyCompleteUI.SetActive(false);
        }

        // Setup confidence buttons
        if (confidenceButton1 != null) confidenceButton1.onClick.AddListener(() => SelectConfidence(1));
        if (confidenceButton2 != null) confidenceButton2.onClick.AddListener(() => SelectConfidence(2));
        if (confidenceButton3 != null) confidenceButton3.onClick.AddListener(() => SelectConfidence(3));
        if (confidenceButton4 != null) confidenceButton4.onClick.AddListener(() => SelectConfidence(4));
        if (confidenceButton5 != null) confidenceButton5.onClick.AddListener(() => SelectConfidence(5));
        HideConfidencePanel();

        // Setup step assessment buttons
        if (stepAssessmentYesButton != null) stepAssessmentYesButton.onClick.AddListener(() => SelectStepAssessment(true));
        if (stepAssessmentNoButton != null) stepAssessmentNoButton.onClick.AddListener(() => SelectStepAssessment(false));
        HideStepAssessmentPanel();

        // Apply initial mode visibility
        ApplyModeVisibility();
        UpdateBubbleVisibility();

        // Show base scene and hide all freeze frames at start
        if (baseSceneObject != null)
        {
            baseSceneObject.SetActive(true);

            // Position robot anchor to the base scene robot so the thought bubble LazyFollow
            // targets the correct robot from the moment the scene loads
            if (robotAnchor != null)
            {
                GameObject baseRobot = FindRobotInFrame(baseSceneObject);
                if (baseRobot != null)
                    robotAnchor.SetPositionAndRotation(baseRobot.transform.position, baseRobot.transform.rotation);
            }
        }
        HideAllFreezeFrames();

        // Mode-specific initialization
        if (appMode == StudyMode.RealTime)
        {
            // RealTime mode: Start Daydream API stream (takes time to initialize)
            visualModeCoroutine = StartCoroutine(StartVisualModeSequence());
        }
        else // Study mode
        {
            if (IsTutorialCondition)
            {
                // Tutorial: Clear the render texture so it doesn't show stale video content
                ClearVisualOutputTexture();
                Debug.Log("[StudyTrialManager] Tutorial mode: skipping visualization init, render texture cleared.");
            }
            else if (IsVisualCondition)
            {
                // Study Visual: Initialize VideoPlayer for pre-recorded visuals
                InitializePreRecordedPlayer();
            }
            else if (IsTextualCondition)
            {
                // Study Textual: Pre-fetch word data
                InitializeTextualMode();
            }
        }
    }

    private void OnValidate()
    {
        // Apply changes immediately in Editor when Inspector value changes
        ApplyModeVisibility();
        UpdateBubbleVisibility();
    }

    private void OnDestroy()
    {
        if (optionPanel != null)
        {
            optionPanel.OnOptionSelected -= HandleOptionSelected;
        }

        // Cleanup confidence button listeners
        if (confidenceButton1 != null) confidenceButton1.onClick.RemoveAllListeners();
        if (confidenceButton2 != null) confidenceButton2.onClick.RemoveAllListeners();
        if (confidenceButton3 != null) confidenceButton3.onClick.RemoveAllListeners();
        if (confidenceButton4 != null) confidenceButton4.onClick.RemoveAllListeners();
        if (confidenceButton5 != null) confidenceButton5.onClick.RemoveAllListeners();

        // Cleanup step assessment button listeners
        if (stepAssessmentYesButton != null) stepAssessmentYesButton.onClick.RemoveAllListeners();
        if (stepAssessmentNoButton != null) stepAssessmentNoButton.onClick.RemoveAllListeners();

        // Unsubscribe from TextualModeManager event
        if (textualModeManager != null)
        {
            textualModeManager.OnReady -= OnTextualModeReady;
        }

        // Clean up pre-recorded player
        CleanupPreRecordedPlayer();
    }

    #endregion

    #region Public Methods

    // IMPORTANT
    public void StartStudy()
    {
        if (isRunning)
        {
            Debug.LogWarning("[StudyTrialManager] Study is already running!");
            return;
        }

        List<SegmentData> activeSegs = ActiveSegments;
        if (activeSegs == null || activeSegs.Count == 0)
        {
            Debug.LogError("[StudyTrialManager] No segments configured for current condition!");
            return;
        }

        string conditionName = appMode == StudyMode.RealTime ? "RealTime" : currentCondition.ToString();
        Debug.Log($"[StudyTrialManager] Starting study with {activeSegs.Count} segments for participant {participantId} in {conditionName} mode");

        // Apply mode visibility
        ApplyModeVisibility();

        // For visual conditions (except Tutorial), start preparing the first video before
        // the bubble fades in so the video is already playing when the thought bubble becomes visible.
        // Tutorial shows an empty thought bubble to avoid influencing participants.
        if (IsVisualCondition && !IsTutorialCondition && activeSegs.Count > 0 && activeSegs[0].preRecordedVisual != null)
        {
            PlayPreRecordedVisual(activeSegs[0].preRecordedVisual);
        }

        // Show thought bubble
        ShowThoughtBubble();

        // Setup segment order
        segmentOrder = new List<int>();
        for (int i = 0; i < activeSegs.Count; i++)
        {
            segmentOrder.Add(i);
        }

        results.Clear();
        currentSegmentIndex = 0;
        isRunning = true;

        studyCoroutine = StartCoroutine(RunStudyCoroutine());
    }

    public void UpdateRealTimePrompt()
    {
        if (appMode != StudyMode.RealTime)
        {
            Debug.LogWarning("[StudyTrialManager] UpdateRealTimePrompt called but not in RealTime mode.");
            return;
        }

        if (daydreamManager == null)
        {
            Debug.LogError("[StudyTrialManager] DaydreamAPIManager not assigned!");
            return;
        }

        daydreamManager.SetPrompt(realTimePrompt);
        currentExecutingPrompt = realTimePrompt;
        Debug.Log($"[StudyTrialManager] RealTime prompt updated: {realTimePrompt}");
    }

    public void ResetStudy()
    {
        if (studyCoroutine != null)
        {
            StopCoroutine(studyCoroutine);
            studyCoroutine = null;
        }

        if (visualModeCoroutine != null)
        {
            StopCoroutine(visualModeCoroutine);
            visualModeCoroutine = null;
        }

        SetPhase(RobotPhase.Idle);
        isRunning = false;
        currentSegmentIndex = -1;
        currentStatus = "Reset";
        currentExecutingPrompt = "";
        results.Clear();

        if (optionPanel != null)
        {
            optionPanel.HidePanel();
        }

        // Hide study complete UI
        if (studyCompleteUI != null)
        {
            studyCompleteUI.SetActive(false);
        }

        // Hide confidence panel
        HideConfidencePanel();

        // Hide step assessment panel
        HideStepAssessmentPanel();

        // Stop pre-recorded video if playing
        StopPreRecordedVisual();

        // Hide any active freeze frame and show base scene again
        HideAllFreezeFrames();
        if (baseSceneObject != null)
            baseSceneObject.SetActive(true);

        // Reset bubble visibility
        if (bubbleAnimator != null && bubbleAnimator.thoughtBubbleCanvasGroup != null)
        {
            bubbleAnimator.thoughtBubbleCanvasGroup.alpha = 0f;
        }

        Debug.Log("[StudyTrialManager] Study reset.");
    }

    #endregion

    #region Study Flow

    private IEnumerator RunStudyCoroutine()
    {
        string startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        List<SegmentData> activeSegs = ActiveSegments;

        // In Textual mode, wait for DatamuseService to finish pre-fetching
        if (IsTextualCondition && textualModeManager != null)
        {
            if (!textualModeManager.IsReady)
            {
                currentStatus = "Waiting for word data to load...";
                Debug.Log("[StudyTrialManager] Waiting for TextualModeManager to be ready...");
                yield return new WaitUntil(() => textualModeManager.IsReady);
                Debug.Log("[StudyTrialManager] TextualModeManager is now ready. Starting segments.");
            }
        }

        while (currentSegmentIndex < segmentOrder.Count)
        {
            int segIdx = segmentOrder[currentSegmentIndex];
            SegmentData segment = activeSegs[segIdx];

            currentStatus = $"Running segment {currentSegmentIndex + 1}/{segmentOrder.Count}";
            Debug.Log($"[StudyTrialManager] Starting segment: {segment.segmentId}");

            if (appMode == StudyMode.RealTime)
            {
                // RealTime flow: Prompt → Animation → delay → Options → delay
                yield return RunRealTimeSegment(segment);
            }
            else
            {
                // Study flow: Visualization → delay → Options → Animation → delay
                yield return RunStudySegment(segment);
            }

            currentSegmentIndex++;
        }

        // Tutorial Part B: Baseline prediction (no thought bubble)
        if (IsTutorialCondition && tutorialBaselineSegments.Count > 0)
        {
            // Fade out thought bubble before baseline phase
            if (bubbleAnimator != null)
                bubbleAnimator.TriggerFadeOut();

            // Wait for fade-out to complete
            yield return new WaitForSeconds(bubbleAnimator != null
                ? bubbleAnimator.smallBubbleFadeOutDuration + bubbleAnimator.fadeOutDuration
                : 0f);

            Debug.Log("[StudyTrialManager] Tutorial Part A complete. Waiting 8 seconds before baseline prediction...");
            yield return new WaitForSeconds(8f);

            Debug.Log($"[StudyTrialManager] Starting Tutorial Part B: {tutorialBaselineSegments.Count} baseline segments");

            for (int i = 0; i < tutorialBaselineSegments.Count; i++)
            {
                SegmentData baselineSegment = tutorialBaselineSegments[i];
                currentStatus = $"Baseline {i + 1}/{tutorialBaselineSegments.Count}";
                Debug.Log($"[StudyTrialManager] Baseline segment {i + 1}/{tutorialBaselineSegments.Count}: {baselineSegment.segmentId}");

                yield return RunBaselineSegment(baselineSegment);
            }
        }

        // Study complete
        SetPhase(RobotPhase.Idle);
        isRunning = false;
        currentStatus = "Complete";
        currentExecutingPrompt = "";

        // Stop all visualizations
        StopPreRecordedVisual();
        if (IsTextualCondition && textualModeManager != null)
            textualModeManager.ClearDisplay();

        // Hide last freeze frame and restore base scene
        if (lastActiveFreezeFrame != null)
        {
            lastActiveFreezeFrame.SetActive(false);
            lastActiveFreezeFrame = null;
        }
        if (baseSceneObject != null)
            baseSceneObject.SetActive(true);

        // Fade out thought bubble
        if (bubbleAnimator != null)
            bubbleAnimator.TriggerFadeOut();

        Debug.Log("[StudyTrialManager] Study completed!");

        // Save results
        SaveResults(startTime);

        // Show study complete UI
        if (studyCompleteUI != null)
        {
            studyCompleteUI.SetActive(true);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySectionComplete();
            Debug.Log("[StudyTrialManager] Study complete UI shown.");
        }
    }
    private IEnumerator RunRealTimeSegment(SegmentData segment)
    {
        // Step 1: Send prompt to Daydream and display it
        SetPhase(RobotPhase.Thinking);
        SendAndDisplayPrompt(segment);

        // Start gaze tracking when visualization begins
        if (gazeTracker != null) gazeTracker.StartTracking();

        // Step 2: Show freeze frames sequentially (stays on last one)
        SetPhase(RobotPhase.Acting);
        yield return ShowFreezeFrames(segment);

        // Step 3: Delay before showing options
        yield return new WaitForSeconds(delayBeforeOptions);

        // Step 4: Show options and wait for selection
        SetPhase(RobotPhase.YourTurn);
        yield return ShowOptionsAndWait(segment);

        // Stop gaze tracking after option is selected
        if (gazeTracker != null) gazeTracker.StopTracking();

        // Step 5: Delay before next segment
        yield return new WaitForSeconds(delayAfterSelection);
    }

    private IEnumerator RunStudySegment(SegmentData segment)
    {
        // Step 1: Start visualization (pre-recorded video or textual display)
        SetPhase(RobotPhase.Thinking);
        SendAndDisplayPrompt(segment);

        // Start gaze tracking when visualization begins
        if (gazeTracker != null) gazeTracker.StartTracking();

        // Step 2: Wait for visualization duration
        yield return new WaitForSeconds(segment.visualizationDuration);

        // Step 3: Delay before showing options
        yield return new WaitForSeconds(delayBeforeOptions);

        // Step 4: Show options and wait for selection
        SetPhase(RobotPhase.YourTurn);
        yield return ShowOptionsAndWait(segment);

        // Stop gaze tracking after option is selected
        if (gazeTracker != null) gazeTracker.StopTracking();

        // Step 5: Fade out thought bubble and stop visualization before freeze frames
        if (bubbleAnimator != null)
            bubbleAnimator.TriggerFadeOut();
        StopPreRecordedVisual();
        if (IsTextualCondition && textualModeManager != null)
            textualModeManager.ClearDisplay();

        // Wait for fade-out to complete (small bubbles fade first, then main bubble)
        yield return new WaitForSeconds(bubbleAnimator != null ? bubbleAnimator.smallBubbleFadeOutDuration + bubbleAnimator.fadeOutDuration : 0f);

        // Wait 2 seconds after selection before showing freeze frames
        yield return new WaitForSeconds(2f);

        // Step 6: Show freeze frames sequentially (stays on last one)
        SetPhase(RobotPhase.Acting);
        yield return ShowFreezeFrames(segment);

        // Step 6b: Show step assessment and wait for response
        ShowStepAssessmentPanel();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayOptionPanelShown();
        yield return new WaitUntil(() => hasStepAssessment);
        yield return new WaitForSeconds(0.3f);
        HideStepAssessmentPanel();

        // Record step assessment on the last result
        if (results.Count > 0)
            results[results.Count - 1].stepAssessmentCorrect = stepAssessmentAnswer;

        // Step 7: Fade thought bubble back in for next segment (unless this is the last one)
        if (currentSegmentIndex + 1 < segmentOrder.Count && bubbleAnimator != null)
        {
            // For visual conditions (except Tutorial), start playing the next video BEFORE
            // fading in the bubble so there is no delay between the bubble appearing and the video playing.
            if (IsVisualCondition && !IsTutorialCondition)
            {
                List<SegmentData> activeSegsForNext = ActiveSegments;
                int nextSegIdx = segmentOrder[currentSegmentIndex + 1];
                SegmentData nextSegment = activeSegsForNext[nextSegIdx];
                if (nextSegment.preRecordedVisual != null)
                {
                    PlayPreRecordedVisual(nextSegment.preRecordedVisual);
                    yield return new WaitUntil(() => preRecordedPlayer != null && preRecordedPlayer.isPlaying);
                }
            }

            bubbleAnimator.TriggerFadeBackIn();
            SetPhase(RobotPhase.Thinking);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayThoughtBubbleShown();
            yield return new WaitForSeconds(bubbleAnimator.fadeInDuration);
        }

        // Step 8: Delay before next segment
        yield return new WaitForSeconds(delayAfterSelection);
    }


    private IEnumerator RunBaselineSegment(SegmentData segment)
    {
        currentExecutingPrompt = segment.prompt;
        currentStatus = $"Baseline: {segment.segmentId}";
        Debug.Log($"[StudyTrialManager] Starting baseline segment: {segment.segmentId}");

        // Step 1: Show options immediately (no visualization phase)
        SetPhase(RobotPhase.YourTurn);
        yield return ShowOptionsAndWait(segment);

        // Step 2: Wait before showing freeze frames
        yield return new WaitForSeconds(2f);

        // Step 3: Show freeze frames sequentially
        SetPhase(RobotPhase.Acting);
        yield return ShowFreezeFrames(segment);

        // Step 4: Show step assessment and wait for response
        ShowStepAssessmentPanel();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayOptionPanelShown();
        yield return new WaitUntil(() => hasStepAssessment);
        yield return new WaitForSeconds(0.3f);
        HideStepAssessmentPanel();

        // Record step assessment on the last result
        if (results.Count > 0)
            results[results.Count - 1].stepAssessmentCorrect = stepAssessmentAnswer;

        // Step 5: Delay before next baseline segment
        yield return new WaitForSeconds(delayAfterSelection);
    }

    private void SendAndDisplayPrompt(SegmentData segment)
    {
        currentExecutingPrompt = segment.prompt;

        // Tutorial: show empty thought bubble without any visualization content
        if (IsTutorialCondition)
        {
            Debug.Log($"[StudyTrialManager] Tutorial mode: showing empty thought bubble (no visualization) for prompt: {segment.prompt}");
            return;
        }

        if (appMode == StudyMode.RealTime)
        {
            // RealTime mode: Send to live Daydream API
            if (daydreamManager != null)
            {
                daydreamManager.SetPrompt(segment.prompt);
                Debug.Log($"[StudyTrialManager] Sent prompt to Daydream: {segment.prompt}");
            }
            else
            {
                Debug.LogWarning("[StudyTrialManager] DaydreamAPIManager not assigned - prompt not sent to API!");
            }
        }
        else if (IsVisualCondition)
        {
            // Study Visual: Play pre-recorded video
            if (segment.preRecordedVisual != null)
            {
                PlayPreRecordedVisual(segment.preRecordedVisual);
                Debug.Log($"[StudyTrialManager] Playing pre-recorded visual for: {segment.prompt}");
            }
            else
            {
                Debug.LogWarning($"[StudyTrialManager] No pre-recorded visual assigned for segment {segment.segmentId}!");
            }
        }
        else if (IsTextualCondition)
        {
            // Study Textual: Send to TextualModeManager
            if (textualModeManager != null)
            {
                textualModeManager.SetPrompt(segment.prompt);
                Debug.Log($"[StudyTrialManager] Sent prompt to TextualModeManager: {segment.prompt}");
            }
            else
            {
                Debug.LogWarning("[StudyTrialManager] TextualModeManager not assigned - prompt not displayed!");
            }
        }
    }

    
    private IEnumerator ShowFreezeFrames(SegmentData segment)
    {
        if (segment.freezeFrames == null || segment.freezeFrames.Count == 0)
        {
            Debug.LogWarning($"[StudyTrialManager] No freeze frames assigned for segment {segment.segmentId}, skipping.");
            yield break;
        }

        // Hide previous segment's last active freeze frame and show base scene in between
        if (lastActiveFreezeFrame != null)
        {
            lastActiveFreezeFrame.SetActive(false);
            lastActiveFreezeFrame = null;
            if (baseSceneObject != null)
                baseSceneObject.SetActive(true);
        }

        currentStatus = $"Showing freeze frames: {segment.segmentId}";
        Debug.Log($"[StudyTrialManager] Showing {segment.freezeFrames.Count} freeze frame(s) for segment {segment.segmentId} ({freezeFrameDuration}s each)");

        for (int i = 0; i < segment.freezeFrames.Count; i++)
        {
            GameObject frame = segment.freezeFrames[i];
            if (frame == null)
            {
                Debug.LogWarning($"[StudyTrialManager] Freeze frame {i} is null in segment {segment.segmentId}, skipping.");
                continue;
            }

            // Hide previous frame (if any)
            if (i > 0 && segment.freezeFrames[i - 1] != null)
                segment.freezeFrames[i - 1].SetActive(false);

            // Hide base scene when the first freeze frame appears
            if (i == 0 && baseSceneObject != null)
                baseSceneObject.SetActive(false);

            // Show current frame
            frame.SetActive(true);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayFreezeFrame();
            Debug.Log($"[StudyTrialManager] Freeze frame {i + 1}/{segment.freezeFrames.Count} active");

            // Reposition robot anchor so the thought bubble LazyFollow stays next to the active robot
            if (robotAnchor != null)
            {
                GameObject robot = FindRobotInFrame(frame);
                if (robot != null)
                    robotAnchor.SetPositionAndRotation(robot.transform.position, robot.transform.rotation);
            }

            // All frames wait for freezeFrameDuration, including the last
            yield return new WaitForSeconds(freezeFrameDuration);
        }

        // Track the last active frame so it can be hidden when the next segment starts
        lastActiveFreezeFrame = segment.freezeFrames[segment.freezeFrames.Count - 1];

        Debug.Log("[StudyTrialManager] Freeze frames complete — remaining on last frame.");
    }

    private GameObject FindRobotInFrame(GameObject frame)
    {
        // Try tag first (fast, no iteration)
        Transform tagged = FindChildWithTag(frame.transform, "Robot");
        if (tagged != null) return tagged.gameObject;

        // Fallback: first Animator in the hierarchy
        Animator anim = frame.GetComponentInChildren<Animator>();
        if (anim != null) return anim.gameObject;

        Debug.LogWarning($"[StudyTrialManager] Could not find robot in freeze frame '{frame.name}'");
        return null;
    }

    
    private Transform FindChildWithTag(Transform parent, string tag)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag(tag)) return child;
            Transform found = FindChildWithTag(child, tag);
            if (found != null) return found;
        }
        return null;
    }

 
    private void HideAllFreezeFrames()
    {
        void HideInList(List<SegmentData> segments)
        {
            foreach (var seg in segments)
            {
                if (seg.freezeFrames == null) continue;
                foreach (var frame in seg.freezeFrames)
                {
                    if (frame != null) frame.SetActive(false);
                }
            }
        }

        HideInList(realTimeSegment);
        HideInList(tutorialSegment);
        HideInList(tutorialBaselineSegments);
        HideInList(recipeASegment);
        HideInList(recipeBSegment);

        lastActiveFreezeFrame = null;
    }

    private IEnumerator ShowOptionsAndWait(SegmentData segment)
    {
        if (optionPanel == null)
        {
            Debug.LogError("[StudyTrialManager] OptionPanelController not assigned!");
            yield break;
        }

        currentStatus = "Awaiting user selection";

        // Record start time for reaction time measurement
        selectionStartTime = Time.time;

        // Show the option panel
        optionPanel.ShowOptions(segment.optionA_Text, segment.optionB_Text, segment.optionC_Text, segment.optionD_Text);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayOptionPanelShown();

        Debug.Log($"[StudyTrialManager] Showing options: A='{segment.optionA_Text}', B='{segment.optionB_Text}', C='{segment.optionC_Text}', D='{segment.optionD_Text}'");

        // Wait until a selection is made
        yield return new WaitUntil(() => optionPanel.HasSelection);

        // Get the selection
        int selection = optionPanel.SelectedOption;
        float reactionTime = Time.time - selectionStartTime;
        string selectedText = selection switch
        {
            0 => segment.optionA_Text,
            1 => segment.optionB_Text,
            2 => segment.optionC_Text,
            3 => segment.optionD_Text,
            _ => ""
        };
        string optionLetter = selection switch { 0 => "A", 1 => "B", 2 => "C", 3 => "D", _ => "?" };

        Debug.Log($"[StudyTrialManager] User selected Option {optionLetter}: {selectedText} (RT: {reactionTime:F2}s)");

        // Record result (include gaze data if tracker is available)
        float gazeTime = gazeTracker != null ? gazeTracker.GazeTimeSeconds : 0f;
        float windowTime = gazeTracker != null ? gazeTracker.WindowTimeSeconds : 0f;
        float gazePct = gazeTracker != null ? gazeTracker.GazePercentage : 0f;

        // Hide options and show confidence panel
        optionPanel.HidePanel();
        ShowConfidencePanel();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayOptionPanelShown();

        Debug.Log("[StudyTrialManager] Awaiting confidence rating...");
        yield return new WaitUntil(() => hasConfidenceSelection);

        int confidence = selectedConfidenceLevel;
        Debug.Log($"[StudyTrialManager] Confidence level selected: {confidence}");

        // Brief delay so the selection sound finishes before the panel hides
        yield return new WaitForSeconds(0.3f);
        HideConfidencePanel();

        results.Add(new SegmentResult
        {
            segmentId = segment.segmentId,
            prompt = segment.prompt,
            selectedOption = selection,
            reactionTimeSeconds = reactionTime,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            selectedText = selectedText,
            gazeTimeSeconds = gazeTime,
            visualizationWindowSeconds = windowTime,
            gazePercentage = gazePct,
            confidenceLevel = confidence
        });

        Debug.Log($"[StudyTrialManager] Gaze data: {gazeTime:F2}s / {windowTime:F2}s ({gazePct:F1}%)");
    }

    #endregion

    #region Pre-Recorded Visual Playback

    private void InitializePreRecordedPlayer()
    {
        if (visualOutputTexture == null)
        {
            Debug.LogWarning("[StudyTrialManager] visualOutputTexture not assigned - pre-recorded visuals won't display!");
            return;
        }

        preRecordedPlayer = gameObject.AddComponent<VideoPlayer>();
        preRecordedPlayer.renderMode = VideoRenderMode.RenderTexture;
        preRecordedPlayer.targetTexture = visualOutputTexture;
        preRecordedPlayer.isLooping = true;
        preRecordedPlayer.playOnAwake = false;
        preRecordedPlayer.aspectRatio = VideoAspectRatio.FitInside;

        Debug.Log("[StudyTrialManager] Pre-recorded VideoPlayer initialized.");
    }

    private void PlayPreRecordedVisual(VideoClip clip)
    {
        if (preRecordedPlayer == null)
        {
            InitializePreRecordedPlayer();
        }

        if (preRecordedPlayer == null) return;

        // If this clip is already playing, skip restart to avoid interruption
        if (preRecordedPlayer.isPlaying && preRecordedPlayer.clip == clip)
        {
            Debug.Log("[StudyTrialManager] Pre-recorded visual already playing for this clip, skipping restart.");
            return;
        }

        preRecordedPlayer.clip = clip;
        preRecordedPlayer.Prepare();
        preRecordedPlayer.prepareCompleted += OnPreRecordedPrepared;
    }

    private void OnPreRecordedPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnPreRecordedPrepared;
        vp.Play();
        Debug.Log("[StudyTrialManager] Pre-recorded visual playing.");
    }

    private void StopPreRecordedVisual()
    {
        if (preRecordedPlayer != null && preRecordedPlayer.isPlaying)
        {
            preRecordedPlayer.Stop();
        }
    }

    private void ClearVisualOutputTexture()
    {
        if (visualOutputTexture == null) return;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = visualOutputTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = previous;
    }

    private void CleanupPreRecordedPlayer()
    {
        if (preRecordedPlayer != null)
        {
            preRecordedPlayer.Stop();
            preRecordedPlayer.targetTexture = null;
            Destroy(preRecordedPlayer);
            preRecordedPlayer = null;
        }
    }

    #endregion

    #region Event Handlers

    private void HandleOptionSelected(int optionIndex)
    {
        // This is called by the OptionPanelController when user clicks
        Debug.Log($"[StudyTrialManager] Option {optionIndex} selected via event");
    }

    #endregion

    #region Confidence Panel

    private void ShowConfidencePanel()
    {
        hasConfidenceSelection = false;
        selectedConfidenceLevel = -1;
        if (confidencePanel != null)
            confidencePanel.SetActive(true);
    }

    private void HideConfidencePanel()
    {
        if (confidencePanel != null)
            confidencePanel.SetActive(false);
    }

    private void SelectConfidence(int level)
    {
        if (hasConfidenceSelection) return;
        selectedConfidenceLevel = level;
        hasConfidenceSelection = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayOptionSelected();
        Debug.Log($"[StudyTrialManager] Confidence selected: {level}");
    }

    private void ShowStepAssessmentPanel()
    {
        hasStepAssessment = false;
        stepAssessmentAnswer = false;
        if (stepAssessmentPanel != null)
            stepAssessmentPanel.SetActive(true);
    }

    private void HideStepAssessmentPanel()
    {
        if (stepAssessmentPanel != null)
            stepAssessmentPanel.SetActive(false);
    }

    private void SelectStepAssessment(bool correct)
    {
        if (hasStepAssessment) return;
        stepAssessmentAnswer = correct;
        hasStepAssessment = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayOptionSelected();
        Debug.Log($"[StudyTrialManager] Step assessment: {(correct ? "Yes" : "No")}");
    }

    #endregion

    #region Helpers

    private void SaveResults(string startTime)
    {
        string condition = appMode == StudyMode.RealTime ? "RealTime" : currentCondition.ToString();

        StudyResults studyResults = new StudyResults
        {
            participantId = participantId,
            condition = condition,
            recipe = CurrentRecipeName,
            modality = CurrentModalityName,
            conditionOrder = conditionOrder,
            counterbalancingGroup = counterbalancingGroup,
            startTime = startTime,
            endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            segments = results
        };

        string json = JsonUtility.ToJson(studyResults, true);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"study_results_{participantId}_G{counterbalancingGroup}_C{conditionOrder}_{condition}_{timestamp}.json";

        // Save to Assets/Results folder
        string resultsFolder = Path.Combine(Application.dataPath, "Results");

        // Create directory if it doesn't exist
        if (!Directory.Exists(resultsFolder))
        {
            Directory.CreateDirectory(resultsFolder);
        }

        string path = Path.Combine(resultsFolder, filename);

        try
        {
            File.WriteAllText(path, json);
            Debug.Log($"[StudyTrialManager] Results saved to: {path}");

            #if UNITY_EDITOR
            // Refresh the asset database so the file shows up in Unity
            AssetDatabase.Refresh();
            #endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[StudyTrialManager] Failed to save results: {e.Message}");
        }
    }

    private void ApplyModeVisibility()
    {
        // Tutorial: hide everything inside the bubble
        bool showVisual = IsVisualCondition && !IsTutorialCondition;
        bool showTextual = IsTextualCondition;

        foreach (var obj in visualObjects)
        {
            if (obj != null) obj.SetActive(showVisual);
        }

        foreach (var obj in textualObjects)
        {
            if (obj != null) obj.SetActive(showTextual);
        }
    }

    private void UpdateBubbleVisibility()
    {
        if (bubbleAnimator != null && bubbleAnimator.thoughtBubbleCanvasGroup != null)
        {
            if (!Application.isPlaying || !isRunning)
            {
                bubbleAnimator.thoughtBubbleCanvasGroup.alpha = 0f;
            }
        }
    }

    private void ShowThoughtBubble()
    {
        if (bubbleAnimator == null) return;

        if (appMode == StudyMode.Study)
        {
            // Study mode: always trigger manually (both Visual pre-recorded and Textual)
            bubbleAnimator.TriggerFadeInAndAnimation();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayThoughtBubbleShown();
            Debug.Log("[StudyTrialManager] Triggered thought bubble fade-in (Study mode).");
        }
        // In RealTime mode: ThoughtBubbleAnimator handles fade-in via OnOutputStreamReceived event
    }

    private IEnumerator StartVisualModeSequence()
    {
        Debug.Log($"[StudyTrialManager] RealTime Mode Active. Waiting {visualModeAPIDelay} seconds before API call...");
        yield return new WaitForSeconds(visualModeAPIDelay);

        if (appMode == StudyMode.RealTime)
        {
            if (daydreamManager != null)
            {
                daydreamManager.SetPrompt(realTimePrompt);
                currentExecutingPrompt = realTimePrompt;
                Debug.Log("[StudyTrialManager] Timer finished. Triggering Daydream API Stream.");
                daydreamManager.StartStreaming();
            }
            else
            {
                Debug.LogError("[StudyTrialManager] DaydreamAPIManager reference is missing!");
            }
        }
        else
        {
            Debug.Log("[StudyTrialManager] Timer finished but mode is no longer RealTime. Aborting API call.");
        }

        visualModeCoroutine = null;
    }

    private void InitializeTextualMode()
    {
        if (textualModeManager == null)
        {
            Debug.LogWarning("[StudyTrialManager] TextualModeManager not assigned - cannot pre-fetch prompts!");
            return;
        }

        // Collect prompts from BOTH recipes so the cache always has complete data
        List<string> allPrompts = new List<string>();
        foreach (var segment in recipeASegment)
        {
            if (!string.IsNullOrEmpty(segment.prompt) && !allPrompts.Contains(segment.prompt))
                allPrompts.Add(segment.prompt);
        }
        foreach (var segment in recipeBSegment)
        {
            if (!string.IsNullOrEmpty(segment.prompt) && !allPrompts.Contains(segment.prompt))
                allPrompts.Add(segment.prompt);
        }
        Debug.Log($"[StudyTrialManager] Collected prompts from both recipes: {allPrompts.Count} unique prompts");

        if (allPrompts.Count == 0)
        {
            Debug.LogWarning("[StudyTrialManager] No prompts found in segments to pre-fetch!");
            return;
        }

        Debug.Log($"[StudyTrialManager] Textual Mode: Sending {allPrompts.Count} segment prompts for pre-fetching:");
        foreach (var p in allPrompts)
        {
            Debug.Log($"  -> '{p}'");
        }

        // Subscribe to ready event for bubble animation
        textualModeManager.OnReady += OnTextualModeReady;

        // Send prompts to TextualModeManager for pre-fetching
        textualModeManager.PreFetchPrompts(allPrompts);
    }

    private void OnTextualModeReady()
    {
        Debug.Log("[StudyTrialManager] Textual mode ready - DatamuseService initialized.");
    }


    private void SetPhase(RobotPhase phase)
    {
        currentPhase = phase;
        if (phaseLabel != null)
        {
            phaseLabel.text = phase switch
            {
                RobotPhase.Thinking => "Thinking",
                RobotPhase.YourTurn => "Your Turn",
                RobotPhase.Acting   => "Acting",
                _                   => ""
            };
            phaseLabel.gameObject.SetActive(phase != RobotPhase.Idle);
        }
    }

    #endregion
}
