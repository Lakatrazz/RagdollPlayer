using BoneLib;
using BoneLib.BoneMenu;

using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Marrow;

using MelonLoader;

using System;

using UnityEngine;

using Page = BoneLib.BoneMenu.Page;

namespace RagdollPlayer;

public class RagdollPlayerMod : MelonMod 
{
    public const string Version = "1.3.0";

    private const float DoubleTapTimer = 0.32f;

    public enum RagdollMode
    {
        LIMP,
        ARM_CONTROL,
    }

    public enum RagdollBinding 
    {
        THUMBSTICK_PRESS,
        DOUBLE_TAP_B,
    }

    public enum RagdollHand 
    {
        RIGHT_HAND,
        LEFT_HAND,
    }

    public static MelonPreferences_Category MelonPrefCategory { get; private set; }
    public static MelonPreferences_Entry<bool> MelonPrefEnabled { get; private set; }
    public static MelonPreferences_Entry<RagdollBinding> MelonPrefBinding { get; private set; }
    public static MelonPreferences_Entry<RagdollHand> MelonPrefHand { get; private set; }
    public static MelonPreferences_Entry<RagdollMode> MelonPrefMode { get; private set; }

    public static bool IsEnabled { get; private set; }
    public static RagdollBinding Binding { get; private set; }
    public static RagdollHand Hand { get; private set; }
    public static RagdollMode Mode { get; private set; }

    public static Page MainPage { get; private set; }
    public static BoolElement EnabledElement { get; private set; }
    public static EnumElement BindingElement { get; private set; }
    public static EnumElement HandElement { get; private set; }
    public static EnumElement ModeElement { get; private set; }

    private static float _lastTimeInput;
    private static bool _ragdollNextButton;

    private static bool _preferencesSetup = false;

    public override void OnInitializeMelon() 
    {
        SetupMelonPrefs();
        SetupBoneMenu();
    }

    public static void SetupMelonPrefs() 
    {
        MelonPrefCategory = MelonPreferences.CreateCategory("Ragdoll Player");
        MelonPrefEnabled = MelonPrefCategory.CreateEntry("IsEnabled", true);
        MelonPrefBinding = MelonPrefCategory.CreateEntry("Binding", RagdollBinding.THUMBSTICK_PRESS);
        MelonPrefHand = MelonPrefCategory.CreateEntry("Hand", RagdollHand.RIGHT_HAND);
        MelonPrefMode = MelonPrefCategory.CreateEntry("Mode", RagdollMode.LIMP);

        IsEnabled = MelonPrefEnabled.Value;
        Binding = MelonPrefBinding.Value;
        Hand = MelonPrefHand.Value;
        Mode = MelonPrefMode.Value;

        _preferencesSetup = true;
    }

    public static void SetupBoneMenu()
    {
        MainPage = Page.Root.CreatePage("Ragdoll Player", Color.green);
        EnabledElement = MainPage.CreateBool("Enabled", Color.yellow, IsEnabled, OnSetEnabled);
        BindingElement = MainPage.CreateEnum("Binding", Color.red, Binding, OnSetBinding);
        ModeElement = MainPage.CreateEnum("Mode", Color.cyan, Mode, OnSetMode);
        HandElement = MainPage.CreateEnum("Hand", Color.green, Hand, OnSetHand);
    }

    private static void OnSetEnabled(bool value) 
    {
        IsEnabled = value;
        MelonPrefEnabled.Value = value;
        MelonPrefCategory.SaveToFile(false);
    }

    private static void OnSetBinding(Enum value) 
    {
        var binding = (RagdollBinding)value;

        Binding = binding;
        MelonPrefBinding.Value = binding;
        MelonPrefCategory.SaveToFile(false);
    }

    private static void OnSetHand(Enum value) 
    {
        var hand = (RagdollHand)value;

        Hand = hand;
        MelonPrefHand.Value = hand;
        MelonPrefCategory.SaveToFile(false);
    }

    private static void OnSetMode(Enum value)
    {
        var mode = (RagdollMode)value;

        Mode = mode;
        MelonPrefMode.Value = mode;
        MelonPrefCategory.SaveToFile(false);
    }

    public override void OnPreferencesLoaded() 
    {
        if (!_preferencesSetup)
        {
            return;
        }

        IsEnabled = MelonPrefEnabled.Value;
        Binding = MelonPrefBinding.Value;
        Hand = MelonPrefHand.Value;
        Mode = MelonPrefMode.Value;

        EnabledElement.Value = IsEnabled;
        BindingElement.Value = Binding;
        HandElement.Value = Hand;
        ModeElement.Value = Mode;
    }

    public override void OnUpdate()
    {
        if (!IsEnabled)
        {
            return;
        }

        var rig = Player.RigManager;

        // Make sure the phys rig exists
        if (rig && !rig.activeSeat && !UIRig.Instance.popUpMenu.m_IsCursorShown) 
        {
            var controller = GetController();
            bool input = GetInput(controller);

            // Toggle ragdoll
            if (input) 
            {
                var physRig = Player.PhysicsRig;

                bool isRagdolled = physRig.torso.shutdown || !physRig.ballLocoEnabled;

                if (!isRagdolled)
                {
                    RagdollRig(rig);
                }
                else
                {
                    UnragdollRig(rig);
                }
            }
        }
    }

    public static void RagdollRig(RigManager rig)
    {
        var physicsRig = rig.physicsRig;

        // Only shutdown the rig if we go limp
        if (Mode == RagdollMode.LIMP)
        {
            physicsRig.ShutdownRig();
        }

        physicsRig.RagdollRig();

        // Ragdoll the legs if we only have arm control
        if (Mode == RagdollMode.ARM_CONTROL)
        {
            physicsRig.DisableBallLoco();
            physicsRig.PhysicalLegs();
            physicsRig.legLf.ShutdownLimb();
            physicsRig.legRt.ShutdownLimb();
        }
    }

    public static void UnragdollRig(RigManager rig) 
    {
        var physicsRig = rig.physicsRig;

        var feet = physicsRig.feet.transform;
        var knee = physicsRig.knee.transform;
        var pelvis = physicsRig.m_pelvis.transform;

        physicsRig.TurnOnRig();
        physicsRig.UnRagdollRig();

        var position = pelvis.position;
        var rotation = pelvis.rotation;

        knee.SetPositionAndRotation(position, rotation);
        feet.SetPositionAndRotation(position, rotation);
    }

    private static BaseController GetController() 
    {
        return Hand switch
        {
            RagdollHand.LEFT_HAND => Player.LeftController,
            _ => Player.RightController,
        };
    }

    private static bool GetInput(BaseController controller) 
    {
        switch (Binding) 
        {
            default:
            case RagdollBinding.THUMBSTICK_PRESS:
                _lastTimeInput = 0f;
                _ragdollNextButton = false;

                return controller.GetThumbStickDown();
            case RagdollBinding.DOUBLE_TAP_B:
                bool isDown = controller.GetBButtonDown();
                float time = Time.realtimeSinceStartup;

                if (isDown && _ragdollNextButton) 
                {
                    if (time - _lastTimeInput <= DoubleTapTimer) 
                    {
                        return true;
                    }
                    else
                    {
                        _ragdollNextButton = false;
                        _lastTimeInput = 0f;
                    }
                }
                else if (isDown) 
                {
                    _lastTimeInput = time;
                    _ragdollNextButton = true;
                }
                else if (time - _lastTimeInput > DoubleTapTimer) 
                {
                    _ragdollNextButton = false;
                    _lastTimeInput = 0f;
                }

                return false;
        }
    }
}