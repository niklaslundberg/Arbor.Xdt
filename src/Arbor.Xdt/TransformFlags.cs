using System;

namespace Arbor.Xdt
{
    [Flags]
    public enum TransformFlags
    {
        None = 0,
        ApplyTransformToAllTargetNodes = 1,
        UseParentAsTargetNode = 2
    }
}