using UnityEngine;

/// <summary>
/// Single-owner broker for the Quest controller aim ray visual.
///
/// Multiple feature scripts (fireworks, lotus, pollen, growth, swing, cat ride, ...) each used
/// to spawn their own <c>LineRenderer</c> parented to the right or left controller. With several
/// of those active in a scene the player saw two or more overlapping rays per hand. This broker
/// arbitrates ownership: only the first feature to <see cref="TryClaim"/> a hand for a given
/// frame is allowed to render its visual. The others simply skip drawing for that frame.
///
/// Ownership is auto-released after a small grace period so a feature that goes silent (lost
/// focus, was destroyed, returned to idle) hands the ray off cleanly to the next requester.
/// </summary>
public static class QuestRayVisualBroker
{
    private const float OwnershipGraceSeconds = 0.15f;

    private static int rightOwnerId;
    private static float rightOwnerLastFrame;
    private static int leftOwnerId;
    private static float leftOwnerLastFrame;

    /// <summary>
    /// Request exclusive permission to draw an aim ray for the given hand this frame.
    /// Returns true if the caller may render its ray; false if another feature already owns it.
    /// </summary>
    public static bool TryClaim(object owner, bool rightHand)
    {
        if (owner == null)
        {
            return false;
        }

        int ownerId = owner.GetHashCode();
        float now = Time.unscaledTime;

        if (rightHand)
        {
            return TryClaim(ref rightOwnerId, ref rightOwnerLastFrame, ownerId, now);
        }

        return TryClaim(ref leftOwnerId, ref leftOwnerLastFrame, ownerId, now);
    }

    private static bool TryClaim(ref int currentOwner, ref float lastFrame, int ownerId, float now)
    {
        if (currentOwner == 0 || currentOwner == ownerId || (now - lastFrame) > OwnershipGraceSeconds)
        {
            currentOwner = ownerId;
            lastFrame = now;
            return true;
        }

        return false;
    }

    /// <summary>Explicitly relinquish ownership (e.g. on disable / destroy).</summary>
    public static void Release(object owner, bool rightHand)
    {
        if (owner == null)
        {
            return;
        }

        int ownerId = owner.GetHashCode();
        if (rightHand && rightOwnerId == ownerId)
        {
            rightOwnerId = 0;
        }
        else if (!rightHand && leftOwnerId == ownerId)
        {
            leftOwnerId = 0;
        }
    }
}
