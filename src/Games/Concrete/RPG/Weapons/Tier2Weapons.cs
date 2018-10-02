﻿using PacManBot.Extensions;

namespace PacManBot.Games.Concrete.Rpg.Weapons
{
    public class Bow : Weapon
    {
        public override string Name => "Bow";
        public override string Description => "Simple and effective.";
        public override int Damage => 9;
        public override double CritChance => 0.1f;
        public override DamageType Type => DamageType.Pierce;

        public override int LevelGet => 9;
    }


    public class ForestSword : Weapon
    {
        public override string Name => "Swordwood";
        public override string Description => "Better than a wooden sword.";
        public override int Damage => 11;
        public override double CritChance => 0.01f;
        public override DamageType Type => DamageType.Cutting;
        public override MagicType Magic => MagicType.Earth;

        public override int LevelGet => 10;
    }


    public class Shield : Weapon
    {
        public override string Name => "Spiky Shield";
        public override string Description => "Also raises defense by 2.";
        public override int Damage => 9;
        public override double CritChance => 0.02f;
        public override DamageType Type => DamageType.Blunt;

        public override int LevelGet => 11;

        public override void EquipEffects(Player player)
        {
            base.EquipEffects(player);
            player.Defense += 2;
        }
    }


    public class SimpleSpell : Weapon
    {
        public override string Name => "Enchantio";
        public override string Description => "An air spell grimoire.\nMight reduce enemy defense.";
        public override int Damage => 9;
        public override double CritChance => 0.05f;
        public override DamageType Type => DamageType.Magic;
        public override MagicType Magic => MagicType.Air;

        public override int LevelGet => 12;

        public override string AttackEffects(Player wielder, Entity target)
        {
            if (Bot.Random.OneIn(3) && !target.Buffs.ContainsKey(nameof(Buffs.Vulnerable)))
            {
                target.AddBuff(nameof(Buffs.Vulnerable), 3);
                return $"{target} is now vulnerable!";
            }
            return "";
        }
    }
}
