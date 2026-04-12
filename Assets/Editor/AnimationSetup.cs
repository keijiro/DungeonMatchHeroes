using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class AnimationSetup
{
    [MenuItem("Tools/Setup Combat Animations")]
    public static void Setup()
    {
        // 1. Create simple Attack animation (Bump forward)
        AnimationClip attackClip = new AnimationClip();
        attackClip.name = "Attack";
        AnimationCurve curveX = AnimationCurve.Linear(0, 0, 0.1f, -0.5f); // Move left (towards party if enemies)
        curveX.AddKey(0.2f, 0);
        attackClip.SetCurve("", typeof(Transform), "localPosition.x", curveX);
        AssetDatabase.CreateAsset(attackClip, "Assets/Animations/Attack.anim");

        // 1b. Create Player Attack (Move right)
        AnimationClip playerAttackClip = new AnimationClip();
        playerAttackClip.name = "PlayerAttack";
        AnimationCurve curvePX = AnimationCurve.Linear(0, 0, 0.1f, 0.5f);
        curvePX.AddKey(0.2f, 0);
        playerAttackClip.SetCurve("", typeof(Transform), "localPosition.x", curvePX);
        AssetDatabase.CreateAsset(playerAttackClip, "Assets/Animations/PlayerAttack.anim");

        // 2. Create Magic animation (Scale pulse)
        AnimationClip magicClip = new AnimationClip();
        magicClip.name = "Magic";
        AnimationCurve curveS = AnimationCurve.Linear(0, 1, 0.15f, 1.3f);
        curveS.AddKey(0.3f, 1);
        magicClip.SetCurve("", typeof(Transform), "localScale.x", curveS);
        magicClip.SetCurve("", typeof(Transform), "localScale.y", curveS);
        AssetDatabase.CreateAsset(magicClip, "Assets/Animations/Magic.anim");

        // 3. Update Controllers
        UpdateController("Assets/Animations/FighterController.controller", "Attack", playerAttackClip);
        UpdateController("Assets/Animations/MageController.controller", "Magic", magicClip);
        
        string[] monsterControllers = AssetDatabase.FindAssets("t:AnimatorController", new[] { "Assets/Animations/Characters/Monster" });
        foreach (string guid in monsterControllers)
        {
            UpdateController(AssetDatabase.GUIDToAssetPath(guid), "Attack", attackClip);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Combat Animations Setup Complete.");
    }

    private static void UpdateController(string path, string stateName, AnimationClip clip)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) return;

        var rootStateMachine = controller.layers[0].stateMachine;
        
        // Check if state exists
        bool exists = false;
        foreach (var state in rootStateMachine.states)
        {
            if (state.state.name == stateName)
            {
                state.state.motion = clip;
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            var newState = rootStateMachine.AddState(stateName);
            newState.motion = clip;
            
            // Add transition back to Idle
            var idleState = rootStateMachine.defaultState;
            var transition = newState.AddTransition(idleState);
            transition.hasExitTime = true;
            transition.exitTime = 1.0f;
            transition.duration = 0.1f;
        }
    }
}
