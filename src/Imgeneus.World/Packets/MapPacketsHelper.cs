﻿using System;
using Imgeneus.Database.Constants;
using Imgeneus.Network.Data;
using Imgeneus.Network.Packets;
using Imgeneus.World.Game;
using Imgeneus.World.Game.Monster;
using Imgeneus.World.Game.NPCs;
using Imgeneus.World.Game.PartyAndRaid;
using Imgeneus.World.Game.Player;
using Imgeneus.World.Game.Zone;
using Imgeneus.World.Serialization;

namespace Imgeneus.World.Packets
{
    /// <summary>
    /// Helps to creates packets and send them to players.
    /// </summary>
    internal class MapPacketsHelper
    {
        internal void SendCharacterLeftMap(WorldClient client, Character removedCharacter)
        {
            using var packet = new Packet(PacketType.CHARACTER_LEFT_MAP);
            packet.Write(removedCharacter.Id);
            client.SendPacket(packet);
        }

        internal void SendCharacterMoves(WorldClient client, Character movedPlayer)
        {
            using var packet = new Packet(PacketType.CHARACTER_MOVE);
            packet.Write(new CharacterMove(movedPlayer).Serialize());
            client.SendPacket(packet);
        }

        internal void SendCharacterMotion(WorldClient client, int characterId, Motion motion)
        {
            using var packet = new Packet(PacketType.CHARACTER_MOTION);
            packet.Write(characterId);
            packet.WriteByte((byte)motion);
            client.SendPacket(packet);
        }

        internal void SendCharacterUsedSkill(WorldClient client, Character sender, IKillable target, Skill skill, AttackResult attackResult)
        {
            PacketType skillType;
            if (target is Character)
            {
                skillType = PacketType.USE_CHARACTER_TARGET_SKILL;
            }
            else if (target is Mob)
            {
                skillType = PacketType.USE_MOB_TARGET_SKILL;
            }
            else
            {
                skillType = PacketType.USE_CHARACTER_TARGET_SKILL;
            }

            Packet packet = new Packet(skillType);
            var targetId = target is null ? 0 : target.Id;
            packet.Write(new SkillRange(sender.Id, targetId, skill, attackResult).Serialize());
            client.SendPacket(packet);
            packet.Dispose();
        }

        internal void SendCharacterConnectedToMap(WorldClient client, Character character)
        {
            using var packet0 = new Packet(PacketType.CHARACTER_ENTERED_MAP);
            packet0.Write(new CharacterEnteredMap(character).Serialize());
            client.SendPacket(packet0);

            using var packet1 = new Packet(PacketType.CHARACTER_MOVE);
            packet1.Write(new CharacterMove(character).Serialize());
            client.SendPacket(packet1);
        }

        internal void SendMobEntered(WorldClient client, Mob mob, bool isNew)
        {
            using var packet = new Packet(PacketType.MOB_ENTER);
            packet.Write(new MobEnter(mob, isNew).Serialize());
            client.SendPacket(packet);
        }

        internal void SendMobMove(WorldClient client, Mob mob)
        {
            using var packet = new Packet(PacketType.MOB_MOVE);
            packet.Write(new MobMove(mob).Serialize());
            client.SendPacket(packet);
        }

        internal void SendMobAttack(WorldClient client, Mob mob, int targetId, AttackResult attackResult)
        {
            using var packet = new Packet(PacketType.MOB_ATTACK);
            packet.Write(new MobAttack(mob, targetId, attackResult).Serialize());
            client.SendPacket(packet);
        }

        internal void SendMobUsedSkill(WorldClient client, Mob mob, int targetId, Skill skill, AttackResult attackResult)
        {
            using var packet = new Packet(PacketType.MOB_SKILL_USE);
            packet.Write(new MobSkillAttack(mob, targetId, skill, attackResult).Serialize());
            client.SendPacket(packet);
        }

        internal void SendCharacterChangedEquipment(WorldClient client, int characterId, Item equipmentItem, byte slot)
        {
            using var packet = new Packet(PacketType.SEND_EQUIPMENT);
            packet.Write(characterId);

            packet.WriteByte(slot);
            packet.WriteByte(equipmentItem is null ? (byte)0 : equipmentItem.Type);
            packet.WriteByte(equipmentItem is null ? (byte)0 : equipmentItem.TypeId);
            packet.WriteByte(equipmentItem is null ? (byte)0 : (byte)20); // TODO: implement enchant here.

            if (equipmentItem != null && equipmentItem.IsCloakSlot)
            {
                for (var i = 0; i < 6; i++) // Something about cloak.
                {
                    packet.WriteByte(0);
                }
            }

            client.SendPacket(packet);
        }

        internal void SendCharacterUsualAttack(WorldClient client, IKiller sender, IKillable target, AttackResult attackResult)
        {
            PacketType attackType;
            if (target is Character)
            {
                attackType = PacketType.CHARACTER_CHARACTER_AUTO_ATTACK;
            }
            else if (target is Mob)
            {
                attackType = PacketType.CHARACTER_MOB_AUTO_ATTACK;
            }
            else
            {
                attackType = PacketType.CHARACTER_CHARACTER_AUTO_ATTACK;
            }
            using var packet = new Packet(attackType);
            packet.Write(new UsualAttack(sender.Id, target.Id, attackResult).Serialize());
            client.SendPacket(packet);
        }

        internal void SendCharacterPartyChanged(WorldClient client, int characterId, PartyMemberType type)
        {
            using var packet = new Packet(PacketType.MAP_PARTY_SET);
            packet.Write(characterId);
            packet.Write((byte)type);
            client.SendPacket(packet);
        }

        internal void SendAttackAndMovementSpeed(WorldClient client, IKillable sender)
        {
            using var packet = new Packet(PacketType.CHARACTER_ATTACK_MOVEMENT_SPEED);
            packet.Write(new CharacterAttackAndMovement(sender).Serialize());
            client.SendPacket(packet);
        }

        internal void SendCharacterKilled(WorldClient client, Character character, IKiller killer)
        {
            using var packet = new Packet(PacketType.CHARACTER_DEATH);
            packet.Write(character.Id);
            packet.WriteByte(1); // killer type. 1 - another player.
            packet.Write(killer.Id);
            client.SendPacket(packet);
        }

        internal void SendSkillCastStarted(WorldClient client, Character sender, IKillable target, Skill skill)
        {
            PacketType type;
            if (target is Character)
                type = PacketType.CHARACTER_SKILL_CASTING;
            else if (target is Mob)
                type = PacketType.MOB_SKILL_CASTING;
            else
                type = PacketType.CHARACTER_SKILL_CASTING;

            using var packet = new Packet(type);
            packet.Write(new SkillCasting(sender.Id, target is null ? 0 : target.Id, skill).Serialize());
            client.SendPacket(packet);
        }

        internal void SendUsedItem(WorldClient client, Character sender, Item item)
        {
            using var packet = new Packet(PacketType.USE_ITEM);
            packet.Write(sender.Id);
            packet.Write(item.Bag);
            packet.Write(item.Slot);
            packet.Write(item.Type);
            packet.Write(item.TypeId);
            packet.Write(item.Count);
            client.SendPacket(packet);
        }

        internal void SendCharacterTeleport(WorldClient client, Character player)
        {
            using var packet = new Packet(PacketType.CHARACTER_MAP_TELEPORT);
            packet.Write(player.Id);
            packet.Write(player.Map.Id);
            packet.Write(player.PosX);
            packet.Write(player.PosY);
            packet.Write(player.PosZ);
            client.SendPacket(packet);
        }

        internal void SendRecoverCharacter(WorldClient client, IKillable sender, int hp, int mp, int sp)
        {
            // NB!!! In previous episodes and in china ep 8 with recover packet it's sent how much hitpoints recovered.
            // But in os ep8 this packet sends current hitpoints.
            using var packet = new Packet(PacketType.CHARACTER_RECOVER);
            packet.Write(sender.Id);
            packet.Write(sender.CurrentHP); // old eps: packet.Write(hp);
            packet.Write(sender.CurrentMP); // old eps: packet.Write(mp);
            packet.Write(sender.CurrentSP); // old eps: packet.Write(sp);
            client.SendPacket(packet);
        }

        internal void Send_Max_HP(WorldClient client, int id, int value)
        {
            using var packet = new Packet(PacketType.CHARACTER_MAX_HITPOINTS);
            packet.Write(id);
            packet.WriteByte(0); // 0 means max hp type.
            packet.Write(value);
            client.SendPacket(packet);
        }

        internal void SendSkillKeep(WorldClient client, int id, ushort skillId, byte skillLevel, AttackResult result)
        {
            using var packet = new Packet(PacketType.CHARACTER_SKILL_KEEP);
            packet.Write(id);
            packet.Write(skillId);
            packet.Write(skillLevel);
            packet.Write(result.Damage.HP);
            packet.Write(result.Damage.MP);
            packet.Write(result.Damage.SP);
            client.SendPacket(packet);
        }

        internal void SendShapeUpdate(WorldClient client, Character sender)
        {
            using var packet = new Packet(PacketType.CHARACTER_SHAPE_UPDATE);
            packet.Write(sender.Id);
            packet.Write((byte)sender.Shape);
            client.SendPacket(packet);
        }

        internal void SendUsedRangeSkill(WorldClient client, Character sender, IKillable target, Skill skill, AttackResult attackResult)
        {
            PacketType type;
            if (target is Character)
                type = PacketType.USE_CHARACTER_RANGE_SKILL;
            else if (target is Mob)
                type = PacketType.USE_MOB_RANGE_SKILL;
            else
                type = PacketType.USE_CHARACTER_RANGE_SKILL;

            using var packet = new Packet(type);
            packet.Write(new SkillRange(sender.Id, target.Id, skill, attackResult).Serialize());
            client.SendPacket(packet);
        }

        internal void SendDeadRebirth(WorldClient client, Character sender)
        {
            using var packet = new Packet(PacketType.DEAD_REBIRTH);
            packet.Write(sender.Id);
            packet.WriteByte(4); // rebirth type.
            packet.Write(sender.Exp);
            packet.Write(sender.PosX);
            packet.Write(sender.PosY);
            packet.Write(sender.PosZ);
            client.SendPacket(packet);
        }

        internal void SendCharacterRebirth(WorldClient client, IKillable sender)
        {
            using var packet = new Packet(PacketType.CHARACTER_LEAVE_DEAD);
            packet.Write(sender.Id);
            client.SendPacket(packet);
        }

        internal void SendMobDead(WorldClient client, IKillable sender, IKiller killer)
        {
            using var packet = new Packet(PacketType.MOB_DEATH);
            packet.Write(sender.Id);
            packet.WriteByte(1); // killer type. Always 1, since only player can kill the mob.
            packet.Write(killer.Id);
            client.SendPacket(packet);
        }

        internal void SendMobRecover(WorldClient client, IKillable sender)
        {
            using var packet = new Packet(PacketType.MOB_RECOVER);
            packet.Write(sender.Id);
            packet.Write(sender.CurrentHP);
            client.SendPacket(packet);
        }

        internal void SendAddItem(WorldClient client, Item item, float x, float y, float z)
        {
            using var packet = new Packet(PacketType.MAP_ADD_ITEM);
            packet.Write(item.Id);
            packet.WriteByte(1); // kind of item
            packet.Write(item.Type);
            packet.Write(item.TypeId);
            packet.Write(item.Count);
            packet.Write(x);
            packet.Write(y);
            packet.Write(z);
            client.SendPacket(packet);
        }

        internal void SendRemoveItem(WorldClient client, Item item)
        {
            using var packet = new Packet(PacketType.MAP_REMOVE_ITEM);
            packet.Write(item.Id);
            client.SendPacket(packet);
        }

        internal void SendNpcEnter(WorldClient client, Npc npc)
        {
            using var packet = new Packet(PacketType.MAP_NPC_ENTER);
            packet.Write(npc.Id);
            packet.Write(npc.Type);
            packet.Write(npc.TypeId);
            packet.Write(npc.PosX);
            packet.Write(npc.PosY);
            packet.Write(npc.PosZ);
            packet.Write(npc.Angle);
            client.SendPacket(packet);
        }

        internal void SendNpcLeave(WorldClient client, Npc npc)
        {
            using var packet = new Packet(PacketType.MAP_NPC_LEAVE);
            packet.Write(npc.Id);
            client.SendPacket(packet);
        }
    }
}
