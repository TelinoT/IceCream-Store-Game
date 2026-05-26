using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Pinwheel.Jupiter
{
    [AttributeUsage(AttributeTargets.Property)]
    public class JAnimatableAttribute : Attribute
    {
        public string DisplayName { get; set; }
        public JCurveOrGradient CurveOrGradient { get; set; }
        public JAnimateTarget Target { get; set; }

        public JAnimatableAttribute(string displayName, JCurveOrGradient curveOrGradient, JAnimateTarget target = JAnimateTarget.Material)
        {
            DisplayName = displayName;
            CurveOrGradient = curveOrGradient;
            Target = target;
        }
    }
}
