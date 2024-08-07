﻿using BoneLib;
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
    public const string Version = "1.2.1";

    private const float DoubleTapTimer = 0.32f;

    public enum RagdollBinding {
        THUMBSTICK_PRESS = 0,
        DOUBLE_TAP_B = 1,
    }

    public enum RagdollHand {
        RIGHT_HAND = 0,
        LEFT_HAND = 1,
    }

    public static MelonPreferences_Category MelonPrefCategory { get; private set; }
    public static MelonPreferences_Entry<bool> MelonPrefEnabled { get; private set; }
    public static MelonPreferences_Entry<RagdollBinding> MelonPrefBinding { get; private set; }
    public static MelonPreferences_Entry<RagdollHand> MelonPrefHand { get; private set; }

    public static bool IsEnabled { get; private set; }
    public static RagdollBinding Binding { get; private set; }
    public static RagdollHand Hand { get; private set; }

    public static Page MainPage { get; private set; }
    public static BoolElement EnabledElement { get; private set; }
    public static EnumElement BindingElement { get; private set; }
    public static EnumElement HandElement { get; private set; }

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

        IsEnabled = MelonPrefEnabled.Value;
        Binding = MelonPrefBinding.Value;
        Hand = MelonPrefHand.Value;

        _preferencesSetup = true;
    }

    public static void SetupBoneMenu()
    {
        MainPage = Page.Root.CreatePage("Ragdoll Player", Color.green);
        EnabledElement = MainPage.CreateBool("Mod Toggle", Color.yellow, IsEnabled, OnSetEnabled);
        BindingElement = MainPage.CreateEnum("Binding", Color.yellow, Binding, OnSetBinding);
        HandElement = MainPage.CreateEnum("Hand", Color.yellow, Hand, OnSetHand);
    }

    public static void OnSetEnabled(bool value) 
    {
        IsEnabled = value;
        MelonPrefEnabled.Value = value;
        MelonPrefCategory.SaveToFile(false);
    }

    public static void OnSetBinding(Enum value) 
    {
        var binding = (RagdollBinding)value;

        Binding = binding;
        MelonPrefBinding.Value = binding;
        MelonPrefCategory.SaveToFile(false);
    }

    public static void OnSetHand(Enum value) 
    {
        var hand = (RagdollHand)value;

        Hand = hand;
        MelonPrefHand.Value = hand;
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

        EnabledElement.Value = IsEnabled;
        BindingElement.Value = Binding;
        HandElement.Value = Hand;
    }

    public override void OnUpdate()
    {
        if (!IsEnabled)
        {
            return;
        }

        var rig = Player.RigManager;

        // Make sure the phys rig exists
        if (rig && !rig.activeSeat && !UIRig.Instance.popUpMenu.m_IsCursorShown) {
            var controller = GetController();
            bool input = GetInput(controller);

            // Toggle ragdoll
            if (input) 
            {
                var physRig = Player.PhysicsRig;

                bool isRagdolled = physRig.torso.shutdown;

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

        physicsRig.ShutdownRig();
        physicsRig.RagdollRig();
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