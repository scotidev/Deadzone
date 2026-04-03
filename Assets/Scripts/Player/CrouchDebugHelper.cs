using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Script temporário de debug para testar o sistema de crouch.
    /// Adicione este script ao GameObject do Player para debug.
    /// </summary>
    public class CrouchDebugHelper : MonoBehaviour
    {
        private CharacterBehaviour character;
        
        void Start()
        {
            character = GetComponent<CharacterBehaviour>();
            Debug.Log("[CROUCH DEBUG HELPER] Inicializado! Pressione Left Ctrl para agachar.");
            
            if (character == null)
            {
                Debug.LogError("[CROUCH DEBUG HELPER] CharacterBehaviour não encontrado!");
            }
        }
        
        void Update()
        {
            // Testa o input diretamente via Keyboard (bypass do Input System)
            // Isso nos diz se o problema é o Input System ou a lógica de crouch
            if (Keyboard.current != null)
            {
                bool leftCtrlPressed = Keyboard.current.leftCtrlKey.isPressed;
                bool leftCtrlJustPressed = Keyboard.current.leftCtrlKey.wasPressedThisFrame;
                bool leftCtrlJustReleased = Keyboard.current.leftCtrlKey.wasReleasedThisFrame;
                
                if (leftCtrlJustPressed)
                {
                    Debug.Log("[CROUCH DEBUG HELPER] ⬇️ LEFT CTRL PRESSIONADO (via Keyboard.current)");
                }
                
                if (leftCtrlJustReleased)
                {
                    Debug.Log("[CROUCH DEBUG HELPER] ⬆️ LEFT CTRL SOLTO (via Keyboard.current)");
                }
                
                // Log contínuo do estado (apenas a cada 60 frames para não spammar)
                if (Time.frameCount % 60 == 0)
                {
                    bool isCrouching = character != null && character.IsCrouching();
                    Debug.Log($"[CROUCH DEBUG HELPER] Estado atual - LeftCtrl: {leftCtrlPressed}, IsCrouching: {isCrouching}");
                }
            }
            else
            {
                if (Time.frameCount % 300 == 0)
                {
                    Debug.LogWarning("[CROUCH DEBUG HELPER] Keyboard.current é null! Input System pode não estar inicializado.");
                }
            }
        }
    }
}
