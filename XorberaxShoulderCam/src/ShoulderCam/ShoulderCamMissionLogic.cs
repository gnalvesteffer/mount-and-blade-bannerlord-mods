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

        public override void OnAgentHit(
            Agent affectedAgent,
            Agent affectorAgent,
            int damage,
            in MissionWeapon affectorWeapon
        )
        {
            base.OnAgentHit(affectedAgent, affectorAgent, damage, in affectorWeapon);
            if (affectedAgent.IsMainAgent || affectedAgent.RiderAgent?.IsMainAgent == true)
            {
                ShoulderCamPatch.ShakeCamera(
                    SubModule.Config.MinimumPlayerHitCamShake + damage * SubModule.Config.PlayerHitCamShakeMultiplier,
                    SubModule.Config.PlayerHitCamShakeDuration
                );
            }
            else if (affectorAgent.IsMainAgent || affectorAgent.RiderAgent?.IsMainAgent == true)
            {
                if (affectorWeapon.Item != null && MeleeWeaponTypes.Contains(affectorWeapon.Item.Type))
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
