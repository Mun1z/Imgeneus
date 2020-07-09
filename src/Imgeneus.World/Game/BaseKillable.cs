﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Imgeneus.Database.Constants;
using Imgeneus.Database.Preload;
using Imgeneus.World.Game.Player;
using MvvmHelpers;

namespace Imgeneus.World.Game
{
    /// <summary>
    /// Abstract entity, that can be killed. Implements common features for killable object.
    /// </summary>
    public abstract class BaseKillable : IKillable, IDisposable
    {
        private readonly IDatabasePreloader _databasePreloader;

        public BaseKillable(IDatabasePreloader databasePreloader)
        {
            _databasePreloader = databasePreloader;
            ActiveBuffs.CollectionChanged += ActiveBuffs_CollectionChanged;
            PassiveBuffs.CollectionChanged += PassiveBuffs_CollectionChanged;
        }

        public virtual void Dispose()
        {
            ActiveBuffs.CollectionChanged -= ActiveBuffs_CollectionChanged;
        }

        private int _id;

        /// <inheritdoc />
        public int Id
        {
            get => _id;
            set
            {
                if (_id == 0)
                {
                    _id = value;
                }
                else
                {
                    throw new ArgumentException("Id can not be set twice.");
                }
            }
        }

        /// <inheritdoc />
        public ushort Level { get; protected set; }

        #region Current hitpoints

        public abstract int MaxHP { get; }

        /// <summary>
        /// Event, that is fired, when max hp changes.
        /// </summary>
        public event Action<IKillable, int> OnMaxHPChanged;

        public abstract int MaxSP { get; }

        /// <summary>
        /// Event, that is fired, when max sp changes.
        /// </summary>
        public event Action<IKillable, int> OnMaxSPChanged;

        public abstract int MaxMP { get; }

        /// <summary>
        /// Event, that is fired, when max mp changes.
        /// </summary>
        public event Action<IKillable, int> OnMaxMPChanged;

        /// <summary>
        /// Event, that is fired, when hp changes.
        /// </summary>
        public event Action<IKillable, HitpointArgs> HP_Changed;

        private int _currentHP;
        public int CurrentHP
        {
            get => _currentHP;
            protected set
            {
                if (_currentHP == value)
                    return;

                if (value > MaxHP)
                    value = MaxHP;

                var args = new HitpointArgs(_currentHP, value);
                _currentHP = value;
                if (_currentHP <= 0)
                {
                    _currentHP = 0;
                    IsDead = true;
                }

                HP_Changed?.Invoke(this, args);
            }
        }

        /// <summary>
        /// Event, that is fired, when mp changes.
        /// </summary>
        public event Action<IKillable, HitpointArgs> MP_Changed;

        private int _currentMP;
        public int CurrentMP
        {
            get => _currentMP;
            set
            {
                if (_currentMP == value)
                    return;

                if (value > MaxMP)
                    value = MaxMP;

                var args = new HitpointArgs(_currentMP, value);
                _currentMP = value;
                MP_Changed?.Invoke(this, args);
            }
        }

        /// <summary>
        /// Event, that is fired, when sp changes.
        /// </summary>
        public event Action<IKillable, HitpointArgs> SP_Changed;

        private int _currentSP;
        public int CurrentSP
        {
            get => _currentSP;
            set
            {
                if (_currentSP == value)
                    return;

                if (value > MaxSP)
                    value = MaxSP;

                var args = new HitpointArgs(_currentSP, value);
                _currentSP = value;
                SP_Changed?.Invoke(this, args);
            }
        }

        /// <inheritdoc />
        public void DecreaseHP(int hp, IKiller damageMaker)
        {
            if (hp == 0)
                return;

            MyKiller = damageMaker;
            CurrentHP -= hp;
        }

        /// <inheritdoc />
        public void IncreaseHP(int hp)
        {
            if (hp == 0)
                return;

            CurrentHP += hp;
        }

        #endregion

        #region Active buffs

        /// <summary>
        /// Active buffs, that increase character characteristic, attack, defense etc.
        /// Don't update it directly, use instead "AddActiveBuff".
        /// </summary>
        public ObservableRangeCollection<ActiveBuff> ActiveBuffs { get; private set; } = new ObservableRangeCollection<ActiveBuff>();

        /// <summary>
        /// Event, that is fired, when buff is added.
        /// </summary>
        public event Action<IKillable, ActiveBuff> OnBuffAdded;

        /// <summary>
        /// Event, that is fired, when buff is removed.
        /// </summary>
        public event Action<IKillable, ActiveBuff> OnBuffRemoved;

        /// <summary>
        /// Updates collection of active buffs.
        /// </summary>
        /// <param name="skill">skill, that client sends</param>
        /// <param name="creator">buff creator</param>
        /// <returns>Newly added or updated active buff</returns>
        public ActiveBuff AddActiveBuff(Skill skill, IKiller creator)
        {
            var resetTime = skill.KeepTime == 0 ? DateTime.UtcNow.AddDays(10) : DateTime.UtcNow.AddSeconds(skill.KeepTime);
            ActiveBuff buff;

            if (skill.IsPassive)
            {
                buff = PassiveBuffs.FirstOrDefault(b => b.SkillId == skill.SkillId);
            }
            else
            {
                buff = ActiveBuffs.FirstOrDefault(b => b.SkillId == skill.SkillId);
            }

            if (buff != null) // We already have such buff. Try to update reset time.
            {
                if (buff.SkillLevel > skill.SkillLevel)
                {
                    // Do nothing, if target already has higher lvl buff.
                    return buff;
                }
                else
                {
                    // If bufs are the same level, we should only update reset time.
                    if (buff.SkillLevel == skill.SkillLevel)
                    {
                        buff.ResetTime = resetTime;

                        // Send update of buff.
                        if (!buff.IsPassive)
                            BuffAdded(buff);
                    }

                    if (buff.SkillLevel < skill.SkillLevel)
                    {
                        // Remove old buff.
                        if (buff.IsPassive)
                            PassiveBuffs.Remove(buff);
                        else
                            ActiveBuffs.Remove(buff);

                        // Create new one with a higher level.
                        buff = new ActiveBuff(creator, skill)
                        {
                            ResetTime = resetTime
                        };
                        if (skill.IsPassive)
                            PassiveBuffs.Add(buff);
                        else
                            ActiveBuffs.Add(buff);
                    }
                }
            }
            else
            {
                // It's a new buff.
                buff = new ActiveBuff(creator, skill)
                {
                    ResetTime = resetTime
                };
                if (skill.IsPassive)
                    PassiveBuffs.Add(buff);
                else
                    ActiveBuffs.Add(buff);
            }

            return buff;
        }

        /// <summary>
        /// Fired, when new buff added or old deleted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ActiveBuffs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ActiveBuff newBuff in e.NewItems)
                {
                    newBuff.OnReset += ActiveBuff_OnReset;
                    ApplyBuffSkill(newBuff);
                }

                // Case, when we are starting up and all skills are added with AddRange call.
                if (e.NewItems.Count != 1)
                {
                    return;
                }

                BuffAdded((ActiveBuff)e.NewItems[0]);
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var buff = (ActiveBuff)e.OldItems[0];
                RelieveBuffSkill(buff);
                BuffRemoved(buff);
            }
        }

        private void ActiveBuff_OnReset(ActiveBuff sender)
        {
            sender.OnReset -= ActiveBuff_OnReset;
            ActiveBuffs.Remove(sender);
        }

        /// <summary>
        /// Call, that notifies about new added buff.
        /// </summary>
        protected virtual void BuffAdded(ActiveBuff buff)
        {
            OnBuffAdded?.Invoke(this, buff);
        }

        /// <summary>
        /// Call, that notifies about buff removed.
        /// </summary>
        protected virtual void BuffRemoved(ActiveBuff buff)
        {
            OnBuffRemoved?.Invoke(this, buff);
        }

        /// <summary>
        /// Call, that notifies about move or attack speed change.
        /// </summary>
        protected abstract void SendMoveAndAttackSpeed();

        /// <summary>
        /// Call, that notifies about extra stats change.
        /// </summary>
        protected abstract void SendAdditionalStats();

        /// <summary>
        /// ?
        /// </summary>
        public event Action<IKillable, ActiveBuff, AttackResult> OnSkillKeep;

        /// <summary>
        /// Applies buff effect.
        /// </summary>
        protected void ApplyBuffSkill(ActiveBuff buff)
        {
            var skill = _databasePreloader.Skills[(buff.SkillId, buff.SkillLevel)];
            switch (skill.TypeDetail)
            {
                case TypeDetail.Buff:
                case TypeDetail.PassiveDefence:
                    ApplyAbility(skill.AbilityType1, skill.AbilityValue1, true);
                    ApplyAbility(skill.AbilityType2, skill.AbilityValue2, true);
                    ApplyAbility(skill.AbilityType3, skill.AbilityValue3, true);
                    ApplyAbility(skill.AbilityType4, skill.AbilityValue4, true);
                    ApplyAbility(skill.AbilityType5, skill.AbilityValue5, true);
                    ApplyAbility(skill.AbilityType6, skill.AbilityValue6, true);
                    ApplyAbility(skill.AbilityType7, skill.AbilityValue7, true);
                    ApplyAbility(skill.AbilityType8, skill.AbilityValue8, true);
                    ApplyAbility(skill.AbilityType9, skill.AbilityValue9, true);
                    ApplyAbility(skill.AbilityType10, skill.AbilityValue10, true);
                    break;

                case TypeDetail.SubtractingDebuff:
                    ApplyAbility(skill.AbilityType1, skill.AbilityValue1, false);
                    ApplyAbility(skill.AbilityType2, skill.AbilityValue2, false);
                    ApplyAbility(skill.AbilityType3, skill.AbilityValue3, false);
                    ApplyAbility(skill.AbilityType4, skill.AbilityValue4, false);
                    ApplyAbility(skill.AbilityType5, skill.AbilityValue5, false);
                    ApplyAbility(skill.AbilityType6, skill.AbilityValue6, false);
                    ApplyAbility(skill.AbilityType7, skill.AbilityValue7, false);
                    ApplyAbility(skill.AbilityType8, skill.AbilityValue8, false);
                    ApplyAbility(skill.AbilityType9, skill.AbilityValue9, false);
                    ApplyAbility(skill.AbilityType10, skill.AbilityValue10, false);
                    break;

                case TypeDetail.PeriodicalHeal:
                    buff.TimeHealHP = skill.TimeHealHP;
                    buff.TimeHealMP = skill.TimeHealMP;
                    buff.TimeHealSP = skill.TimeHealSP;
                    buff.OnPeriodicalHeal += Buff_OnPeriodicalHeal;
                    buff.StartPeriodicalHeal();
                    break;

                case TypeDetail.PeriodicalDebuff:
                    buff.TimeHPDamage = skill.TimeDamageHP;
                    buff.TimeMPDamage = skill.TimeDamageMP;
                    buff.TimeSPDamage = skill.TimeDamageSP;
                    buff.TimeDamageType = skill.TimeDamageType;
                    buff.OnPeriodicalDebuff += Buff_OnPeriodicalDebuff;
                    buff.StartPeriodicalDebuff();
                    break;

                case TypeDetail.PreventAttack:
                case TypeDetail.Immobilize:
                    SendMoveAndAttackSpeed();
                    break;

                case TypeDetail.Stealth:
                    IsStealth = true;

                    var sprinterBuff = ActiveBuffs.FirstOrDefault(b => b.SkillId == 681 || b.SkillId == 114); // 114 (old ep) 681 (new ep) are unique numbers for sprinter buff.
                    if (sprinterBuff != null)
                        sprinterBuff.CancelBuff();
                    break;

                case TypeDetail.WeaponMastery:
                    if (!_weaponSpeedPassiveSkillModificator.ContainsKey(skill.Weapon1))
                        _weaponSpeedPassiveSkillModificator.Add(skill.Weapon1, skill.Weaponvalue);
                    else
                        _weaponSpeedPassiveSkillModificator[skill.Weapon1] = skill.Weaponvalue;

                    if (!_weaponSpeedPassiveSkillModificator.ContainsKey(skill.Weapon2))
                        _weaponSpeedPassiveSkillModificator.Add(skill.Weapon2, skill.Weaponvalue);
                    else
                        _weaponSpeedPassiveSkillModificator[skill.Weapon2] = skill.Weaponvalue;

                    SendMoveAndAttackSpeed();
                    break;

                default:
                    throw new NotImplementedException("Not implemented buff skill type.");
            }
        }

        /// <summary>
        /// Removes buff effect.
        /// </summary>
        protected void RelieveBuffSkill(ActiveBuff buff)
        {
            var skill = _databasePreloader.Skills[(buff.SkillId, buff.SkillLevel)];
            switch (skill.TypeDetail)
            {
                case TypeDetail.Buff:
                case TypeDetail.PassiveDefence:
                    ApplyAbility(skill.AbilityType1, skill.AbilityValue1, false);
                    ApplyAbility(skill.AbilityType2, skill.AbilityValue2, false);
                    ApplyAbility(skill.AbilityType3, skill.AbilityValue3, false);
                    ApplyAbility(skill.AbilityType4, skill.AbilityValue4, false);
                    ApplyAbility(skill.AbilityType5, skill.AbilityValue5, false);
                    ApplyAbility(skill.AbilityType6, skill.AbilityValue6, false);
                    ApplyAbility(skill.AbilityType7, skill.AbilityValue7, false);
                    ApplyAbility(skill.AbilityType8, skill.AbilityValue8, false);
                    ApplyAbility(skill.AbilityType9, skill.AbilityValue9, false);
                    ApplyAbility(skill.AbilityType10, skill.AbilityValue10, false);
                    break;

                case TypeDetail.SubtractingDebuff:
                    ApplyAbility(skill.AbilityType1, skill.AbilityValue1, true);
                    ApplyAbility(skill.AbilityType2, skill.AbilityValue2, true);
                    ApplyAbility(skill.AbilityType3, skill.AbilityValue3, true);
                    ApplyAbility(skill.AbilityType4, skill.AbilityValue4, true);
                    ApplyAbility(skill.AbilityType5, skill.AbilityValue5, true);
                    ApplyAbility(skill.AbilityType6, skill.AbilityValue6, true);
                    ApplyAbility(skill.AbilityType7, skill.AbilityValue7, true);
                    ApplyAbility(skill.AbilityType8, skill.AbilityValue8, true);
                    ApplyAbility(skill.AbilityType9, skill.AbilityValue9, true);
                    ApplyAbility(skill.AbilityType10, skill.AbilityValue10, true);
                    break;

                case TypeDetail.PeriodicalHeal:
                    buff.OnPeriodicalHeal -= Buff_OnPeriodicalHeal;
                    break;

                case TypeDetail.PeriodicalDebuff:
                    buff.OnPeriodicalDebuff -= Buff_OnPeriodicalDebuff;
                    break;

                case TypeDetail.PreventAttack:
                case TypeDetail.Immobilize:
                    SendMoveAndAttackSpeed();
                    break;

                case TypeDetail.Stealth:
                    IsStealth = ActiveBuffs.Any(b => _databasePreloader.Skills[(b.SkillId, b.SkillLevel)].TypeDetail == TypeDetail.Stealth);
                    break;

                case TypeDetail.WeaponMastery:
                    _weaponSpeedPassiveSkillModificator.Remove(skill.Weapon1);
                    _weaponSpeedPassiveSkillModificator.Remove(skill.Weapon2);

                    SendMoveAndAttackSpeed();
                    break;

                default:
                    throw new NotImplementedException("Not implemented buff skill type.");
            }
        }

        private void ApplyAbility(AbilityType abilityType, ushort abilityValue, bool addAbility)
        {
            switch (abilityType)
            {
                case AbilityType.None:
                    return;

                case AbilityType.PhysicalAttackRate:
                case AbilityType.ShootingAttackRate:
                    if (addAbility)
                        _skillPhysicalHittingChance += abilityValue;
                    else
                        _skillPhysicalHittingChance -= abilityValue;
                    return;

                case AbilityType.PhysicalEvationRate:
                case AbilityType.ShootingEvationRate:
                    if (addAbility)
                        _skillPhysicalEvasionChance += abilityValue;
                    else
                        _skillPhysicalEvasionChance -= abilityValue;
                    return;

                case AbilityType.MagicAttackRate:
                    if (addAbility)
                        _skillMagicHittingChance += abilityValue;
                    else
                        _skillMagicHittingChance -= abilityValue;
                    return;

                case AbilityType.MagicEvationRate:
                    if (addAbility)
                        _skillMagicEvasionChance += abilityValue;
                    else
                        _skillMagicEvasionChance -= abilityValue;
                    return;

                case AbilityType.CriticalAttackRate:
                    if (addAbility)
                        _skillCriticalHittingChance += abilityValue;
                    else
                        _skillCriticalHittingChance -= abilityValue;
                    return;

                case AbilityType.PhysicalAttackPower:
                case AbilityType.ShootingAttackPower:
                    if (addAbility)
                        _skillPhysicalAttackPower += abilityValue;
                    else
                        _skillPhysicalAttackPower -= abilityValue;
                    return;

                case AbilityType.MagicAttackPower:
                    if (addAbility)
                        _skillMagicAttackPower += abilityValue;
                    else
                        _skillMagicAttackPower -= abilityValue;
                    return;

                case AbilityType.Str:
                    if (addAbility)
                        ExtraStr += abilityValue;
                    else
                        ExtraStr -= abilityValue;

                    SendAdditionalStats();
                    return;

                case AbilityType.Rec:
                    if (addAbility)
                        ExtraRec += abilityValue;
                    else
                        ExtraRec -= abilityValue;

                    SendAdditionalStats();
                    return;

                case AbilityType.Int:
                    if (addAbility)
                        ExtraInt += abilityValue;
                    else
                        ExtraInt -= abilityValue;

                    SendAdditionalStats();
                    return;

                case AbilityType.Wis:
                    if (addAbility)
                        ExtraWis += abilityValue;
                    else
                        ExtraWis -= abilityValue;

                    SendAdditionalStats();
                    return;

                case AbilityType.Dex:
                    if (addAbility)
                        ExtraDex += abilityValue;
                    else
                        ExtraDex -= abilityValue;

                    SendAdditionalStats();
                    return;

                case AbilityType.Luc:
                    if (addAbility)
                        ExtraLuc += abilityValue;
                    else
                        ExtraLuc -= abilityValue;

                    SendAdditionalStats();
                    return;

                case AbilityType.HP:
                    if (addAbility)
                        ExtraHP += abilityValue;
                    else
                        ExtraHP -= abilityValue;
                    break;

                case AbilityType.MP:
                    if (addAbility)
                        ExtraMP += abilityValue;
                    else
                        ExtraMP -= abilityValue;
                    break;

                case AbilityType.SP:
                    if (addAbility)
                        ExtraSP += abilityValue;
                    else
                        ExtraSP -= abilityValue;
                    break;

                case AbilityType.PhysicalDefense:
                case AbilityType.ShootingDefense:
                    if (addAbility)
                        ExtraDefense += abilityValue;
                    else
                        ExtraDefense -= abilityValue;

                    SendAdditionalStats();
                    return;

                case AbilityType.MagicResistance:
                    if (addAbility)
                        ExtraResistance += abilityValue;
                    else
                        ExtraResistance -= abilityValue;

                    SendAdditionalStats();
                    return;

                case AbilityType.MoveSpeed:
                    if (addAbility)
                        MoveSpeed += abilityValue;
                    else
                        MoveSpeed -= abilityValue;
                    return;

                case AbilityType.AttackSpeed:
                    if (addAbility)
                        SetAttackSpeedModifier(abilityValue);
                    else
                        SetAttackSpeedModifier(-1 * abilityValue);
                    return;

                default:
                    throw new NotImplementedException($"Not implemented ability type {abilityType}");
            }
        }

        private void Buff_OnPeriodicalHeal(ActiveBuff buff, AttackResult healResult)
        {
            IncreaseHP(healResult.Damage.HP);
            CurrentMP += healResult.Damage.MP;
            CurrentSP += healResult.Damage.SP;

            OnSkillKeep?.Invoke(this, buff, healResult);
        }

        private void Buff_OnPeriodicalDebuff(ActiveBuff buff, AttackResult debuffResult)
        {
            var damage = debuffResult.Damage;

            if (buff.TimeDamageType == TimeDamageType.Percent)
            {
                damage = new Damage(
                    Convert.ToUInt16(CurrentHP * debuffResult.Damage.HP * 1.0 / 100),
                    Convert.ToUInt16(CurrentSP * debuffResult.Damage.SP * 1.0 / 100),
                    Convert.ToUInt16(CurrentMP * debuffResult.Damage.MP * 1.0 / 100));
            }

            DecreaseHP(damage.HP, buff.BuffCreator);
            CurrentMP -= damage.MP;
            CurrentSP -= damage.SP;

            OnSkillKeep?.Invoke(this, buff, new AttackResult(AttackSuccess.Normal, damage));
        }

        #endregion

        #region Passive buffs

        /// <summary>
        /// Passive buffs, that increase character characteristic, attack, defense etc.
        /// Don't update it directly, use instead "AddPassiveBuff".
        /// </summary>
        public ObservableRangeCollection<ActiveBuff> PassiveBuffs { get; private set; } = new ObservableRangeCollection<ActiveBuff>();

        private void PassiveBuffs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ActiveBuff newBuff in e.NewItems)
                {
                    newBuff.OnReset += PassiveBuff_OnReset;
                    ApplyBuffSkill(newBuff);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var buff = (ActiveBuff)e.OldItems[0];
                RelieveBuffSkill(buff);
            }
        }

        private void PassiveBuff_OnReset(ActiveBuff sender)
        {
            sender.OnReset -= PassiveBuff_OnReset;
            PassiveBuffs.Remove(sender);
        }

        #endregion

        #region Element

        /// <inheritdoc />
        public abstract Element DefenceElement { get; }

        /// <inheritdoc />
        public abstract Element AttackElement { get; }

        #endregion

        #region Death

        /// <inheritdoc />
        public event Action<IKillable, IKiller> OnDead;

        /// <inheritdoc />
        public IKiller MyKiller { get; protected set; }


        protected bool _isDead;

        /// <inheritdoc />
        public bool IsDead
        {
            get => _isDead;
            protected set
            {
                _isDead = value;

                if (_isDead)
                    OnDead?.Invoke(this, MyKiller);
            }
        }

        #endregion

        #region Position

        /// <inheritdoc />
        public float PosX { get; set; }

        /// <inheritdoc />
        public float PosY { get; set; }

        /// <inheritdoc />
        public float PosZ { get; set; }

        #endregion

        #region Total stats

        public abstract int TotalLuc { get; }

        public abstract int TotalWis { get; }

        public abstract int TotalDex { get; }

        #endregion

        #region Defense & Resistance

        public abstract int Defense { get; }

        public abstract int Resistance { get; }

        #endregion

        #region Extra stats

        /// <summary>
        /// Yellow strength stat, that is calculated based on worn items, orange stats and active buffs.
        /// </summary>
        public int ExtraStr { get; protected set; }

        /// <summary>
        /// Yellow dexterity stat, that is calculated based on worn items, orange stats and active buffs.
        /// </summary>
        public int ExtraDex { get; protected set; }

        /// <summary>
        /// Yellow rec stat, that is calculated based on worn items, orange stats and active buffs.
        /// </summary>
        public int ExtraRec { get; protected set; }

        /// <summary>
        /// Yellow intelligence stat, that is calculated based on worn items, orange stats and active buffs.
        /// </summary>
        public int ExtraInt { get; protected set; }

        /// <summary>
        /// Yellow luck stat, that is calculated based on worn items, orange stats and active buffs.
        /// </summary>
        public int ExtraLuc { get; protected set; }

        /// <summary>
        /// Yellow wisdom stat, that is calculated based on worn items, orange stats and active buffs.
        /// </summary>
        public int ExtraWis { get; protected set; }

        /// <summary>
        /// Physical defense from equipment and buffs.
        /// </summary>
        public int ExtraDefense { get; protected set; }

        /// <summary>
        /// Magical resistance from equipment and buffs.
        /// </summary>
        public int ExtraResistance { get; protected set; }

        private int _extraHP;
        /// <summary>
        /// Health points, that are provided by equipment and buffs.
        /// </summary>
        public int ExtraHP
        {
            get => _extraHP;
            protected set
            {
                _extraHP = value;

                if (CurrentHP > MaxHP)
                    CurrentHP = MaxHP;

                OnMaxHPChanged?.Invoke(this, MaxHP);
            }
        }

        private int _extraSP;
        /// <summary>
        /// Stamina points, that are provided by equipment and buffs.
        /// </summary>
        public int ExtraSP
        {
            get => _extraSP;
            protected set
            {
                _extraSP = value;

                if (CurrentSP > MaxSP)
                    CurrentSP = MaxSP;

                OnMaxSPChanged?.Invoke(this, MaxSP);
            }
        }

        private int _extraMP;
        /// <summary>
        /// Mana points, that are provided by equipment and buffs.
        /// </summary>
        public int ExtraMP
        {
            get => _extraMP;
            protected set
            {
                _extraMP = value;

                if (CurrentMP > MaxMP)
                    CurrentMP = MaxMP;

                OnMaxMPChanged?.Invoke(this, MaxMP);
            }
        }

        #endregion

        #region Hitting chances

        public abstract double PhysicalHittingChance { get; }

        public abstract double PhysicalEvasionChance { get; }

        public abstract double MagicHittingChance { get; }

        public abstract double MagicEvasionChance { get; }

        /// <summary>
        /// Possibility to hit enemy gained from skills.
        /// </summary>
        protected double _skillPhysicalHittingChance;

        /// <summary>
        /// Possibility to escape hit gained from skills.
        /// </summary>
        protected double _skillPhysicalEvasionChance;

        /// <summary>
        /// Possibility to make critical hit.
        /// </summary>
        protected double _skillCriticalHittingChance;

        /// <summary>
        /// Possibility to hit enemy gained from skills.
        /// </summary>
        protected double _skillMagicHittingChance;

        /// <summary>
        /// Possibility to escape hit gained from skills.
        /// </summary>
        protected double _skillMagicEvasionChance;

        /// <summary>
        /// Additional attack power.
        /// </summary>
        protected int _skillPhysicalAttackPower;

        /// <summary>
        /// Additional attack power.
        /// </summary>
        protected int _skillMagicAttackPower;

        /// <summary>
        /// Wapon speed calculated from passive skill. Key is weapon, value is speed modificator.
        /// </summary>
        protected readonly Dictionary<byte, byte> _weaponSpeedPassiveSkillModificator = new Dictionary<byte, byte>();

        #endregion

        #region Move & Attack speed

        /// <summary>
        /// Event, that is fired, when attack or move speed changes.
        /// </summary>
        public event Action<IKillable> OnAttackOrMoveChanged;

        protected void InvokeAttackOrMoveChanged()
        {
            OnAttackOrMoveChanged?.Invoke(this);
        }

        public abstract AttackSpeed AttackSpeed { get; }

        /// <summary>
        /// Attack speed modifier is made of equipment and buffs.
        /// </summary>
        protected int _attackSpeedModifier;

        /// <summary>
        /// Sets attack modifier.
        /// </summary>
        protected void SetAttackSpeedModifier(int speed)
        {
            if (speed == 0)
                return;

            _attackSpeedModifier += speed;
            InvokeAttackOrMoveChanged();
        }

        public abstract int MoveSpeed { get; protected set; }

        #endregion

        #region Stealth

        public abstract bool IsStealth { get; protected set; }

        #endregion
    }
}
