#if UNITY_XR
using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Avatars.Controllers;
using Liminal.SDK.VR.Input;
using Liminal.SDK.VR.Pointers;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using System;
using Object = UnityEngine.Object;
using System.Linq;
using Liminal.SDK.VR.Devices.GearVR.Avatar;

namespace Liminal.SDK.XR
{
	/// <summary>
	/// Mappings and further manual information available here: https://docs.unity3d.com/Manual/xr_input.html
	/// All of the below are on a per-controller basis and may or may not exist depending on the platform that it currently running
	/// 
	/// Buttons:
	/// - primaryButton
	/// - secondaryButton
	/// - secondaryTouch
	/// - gripButton
	/// - triggerButton
	/// - menuButton
	/// - primary2DAxisClick
	/// - primary2DAxisTouch
	/// - userPresence (WMR, Oculus)
	/// 
	/// Axis:
	/// - trigger
	/// - grip
	/// - batteryLevel (only WMR)
	/// 
	/// 2D Axis:
	/// - primary2DAxis
	/// - secondary2DAxis
	/// </summary>
	public class UnityXRController : UnityXRInputDevice
	{
		#region Inner enums
		public enum EPressState
		{
			None,
			Down,
			Pressing,
			Up
		}
		#endregion

		#region InputFeature inner classes
		public abstract class InputFeature
		{
			protected InputDevice? Device { get; private set; }
			public EPressState PressState { get; protected set; }
			public abstract string Name { get; }

			public InputFeature()
			{
				PressState = EPressState.None;
			}

			public void AssignDevice(InputDevice aDevice)
			{
				if (Device.HasValue) return;

				Device = aDevice;
			}

			public abstract void UpdateState();
		}

		public abstract class InputFeature<T> : InputFeature where T : IEquatable<T>
		{
			// RawValue is assigned, also assign the 'normalised' Value
			public virtual T RawValue
			{
				get; protected set;
			}
			public T Value { get; protected set; }

			public InputFeatureUsage<T> BaseFeature { get; }

			public override string Name => BaseFeature.name;

			public InputFeature(InputFeatureUsage<T> aBaseFeature) : base()
			{
				BaseFeature = aBaseFeature;
			}
		}

		public class ButtonInputFeature : InputFeature<bool>
		{
			public override bool RawValue
			{
				get => base.RawValue;
				protected set
				{
					base.RawValue = value;
					Value = RawValue;
				}
			}

			public ButtonInputFeature(InputFeatureUsage<bool> aBaseFeature) : base(aBaseFeature)
			{
			}

			public override void UpdateState()
			{
				if (!Device.HasValue) return;

				if (!Device.Value.TryGetFeatureValue(BaseFeature, out bool isPressed))
				{
					// couldn't get input for the feature, so mark press state as none
					PressState = EPressState.None;
					RawValue = false;
				}

				// received a value, so update accordingly
				EPressState currentState = PressState;
				RawValue = isPressed;

				if (isPressed)
				{
					switch (currentState)
					{
						case EPressState.None:
							PressState = EPressState.Down;
							break;
						case EPressState.Down:
							PressState = EPressState.Pressing;
							break;
						default:
							break;
					}
				}
				else
				{
					switch (currentState)
					{
						case EPressState.Pressing:
							PressState = EPressState.Up;
							break;
						case EPressState.Up:
							PressState = EPressState.None;
							break;
						default:
							break;
					}
				}
			}
		}

		public interface AxisInputFeature { }

		public class Axis1DInputFeature : InputFeature<float>, AxisInputFeature
		{
			private const float THRESHOLD = 0.1f;

			public override float RawValue
			{
				get => base.RawValue;
				protected set
				{
					base.RawValue = value;
					Value = value >= THRESHOLD ? 1f : 0f;
				}
			}

			public Axis1DInputFeature(InputFeatureUsage<float> aBaseFeature) : base(aBaseFeature)
			{
			}

			public override void UpdateState()
			{
				if (!Device.HasValue) return;

				if (!Device.Value.TryGetFeatureValue(BaseFeature, out float rawActuated))
				{
					// couldn't get input for the feature, so mark press state as none
					PressState = EPressState.None;
					RawValue = 0.0f;
				}

				// received a value, so update accordingly
				EPressState currentState = PressState;
				RawValue = rawActuated;

				// if above or equal to the threshold the axis is considered 'pressed'
				if (rawActuated >= THRESHOLD)
				{
					switch (currentState)
					{
						case EPressState.None:
							PressState = EPressState.Down;
							break;
						case EPressState.Down:
							PressState = EPressState.Pressing;
							break;
						default:
							break;
					}
				}
				else
				{
					switch (currentState)
					{
						case EPressState.Pressing:
							PressState = EPressState.Up;
							break;
						case EPressState.Up:
							PressState = EPressState.None;
							break;
						default:
							break;
					}
				}
			}
		}

		public class Axis2DInputFeature : InputFeature<Vector2>, AxisInputFeature
		{
			private const float THRESHOLD = 0.1f;

			public override Vector2 RawValue
			{
				get => base.RawValue;
				protected set
				{
					base.RawValue = value;

					Value = new Vector2(
						Mathf.Abs(base.RawValue.x) >= THRESHOLD ? 1f * Mathf.Sign(base.RawValue.x) : 0f,
						Mathf.Abs(base.RawValue.y) >= THRESHOLD ? 1f * Mathf.Sign(base.RawValue.y) : 0f
					);
				}
			}

			public Axis2DInputFeature(InputFeatureUsage<Vector2> aBaseFeature) : base(aBaseFeature)
			{
			}

			public override void UpdateState()
			{
				if (!Device.HasValue) return;

				if (!Device.Value.TryGetFeatureValue(BaseFeature, out Vector2 rawActuated))
				{
					// couldn't get input for the feature, so mark press state as none
					PressState = EPressState.None;
					RawValue = Vector2.zero;
				}

				// received a value, so update accordingly
				EPressState currentState = PressState;
				RawValue = rawActuated;

				// if either axis exceeds the threshold, considered pressed
				if (Mathf.Abs(rawActuated.x) >= THRESHOLD ||
					Mathf.Abs(rawActuated.y) >= THRESHOLD)
				{
					switch (currentState)
					{
						case EPressState.None:
							PressState = EPressState.Down;
							break;
						case EPressState.Down:
							PressState = EPressState.Pressing;
							break;
						default:
							break;
					}
				}
				else
				{
					switch (currentState)
					{
						case EPressState.Pressing:
							PressState = EPressState.Up;
							break;
						case EPressState.Up:
							PressState = EPressState.None;
							break;
						default:
							break;
					}
				}
			}
		}
		#endregion

		public override string Name => "UnityXRController";

		// TODO: Confirm this?
		public override int ButtonCount => 3;

		// this is mapped to 'primaryTouch' inputFeature
		public override bool IsTouching { get => GetButton(VRButton.Touch); }

		private static readonly VRInputDeviceCapability _capabilities = VRInputDeviceCapability.DirectionalInput |
																		VRInputDeviceCapability.Touch |
																		VRInputDeviceCapability.TriggerButton;
		private VRInputDeviceHand mHand;
		public override VRInputDeviceHand Hand => mHand;

		/// <summary>
		/// TODO: This mapping is functional for Oculus Quest.
		/// Will need to define a minimum functional mapping to use with controllers/systems with fewer input features, as per the table:
		/// https://docs.unity3d.com/Manual/xr_input.html
		/// </summary>
		private Dictionary<string, InputFeature> _inputFeatures = new Dictionary<string, InputFeature>
		{
            // buttons
            { VRButton.Back, new ButtonInputFeature(CommonUsages.secondaryButton) },
			{ VRButton.Touch, new ButtonInputFeature(CommonUsages.primaryTouch) },
			{ VRButton.Trigger, new ButtonInputFeature(CommonUsages.triggerButton) },
            // TODO: Map VRButton.Primary to CommonUsages.triggerButton, and create a new mapping option for CommonUsages.primaryButton
            { VRButton.Primary, new ButtonInputFeature(CommonUsages.triggerButton) },
			{ VRButton.Seconday, new ButtonInputFeature(CommonUsages.gripButton) },
			{ VRButton.Three, new ButtonInputFeature(CommonUsages.primary2DAxisTouch) },
			{ VRButton.Four, new ButtonInputFeature(CommonUsages.primary2DAxisClick) },

            // axis 2D
            { VRAxis.One, new Axis2DInputFeature(CommonUsages.primary2DAxis) },

            // axis 1D
            { VRAxis.Two, new Axis1DInputFeature(CommonUsages.trigger) },
			{ VRAxis.Three, new Axis1DInputFeature(CommonUsages.grip) },
		};

		public UnityXRController(VRInputDeviceHand hand) : base(OVRUtils.GetControllerType(hand))
		{
			mHand = hand;

			Pointer?.Activate();

			foreach (var pairs in _inputFeatures.ToArray())
			{
				InputFeature inputFeature = pairs.Value;

				// also register axes with their raw counterpart
				if (inputFeature is AxisInputFeature && !pairs.Key.EndsWith("Raw"))
				{
					string rawKey = $"{pairs.Key}Raw";

					if (!_inputFeatures.ContainsKey(rawKey))
					{
						_inputFeatures.Add(rawKey, inputFeature);
					}
				}

				inputFeature.AssignDevice(InputDevice);
			}

			Debug.Log($"[{GetType().Name}] UnityXRController({hand}) created.");
		}

		public UnityXRController()
		{
		}

		protected override IVRPointer CreatePointer()
		{
			return new InputDevicePointer(this);
		}

		public InputDevice InputDevice => InputDevices.GetDeviceAtXRNode(Hand == VRInputDeviceHand.Right ? XRNode.RightHand : XRNode.LeftHand);

		public override bool HasCapabilities(VRInputDeviceCapability capabilities)
		{
			return ((_capabilities & capabilities) == capabilities);
		}

		public override bool HasAxis1D(string axis)
		{
			return _inputFeatures.TryGetValue(axis, out var feature) && feature is Axis1DInputFeature;
		}

		public override bool HasAxis2D(string axis)
		{
			return _inputFeatures.TryGetValue(axis, out var feature) && feature is Axis2DInputFeature;
		}

		public override bool HasButton(string button)
		{
			return _inputFeatures.TryGetValue(button, out var feature) && feature is ButtonInputFeature;
		}

		public override float GetAxis1D(string axis)
		{
			if (!HasAxis1D(axis)) return 0f;

			var axis1DFeature = _inputFeatures[axis] as Axis1DInputFeature;
			return axis.Contains("Raw") ? axis1DFeature.RawValue : axis1DFeature.Value;
		}

		public override Vector2 GetAxis2D(string axis)
		{
			if (!HasAxis2D(axis)) return Vector2.zero;

			var axis2DFeature = _inputFeatures[axis] as Axis2DInputFeature;
			return axis.Contains("Raw") ? axis2DFeature.RawValue : axis2DFeature.Value;
		}

		public override bool GetButton(string button)
		{
			return GetButtonState(button) == EPressState.Pressing;
		}

		public override bool GetButtonDown(string button)
		{
			return GetButtonState(button) == EPressState.Down;
		}

		public override bool GetButtonUp(string button)
		{
			return GetButtonState(button) == EPressState.Up;
		}

		public EPressState GetButtonState(string button)
		{
			if (!HasButton(button)) return EPressState.None;

			var buttonFeature = _inputFeatures[button] as ButtonInputFeature;
			return buttonFeature.PressState;
		}

		public override void Update()
		{
			// foreach input registered
			foreach (var feature in _inputFeatures.Values)
			{
				// update it
				try
				{
					feature.UpdateState();
				}
				catch (Exception)
				{
					Debug.LogError($"Problems occuring within {feature.Name}");
				}
			}
		}
	}
}
#endif