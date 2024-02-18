using BoneLib;
using BoneLib.BoneMenu;
using BoneLib.BoneMenu.Elements;

using MelonLoader;

using SLZ.Rig;

using UnityEngine;

namespace RagdollPlayer
{
    public class RagdollPlayerMod : MelonMod {
        public const string Version = "1.2.0";

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
        public static MelonPreferences_Entry<bool> MelonPrefKeepArmControl { get; private set; }
        public static MelonPreferences_Entry<RagdollBinding> MelonPrefBinding { get; private set; }
        public static MelonPreferences_Entry<RagdollHand> MelonPrefHand { get; private set; }

        public static bool IsEnabled { get; private set; }
        public static bool KeepArmControl { get; private set; }
        public static RagdollBinding Binding { get; private set; }
        public static RagdollHand Hand { get; private set; }

        public static MenuCategory BoneMenuCategory { get; private set; }
        public static BoolElement EnabledElement { get; private set; }
        public static BoolElement KeepArmControlElement { get; private set; }
        public static EnumElement<RagdollBinding> BindingElement { get; private set; }
        public static EnumElement<RagdollHand> HandElement { get; private set; }

        private static float _lastTimeInput;
        private static bool _ragdollNextButton;

        private static bool _preferencesSetup = false;

        public override void OnInitializeMelon() {
            SetupMelonPrefs();
            SetupBoneMenu();
        }

        public static void SetupMelonPrefs() {
            MelonPrefCategory = MelonPreferences.CreateCategory("Ragdoll Player");
            MelonPrefEnabled = MelonPrefCategory.CreateEntry("IsEnabled", true);
            MelonPrefKeepArmControl = MelonPrefCategory.CreateEntry("KeepArmControl", false);
            MelonPrefBinding = MelonPrefCategory.CreateEntry("Binding", RagdollBinding.THUMBSTICK_PRESS);
            MelonPrefHand = MelonPrefCategory.CreateEntry("Hand", RagdollHand.RIGHT_HAND);

            IsEnabled = MelonPrefEnabled.Value;
            KeepArmControl = MelonPrefKeepArmControl.Value;
            Binding = MelonPrefBinding.Value;
            Hand = MelonPrefHand.Value;

            _preferencesSetup = true;
        }

        public static void SetupBoneMenu()
        {
            BoneMenuCategory = MenuManager.CreateCategory("Ragdoll Player", Color.green);
            EnabledElement = BoneMenuCategory.CreateBoolElement("Mod Toggle", Color.yellow, IsEnabled, OnSetEnabled);
            KeepArmControlElement = BoneMenuCategory.CreateBoolElement("Keep Arm Control", Color.yellow, KeepArmControl, OnSetArmControl);
            BindingElement = BoneMenuCategory.CreateEnumElement("Binding", Color.yellow, Binding, OnSetBinding);
            HandElement = BoneMenuCategory.CreateEnumElement("Hand", Color.yellow, Hand, OnSetHand);
        }

        public static void OnSetEnabled(bool value) {
            IsEnabled = value;
            MelonPrefEnabled.Value = value;
            MelonPrefCategory.SaveToFile(false);
        }

        public static void OnSetArmControl(bool value)
        {
            KeepArmControl = value;
            MelonPrefKeepArmControl.Value = value;
            MelonPrefCategory.SaveToFile(false);
        }

        public static void OnSetBinding(RagdollBinding binding) {
            Binding = binding;
            MelonPrefBinding.Value = binding;
            MelonPrefCategory.SaveToFile(false);
        }

        public static void OnSetHand(RagdollHand hand) {
            Hand = hand;
            MelonPrefHand.Value = hand;
            MelonPrefCategory.SaveToFile(false);
        }

        public override void OnPreferencesLoaded() {
            if (!_preferencesSetup)
            {
                return;
            }

            IsEnabled = MelonPrefEnabled.Value;
            KeepArmControl = MelonPrefKeepArmControl.Value;
            Binding = MelonPrefBinding.Value;
            Hand = MelonPrefHand.Value;

            EnabledElement.SetValue(IsEnabled);
            KeepArmControlElement.SetValue(KeepArmControl);
            BindingElement.SetValue(Binding);
            HandElement.SetValue(Hand);
        }

        public override void OnUpdate() {
            if (IsEnabled) {
                var physRig = Player.physicsRig;

                // Make sure the phys rig exists
                if (physRig && !physRig.manager.activeSeat && !physRig.manager.uiRig.popUpMenu.m_IsCursorShown) {
                    var controller = GetController();
                    bool input = GetInput(controller);

                    // Toggle ragdoll
                    if (input) {
                        bool isRagdolled = physRig.torso.spineInternalMult == 0f;

                        if (!isRagdolled) {
                            physRig.RagdollRig();

                            if (KeepArmControl) {
                                physRig.leftHand.physHand.forceMultiplier = 1f;
                                physRig.rightHand.physHand.forceMultiplier = 1f;
                            }
                        }
                        else
                            physRig.UnRagdollRig();
                    }
                }
            }
        }

        private static BaseController GetController() {
            switch (Hand) {
                default:
                case RagdollHand.RIGHT_HAND:
                    return Player.rightController;
                case RagdollHand.LEFT_HAND:
                    return Player.leftController;
            }
        }

        private static bool GetInput(BaseController controller) {
            switch (Binding) {
                default:
                case RagdollBinding.THUMBSTICK_PRESS:
                    _lastTimeInput = 0f;
                    _ragdollNextButton = false;

                    return controller.GetThumbStickDown();
                case RagdollBinding.DOUBLE_TAP_B:
                    bool isDown = controller.GetBButtonDown();
                    float time = Time.realtimeSinceStartup;

                    if (isDown && _ragdollNextButton) {
                        if (time - _lastTimeInput <= DoubleTapTimer) {
                            return true;
                        }
                        else {
                            _ragdollNextButton = false;
                            _lastTimeInput = 0f;
                        }
                    }
                    else if (isDown) {
                        _lastTimeInput = time;
                        _ragdollNextButton = true;
                    }
                    else if (time - _lastTimeInput > DoubleTapTimer) {
                        _ragdollNextButton = false;
                        _lastTimeInput = 0f;
                    }

                    return false;
            }
        }
    }
}
