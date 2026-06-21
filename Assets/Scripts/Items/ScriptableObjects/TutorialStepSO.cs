using UnityEngine;

namespace Deadzone.UI {

    /// <summary>
    /// Defines what action the player must perform to complete this tutorial step.
    /// </summary>
    public enum CompletionType {
        OnMouseMove,
        OnWASDPress,
        OnItemSelected,
        OnAttack,
        OnTimeout,
        OnJumpPress,
        OnCrouchPress,
        OnRunPress,
        OnReloadPress,
        OnMeleePress,
        OnAimPress
    }

    /// <summary>
    /// ScriptableObject that defines a single tutorial step.
    /// Create instances via Assets > Create > Deadzone > Tutorial Step.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTutorialStep", menuName = "Deadzone/Tutorial Step")]
    public class TutorialStepSO : ScriptableObject {

        #region SERIALIZED FIELDS

        [Header("Identification")]
        [SerializeField] private string stepId;

        [Header("Display")]
        [TextArea(2, 6)]
        [SerializeField] private string tutorialText;

        [SerializeField] private Sprite tutorialImage;

        [Header("Completion")]
        [SerializeField] private CompletionType completionType;

        [Tooltip("Parameter for completion (e.g. item ID for OnItemSelected).")]
        [SerializeField] private string completionParam;

        [Header("Timeout")]
        [Tooltip("Override the default timeout from TutorialUI. 0 = use default.")]
        [SerializeField] private float timeout = 0f;

        [Header("Auto-Chaining")]
        [Tooltip("ID do próximo tutorial a ser exibido automaticamente quando este for concluído.")]
        [SerializeField] private string nextStepId;

        #endregion

        #region PROPERTIES

        public string StepId => stepId;
        public string TutorialText => tutorialText;
        public Sprite TutorialImage => tutorialImage;
        public CompletionType CompletionType => completionType;
        public string CompletionParam => completionParam;
        public float Timeout => timeout;
        public string NextStepId => nextStepId;

        #endregion

        #region METHODS

        /// <summary>
        /// Configures this SO at runtime (used when creating dynamic tutorials via ScriptableObject.CreateInstance).
        /// </summary>
        public void Setup(string id, string text, Sprite image, CompletionType type, string param, float time = 0f, string nextId = "") {
            stepId = id;
            tutorialText = text;
            tutorialImage = image;
            completionType = type;
            completionParam = param;
            timeout = time;
            nextStepId = nextId;
        }

        #endregion

    }

}
