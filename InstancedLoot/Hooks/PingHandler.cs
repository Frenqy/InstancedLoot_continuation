using InstancedLoot.Components;
using RoR2;

namespace InstancedLoot.Hooks;

public sealed class PingHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.PingerController.RebuildPing += Rebuild;
        On.RoR2.PositionIndicator.UpdatePositions += Positions;
    }
    public override void UnregisterHooks()
    {
        On.RoR2.PingerController.RebuildPing -= Rebuild;
        On.RoR2.PositionIndicator.UpdatePositions -= Positions;
    }
    private void Rebuild(On.RoR2.PingerController.orig_RebuildPing orig, PingerController self, PingerController.PingInfo info)
    {
        if (!self.GetComponent<PingerControllerRenderBehaviour>()) self.gameObject.AddComponent<PingerControllerRenderBehaviour>();
        orig(self, info);
    }
    private void Positions(On.RoR2.PositionIndicator.orig_UpdatePositions orig, UICamera camera)
    {
        orig(camera);
        foreach (var indicator in PositionIndicator.instancesList)
        {
            if (!indicator.targetTransform || Utils.IsObjectInteractibleForCameraRigController(indicator.targetTransform.gameObject, camera.cameraRigController)) continue;
            if (indicator.insideViewObject) indicator.insideViewObject.SetActive(false);
            if (indicator.outsideViewObject) indicator.outsideViewObject.SetActive(false);
            if (indicator.alwaysVisibleObject) indicator.alwaysVisibleObject.SetActive(false);
        }
    }
}
