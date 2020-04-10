using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace ShoulderCam
{
    public class ShoulderCamMissionLogic : MissionLogic
    {
        private static readonly HashSet<ItemObject.ItemTypeEnum> MeleeWeaponTypes = new HashSet<ItemObject.ItemTypeEnum>
        {
            ItemObject.ItemTypeEnum.Horse,
            ItemObject.ItemTypeEnum.Polearm,
            ItemObject.ItemTypeEnum.Shield,
            ItemObject.ItemTypeEnum.OneHandedWeapon,
            ItemObject.ItemTypeEnum.TwoHandedWeapon,
        };

        public override void EarlyStart()
        {
            base.EarlyStart();
            ShoulderCamPatch.ShakeCamera(0, 0);
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, int damage, int weaponKind, int currentWeaponUsageIndex)
        {
            base.OnAgentHit(affectedAgent, affectorAgent, damage, weaponKind, currentWeaponUsageIndex);
            if (affectedAgent.IsMainAgent || affectedAgent.RiderAgent?.IsMainAgent == true)
            {
                ShoulderCamPatch.ShakeCamera(
                    SubModule.Config.MinimumPlayerHitCamShake + damage * SubModule.Config.PlayerHitCamShakeMultiplier,
                    SubModule.Config.PlayerHitCamShakeDuration
                );
            }
            else if (affectorAgent.IsMainAgent || affectorAgent.RiderAgent?.IsMainAgent == true)
            {
                var weapon = ItemObject.GetItemFromWeaponKind(weaponKind);
                if (weapon != null && MeleeWeaponTypes.Contains(weapon.Type))
                {
                    ShoulderCamPatch.ShakeCamera(
                        SubModule.Config.MinimumEnemyHitCamShakeAmount + damage * SubModule.Config.EnemyHitCamShakeMultiplier,
                        SubModule.Config.EnemyHitCamShakeDuration
                    );
                }
            }
        }
    }
}
