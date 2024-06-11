using BoneLib;
using BoneLib.BoneMenu;
using BoneLib.BoneMenu.Elements;

using Il2CppSLZ.Bonelab;
using Il2CppSLZ.Interaction;
using Il2CppSLZ.Rig;

using MelonLoader;

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
        public static MelonPreferences_Entry<RagdollBinding> MelonPrefBinding { get; private set; }
        public static MelonPreferences_Entry<RagdollHand> MelonPrefHand { get; private set; }

        public static bool IsEnabled { get; private set; }
        public static RagdollBinding Binding { get; private set; }
        public static RagdollHand Hand { get; private set; }

        public static MenuCategory BoneMenuCategory { get; private set; }
        public static BoolElement EnabledElement { get; private set; }
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
            MelonPrefBinding = MelonPrefCategory.CreateEntry("Binding", RagdollBinding.THUMBSTICK_PRESS);
            MelonPrefHand = MelonPrefCategory.CreateEntry("Hand", RagdollHand.RIGHT_HAND);

            IsEnabled = MelonPrefEnabled.Value;
            Binding = MelonPrefBinding.Value;
            Hand = MelonPrefHand.Value;

            _preferencesSetup = true;
        }

        public static void SetupBoneMenu()
        {
            BoneMenuCategory = MenuManager.CreateCategory("Ragdoll Player", Color.green);
            EnabledElement = BoneMenuCategory.CreateBoolElement("Mod Toggle", Color.yellow, IsEnabled, OnSetEnabled);
            BindingElement = BoneMenuCategory.CreateEnumElement("Binding", Color.yellow, Binding, OnSetBinding);
            HandElement = BoneMenuCategory.CreateEnumElement("Hand", Color.yellow, Hand, OnSetHand);
        }

        public static void OnSetEnabled(bool value) {
            IsEnabled = value;
            MelonPrefEnabled.Value = value;
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
            Binding = MelonPrefBinding.Value;
            Hand = MelonPrefHand.Value;

            EnabledElement.SetValue(IsEnabled);
            BindingElement.SetValue(Binding);
            HandElement.SetValue(Hand);
        }

        public override void OnUpdate() {
            if (IsEnabled) {
                var rig = Player.rigManager;

                // Make sure the phys rig exists
                if (rig && !rig.activeSeat && !UIRig.Instance.popUpMenu.m_IsCursorShown) {
                    var controller = GetController();
                    bool input = GetInput(controller);

                    // Toggle ragdoll
                    if (input) {
                        var physRig = Player.physicsRig;

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
        }

        public static void RagdollRig(RigManager rig)
        {
            var physicsRig = rig.physicsRig;

            physicsRig.ShutdownRig();
            physicsRig.RagdollRig();
        }

        public static void UnragdollRig(RigManager rig) {
            var physicsRig = rig.physicsRig;

            physicsRig.TurnOnRig();
            physicsRig.UnRagdollRig();
        }

        private static BaseController GetController() {
            return Hand switch
            {
                RagdollHand.LEFT_HAND => Player.leftController,
                _ => Player.rightController,
            };
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
