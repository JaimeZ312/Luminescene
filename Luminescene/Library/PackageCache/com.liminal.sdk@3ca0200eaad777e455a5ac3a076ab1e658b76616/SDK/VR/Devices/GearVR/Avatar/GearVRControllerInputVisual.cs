using Liminal.SDK.VR.Avatars.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Liminal.SDK.VR.Devices.GearVR.Avatar
{
    /// <summary>
    /// Represents an input visual component on the GearVR Controller.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class GearVRControllerInputVisual : VRControllerInputVisual
    {
        private static class Uniforms
        {
            public static readonly int _EmissionColor = Shader.PropertyToID("_EmissionColor");
        }

        private static readonly Color DefaultColor = Color.clear;

        private Renderer mRenderer;
        private MaterialPropertyBlock mPropertyBlock;
        private Color mColor = DefaultColor;
        private bool mStarted;
        private bool mHasColorSet;

        #region Properties

        /// <summary>
        /// Gets or sets the color of the input visual.
        /// </summary>
        public override Color Color
        {
            get { return mColor; }
            set
            {
                mHasColorSet = true;
                SetColor(value);
            }
        }

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            mRenderer = GetComponent<Renderer>();
        }

        private void Start()
        {
            mStarted = true;
            if (mHasColorSet)
            {
                SetColor(mColor);
            }
        }

        #endregion

        /// <summary>
        /// Resets the color override of the visual component.
        /// </summary>
        public override void ResetColor()
        {
            SetColor(DefaultColor);
            mHasColorSet = false;
        }

        private void SetColor(Color color)
        {
            mColor = color;
            if (!mStarted || mRenderer == null)
                return;

            mPropertyBlock = mPropertyBlock ?? new MaterialPropertyBlock();
            mPropertyBlock.SetColor(Uniforms._EmissionColor, mColor);
            mRenderer.SetPropertyBlock(mPropertyBlock);
        }
    }
}
