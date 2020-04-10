using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace CutThroughEveryone
{
    internal static class SliceLogic
    {
        private struct SliceMetadatum
        {
            public HashSet<Agent.UsageDirection> SliceDirections;
        }

        private static readonly SliceMetadatum BladeSliceMetadatum = new SliceMetadatum
        {
            SliceDirections = new HashSet<Agent.UsageDirection>
            {
                Agent.UsageDirection.AttackUp,
                Agent.UsageDirection.AttackDown,
                Agent.UsageDirection.AttackLeft,
                Agent.UsageDirection.AttackRight,
            },
        };

        private static readonly SliceMetadatum PolearmSliceMetadatum = new SliceMetadatum
        {
            SliceDirections = new HashSet<Agent.UsageDirection>
            {
                Agent.UsageDirection.AttackUp,
                Agent.UsageDirection.AttackDown,
                Agent.UsageDirection.AttackLeft,
                Agent.UsageDirection.AttackRight,
            },
        };

        private static readonly SliceMetadatum AxeSliceMetadatum = new SliceMetadatum
        {
            SliceDirections = new HashSet<Agent.UsageDirection>
            {
                Agent.UsageDirection.AttackLeft,
                Agent.UsageDirection.AttackRight,
            },
        };

        private static readonly IReadOnlyDictionary<WeaponClass, SliceMetadatum> WeaponClassSliceMetadata = new Dictionary<WeaponClass, SliceMetadatum>
        {
            [WeaponClass.Dagger] = BladeSliceMetadatum,
            [WeaponClass.OneHandedSword] = BladeSliceMetadatum,
            [WeaponClass.TwoHandedSword] = BladeSliceMetadatum,
            [WeaponClass.OneHandedAxe] = AxeSliceMetadatum,
            [WeaponClass.TwoHandedAxe] = AxeSliceMetadatum,
            [WeaponClass.LowGripPolearm] = PolearmSliceMetadatum,
            [WeaponClass.OneHandedPolearm] = PolearmSliceMetadatum,
            [WeaponClass.TwoHandedPolearm] = PolearmSliceMetadatum,
        };

        internal static bool ShouldSliceThrough(AttackCollisionData collisionData, Agent attacker, Agent victim)
        {
            if (victim == null || SubModule.Config.ShouldOnlyCutThroughWhenUnitIsKilled && (int)victim.Health != 0)
            {
                return false;
            }
            var weaponClass = attacker.WieldedWeapon.Weapons?.FirstOrDefault()?.WeaponClass ?? WeaponClass.Undefined;
            if (WeaponClassSliceMetadata.ContainsKey(weaponClass))
            {
                var weaponSliceMetadatum = WeaponClassSliceMetadata[weaponClass];
                var isSliceDirection = weaponSliceMetadatum.SliceDirections.Contains(collisionData.AttackDirection);
                if (isSliceDirection)
                {
                    return collisionData.InflictedDamage > collisionData.AbsorbedByArmor;
                }
            }
            return false;
        }
    }
}
