using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using DynamicPatcher;
using PatcherYRpp;
using PatcherYRpp.Utilities;
using Extension.Ext;
using Extension.EventSystems;
using Extension.INI;
using Extension.Utilities;
using System.Runtime.InteropServices.ComTypes;

namespace Extension.Script
{
    [Serializable]
    public enum AircraftGuardState
    {
        STOP, READY, GUARD, ROLLING, ATTACK, RELOAD
    }

    [Serializable]
    [GlobalScriptable(typeof(TechnoExt))]
    [UpdateAfter(typeof(TechnoStatusScript))]
    public class AircraftAreaGuardScript : TransformScriptable
    {
        public AircraftAreaGuardScript(TechnoExt owner) : base(owner) { }

        public AircraftGuardState State = AircraftGuardState.STOP;
        public bool Clockwise = false; // 顺时针巡航

        private IConfigWrapper<AircraftAreaGuardData> _data;
        private AircraftAreaGuardData data
        {
            get
            {
                if (null == _data)
                {
                    _data = Ini.GetConfig<AircraftAreaGuardData>(Ini.RulesDependency, section);
                }
                return _data.Data;
            }
        }

        private CoordStruct destCenter = default; // 航点
        private List<CoordStruct> destList = new List<CoordStruct>(); // 巡航的下一个实际坐标点
        private int destIndex = 0; // 巡航点序号

        private bool onStopCommand;

        public override bool OnAwake()
        {
            ILocomotion locomotion = null;
            if (!data.Enable || pTechno.Ref.Type.Ref.MissileSpawn || !pTechno.CastIf<AircraftClass>(AbstractType.Aircraft, out Pointer<AircraftClass> pAircraft)
                || (locomotion = pAircraft.Convert<FootClass>().Ref.Locomotor).ToLocomotionClass().Ref.GetClassID() != LocomotionClass.Fly)
            {
                return false;
            }
            return true;
        }

        public override void OnTransform(object sender, EventArgs args)
        {
            Pointer<TechnoClass> pTarget = ((TechnoTypeChangeEventArgs)args).pTechno;
            if (!pTarget.IsNull && pTarget == pTechno)
            {
                _data = null;
            }
        }


        public override void OnUpdate()
        {
            // AI不允许使用这个逻辑
            Pointer<HouseClass> pHouse = pTechno.Ref.Owner;
            if (pHouse.IsNull || !pHouse.Ref.ControlledByHuman())
            {
                return;
            }

            Pointer<MissionClass> pMission = pTechno.Convert<MissionClass>();
            Pointer<FootClass> pFoot = pTechno.Convert<FootClass>();
            ILocomotion locomotion = pFoot.Ref.Locomotor;

            // Pointer<FlyLocomotionClass> pFly = locomotion.ToLocomotionClass<FlyLocomotionClass>();

            // CoordStruct pos = pTechno.Ref.Base.Base.GetCoords();
            // Surface.Primary.Ref.DrawText(State.ToString(), pos, ColorStruct.White);
            // Surface.Primary.Ref.DrawText(pMission.Ref.CurrentMission.ToString() + " " + (pFly.Ref.IsElevating ? "IsElevating" : ""), pos + new CoordStruct(0, 0, 256), ColorStruct.Green);
            // if (default != destCenter)
            // {
            //     BulletEffectHelper.GreenCrosshair(destCenter, 512);
            // }

            switch (State)
            {
                case AircraftGuardState.READY:
                    // 停在机场等待起飞

                    bool inAir = pTechno.InAir();
                    if (inAir)
                    {
                        // 移动设定巡航点
                        if (data.AutoGuard)
                        {
                            switch (pMission.Ref.CurrentMission)
                            {
                                case Mission.Attack:
                                case Mission.Enter:
                                // 攻击或返航途中，什么都不做
                                    CancelAreaGuard();
                                    return;
                                case Mission.Guard:
                                case Mission.Area_Guard:
                                case Mission.Move:
                                    // 返航途中有新的移动指令
                                    // Logger.Log($"{Game.CurrentFrame} 返航中，设置巡航点 {pMission.Ref.CurrentMission}");
                                    if (SetupDestination())
                                    {
                                        if (onStopCommand)
                                        {
                                            // 按下S，跳过一次目的地更改
                                            onStopCommand = false;
                                            // Logger.Log($"{Game.CurrentFrame} [{section}]{pTechno} 跳过巡航");
                                        }
                                        else
                                        {
                                            // Logger.Log($"{Game.CurrentFrame} [{section}]{pTechno} 开始巡航");
                                            this.State = AircraftGuardState.GUARD;
                                        }
                                    }
                                    return;
                            }
                        }
                    }
                    else
                    {
                        onStopCommand = false;
                        // Logger.Log($"{Game.CurrentFrame} [{section}]{pTechno} 在地面待机中 DefaultToGuard = {data.DefaultToGuard}");
                        CancelAreaGuard();
                        // 弹药充足，自动起飞
                        if (data.DefaultToGuard && pTechno.Ref.Ammo >= data.MaxAmmo)
                        {
                            CoordStruct sourcePos = pTechno.Ref.Base.Base.GetCoords();
                            if (MapClass.Instance.TryGetCellAt(sourcePos, out Pointer<CellClass> pCell))
                            {
                                // Logger.Log($"{Game.CurrentFrame} [{section}]{pTechno} 在地面待机中，设置当前格子为新巡航点");
                                SetupDestination(pCell.Ref.GetCoordsWithBridge());
                                // Logger.Log($"{Game.CurrentFrame} [{section}]{pTechno} 开始警戒巡航");
                                this.State = AircraftGuardState.GUARD;
                            }
                        }
                    }
                    break;
                case AircraftGuardState.STOP:
                    // 什么都不做，等待飞机降落
                    if (pTechno.Ref.Base.GetHeight() <= 0)
                    {
                        State = AircraftGuardState.READY;
                    }
                    // 即使按下S之后，Mission仍然是Area_Guard，此时不能切换至Ready
                    switch (pMission.Ref.CurrentMission)
                    {
                        case Mission.Attack:
                        case Mission.Enter:
                        case Mission.Move: // 鼠标指令前往别处
                            // Logger.Log($"{Game.CurrentFrame} 按下了S，返航中 {pMission.Ref.CurrentMission}");
                            State = AircraftGuardState.READY;
                            return;
                    }
                    break;
                case AircraftGuardState.ATTACK:
                    // 飞机正在追击目标
                    Pointer<AbstractClass> pTarget = pTechno.Ref.Target;
                    if (pTarget.IsNull)
                    {
                        // 目标不存在，带着蛋飞，切换状态
                        if (default != destCenter && pTechno.Ref.Ammo >= data.MaxAmmo)
                        {
                            // 返回巡航状态
                            this.State = AircraftGuardState.GUARD;
                            pMission.Ref.ForceMission(Mission.Area_Guard);
                        }
                        else
                        {
                            // 返航
                            BackToAirport();
                            this.State = AircraftGuardState.RELOAD;
                        }
                    }
                    else if (data.ChaseRange > 0)
                    {
                        int dist = data.ChaseRange * 256;
                        CoordStruct targetPos = pTarget.Ref.GetCoords();
                        if (targetPos.DistanceFrom(pTechno.Ref.Base.Base.GetCoords()) > dist)
                        {
                            // 超出追击距离
                            pTechno.Ref.SetTarget(IntPtr.Zero);
                        }
                    }
                    break;
                case AircraftGuardState.RELOAD:
                    // 返航等待油满重新出发
                    if (!pTechno.InAir() && pTechno.Ref.Ammo >= data.MaxAmmo)
                    {
                        // Logger.Log($"{Game.CurrentFrame} [{section}]{pTechno} 油满出发");
                        State = AircraftGuardState.GUARD;
                        locomotion.Move_To(destCenter);
                        pMission.Ref.ForceMission(Mission.Area_Guard); // Mission 会变成 Guard
                    }
                    break;
                case AircraftGuardState.GUARD:
                case AircraftGuardState.ROLLING:
                    // Logger.Log($"{Game.CurrentFrame} [{section}]{pTechno} 当前任务{pMission.Ref.CurrentMission}");
                    // 巡航直到进入攻击状态或者按下S键停止
                    switch (pMission.Ref.CurrentMission)
                    {
                        case Mission.Guard: // 重新起飞前往记录的航点
                        case Mission.Area_Guard: // 正常巡航
                            break;
                        case Mission.Move: // 鼠标指令前往别处
                            if (data.AutoGuard)
                            {
                                // 巡航中，获得移动指令，会重设航点
                                // Logger.Log($"{Game.CurrentFrame} 巡航中，设置巡航点");
                                if (SetupDestination())
                                {
                                    State = AircraftGuardState.GUARD;
                                }
                                break;
                            }
                            CancelAreaGuard();
                            this.State = AircraftGuardState.STOP;
                            return;
                        case Mission.Enter:
                            // Logger.Log($"{Game.CurrentFrame} 从巡航中切换返航");
                            if (onStopCommand && data.DefaultToGuard && pTechno.Ref.Ammo >= data.MaxAmmo)
                            {
                                // 还有蛋，不返航，回基地巡逻
                                pMission.Ref.ForceMission(Mission.Area_Guard);
                            }
                            else
                            {
                                // 命令返航
                                this.State = AircraftGuardState.STOP;
                            }
                            return;
                        default:
                            // Logger.Log($"{Game.CurrentFrame} [{section}]{pTechno} 暂停巡航");
                            this.State = AircraftGuardState.RELOAD;
                            return;
                    }
                    // 不带蛋飞行
                    if (pTechno.Ref.Ammo == 0)
                    {
                        BackToAirport();
                        this.State = AircraftGuardState.RELOAD;
                        return;
                    }
                    // 当前飞机需要前往的目的地
                    CoordStruct destNow = default;
                    locomotion.Destination(destNow.GetThisPointer());
                    CoordStruct location = pTechno.Ref.Base.Base.GetCoords();
                    // BulletEffectHelper.GreenLine(destNow, location);
                    if (FoundAndAttack(location))
                    {
                        this.State = AircraftGuardState.ATTACK;
                    }
                    else
                    {
                        // BulletEffectHelper.GreenCell(destNow);
                        // 检查是否需要更换巡航点
                        if (destNow != destCenter && !destList.Contains(destNow) && pMission.Ref.CurrentMission != Mission.Area_Guard)
                        {
                            // Logger.Log($"{Game.CurrentFrame} [{section}]{pTechno} 目的地不在巡航计划内");
                            // WWSB, 机场有飞机停下去之后会召回在外面的飞机
                            locomotion.Move_To(destList[destIndex]);
                        }
                        // 无视Z坐标判断距离
                        CoordStruct posA = location;
                        posA.Z = 0;
                        CoordStruct posB = destNow;
                        posB.Z = 0;
                        bool changeDest = posA.DistanceFrom(posB) <= 512;
                        if (changeDest)
                        {
                            if (destIndex > 0)
                            {
                                // 进入转圈状态
                                State = AircraftGuardState.ROLLING;
                            }
                            // 清除飞机的目的地
                            pTechno.Ref.SetDestination(default(Pointer<CellClass>), false);
                            // Logger.Log($"{Game.CurrentFrame} [{section}]{pTechno} 前往下一个巡航点");
                            CoordStruct destNew = destList[destIndex];
                            locomotion.Move_To(destNew);
                            // BulletEffectHelper.BlueLine(destNew, location, 1, 450);
                            if (++destIndex >= destList.Count())
                            {
                                destIndex = 0;
                            }
                        }
                    }
                    break;
            }
        }

        public override void OnStopCommand()
        {
            onStopCommand = true;
            CancelAreaGuard();
            this.State = AircraftGuardState.STOP;
            // 任务改成返航
            pTechno.Ref.BaseMission.ForceMission(Mission.Enter);
        }

        public bool IsAreaGuardRolling()
        {
            switch (State)
            {
                case AircraftGuardState.GUARD:
                case AircraftGuardState.ROLLING:
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取移动目的地，设定航点，进入警戒巡航状态
        /// </summary>
        public void StartAreaGuard()
        {
            // Logger.Log($"{Game.CurrentFrame} [{section}]{pTechno} 按下Ctrl+Alt");
            switch (State)
            {
                case AircraftGuardState.READY:
                case AircraftGuardState.GUARD:
                case AircraftGuardState.ROLLING:
                    // 设定新的航点
                    CoordStruct dest = default;
                    Pointer<FootClass> pFoot = pTechno.Convert<FootClass>();
                    Pointer<AbstractClass> pDest = pFoot.Ref.Destination;
                    if (!pDest.IsNull)
                    {
                        dest = pDest.Ref.GetCoords();
                    }
                    else
                    {
                        pFoot.Ref.Locomotor.Destination(dest.GetThisPointer());
                    }
                    // CoordStruct pos = pTechno.Ref.Base.Base.GetCoords();
                    // BulletEffectHelper.RedLine(pos, dest);
                    // BulletEffectHelper.RedCrosshair(dest, 1024);
                    // Logger.Log($"{Game.CurrentFrame} [{section}]{pTechno} 获取到单位的目的地 {dest}");
                    if (SetupDestination(dest))
                    {
                        // Logger.Log($"{Game.CurrentFrame} [{section}]{pTechno} 开始警戒巡航");
                        this.State = AircraftGuardState.GUARD;
                    }
                    break;
            }
        }

        /// <summary>
        /// 设定新的巡航点
        /// </summary>
        /// <returns></returns>
        private bool SetupDestination()
        {
            CoordStruct dest = default;
            pTechno.Convert<FootClass>().Ref.Locomotor.Destination(dest.GetThisPointer());
            return SetupDestination(dest);
        }

        /// <summary>
        /// 设定新的巡航点
        /// </summary>
        /// <param name="dest"></param>
        /// <returns></returns>
        private bool SetupDestination(CoordStruct dest)
        {
            if (default != dest && dest != destCenter && !destList.Contains(dest))
            {
                // Logger.Log($"{Game.CurrentFrame} [{section}]{pTechno} 设定巡航点，目的地{dest}, 巡航中心点 {destCenter}");
                this.destCenter = dest;
                // 新建巡航点，以飞机所在的位置和目标的位置的朝向为参考方向，画16个点
                CoordStruct location = pTechno.Ref.Base.Base.GetCoords();
                // BulletEffectHelper.GreenLine(dest, location, 1, 450);
                DirStruct sourceDir = FLHHelper.Point2Dir(location, dest);
                double sourceRad = sourceDir.radians();
                CoordStruct flh = new CoordStruct(data.GuardRadius * 256, 0, 0);
                this.destList.Clear();
                bool clockwise = data.Clockwise;
                if (data.Randomwise)
                {
                    clockwise = MathEx.Random.Next(2) == 0;
                }
                this.Clockwise = clockwise; // 顺时针还是逆时针巡航
                for (int i = 0; i < 16; i++)
                {
                    DirStruct targetDir = FLHHelper.DirNormalized(i, 16);
                    double targetRad = targetDir.radians();
                    float angle = (float)(sourceRad - targetRad);
                    targetDir = FLHHelper.Radians2Dir(angle);
                    CoordStruct newDest = FLHHelper.GetFLHAbsoluteCoords(dest, flh, targetDir);
                    if (i == 0)
                    {
                        // 第一个肯定是队列头，位于飞机前进方向正前方
                        this.destList.Add(newDest);
                        // BulletEffectHelper.BlueCrosshair(newDest, 128, 1, 450);
                        // BulletEffectHelper.BlueLine(dest, newDest, 1, 450);
                    }
                    else
                    {
                        // 顺序添加为逆时针，插入第二位为顺时针
                        if (clockwise)
                        {
                            this.destList.Insert(1, newDest);
                        }
                        else
                        {
                            this.destList.Add(newDest);
                        }
                        // BulletEffectHelper.RedCrosshair(newDest, 128, 1, 450);
                    }
                }
                this.destIndex = 0;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 取消巡航
        /// </summary>
        private void CancelAreaGuard()
        {
            // Logger.Log($"{Game.CurrentFrame} [{section}]{pTechno} 取消巡航");
            this.destCenter = default;
            this.destList.Clear();
            this.destIndex = 0;
        }

        private void BackToAirport()
        {
            // Logger.Log($"{Game.CurrentFrame} [{section}]{pTechno} 返回机场");
            pTechno.Ref.SetTarget(IntPtr.Zero);
            pTechno.Convert<MissionClass>().Ref.ForceMission(Mission.Enter);
        }

        private bool FoundAndAttack(CoordStruct location)
        {
            if (data.AutoFire)
            {
                // 飞机有武器
                Pointer<WeaponStruct> pPrimary = pTechno.Ref.GetWeapon(0);
                bool hasPrimary = !pPrimary.IsNull && !pPrimary.Ref.WeaponType.IsNull && !pPrimary.Ref.WeaponType.Ref.NeverUse;
                Pointer<WeaponStruct> pSecondary = pTechno.Ref.GetWeapon(1);
                bool hasSecondary = !pSecondary.IsNull && !pSecondary.Ref.WeaponType.IsNull && !pSecondary.Ref.WeaponType.Ref.NeverUse;
                if (hasPrimary || hasSecondary)
                {
                    CoordStruct sourcePos = location;
                    if (!data.FindRangeAroundSelf)
                    {
                        // 以航点为中心搜索可以攻击的目标
                        sourcePos = destCenter;
                    }
                    int cellSpread = data.GuardRange;
                    // 搜索可以攻击的目标
                    Pointer<AbstractClass> pTarget = IntPtr.Zero;
                    // 使用Cell搜索目标
                    bool canAA = (hasPrimary && pPrimary.Ref.WeaponType.Ref.Projectile.Ref.AA) || (hasSecondary && pSecondary.Ref.WeaponType.Ref.Projectile.Ref.AA);
                    // 检索范围内的单位类型
                    List<Pointer<TechnoClass>> pTechnoList = FinderHelper.GetCellSpreadTechnos(sourcePos, cellSpread, false, canAA, false);
                    // TODO 对检索到的单位按威胁值排序
                    foreach (Pointer<TechnoClass> pTargetTechno in pTechnoList)
                    {
                        // 检查能否攻击
                        if (CheckTarget(pTargetTechno))
                        {
                            pTarget = pTargetTechno.Convert<AbstractClass>();
                            break;
                        }
                    }
                    // 检索不到常规目标，检索替身
                    if (pTarget.IsNull)
                    {
                        double dist = (cellSpread <= 0 ? 1 : cellSpread) * 256;
                        foreach (KeyValuePair<TechnoExt, StandData> stand in TechnoStatusScript.StandArray)
                        {
                            if (!stand.Key.OwnerObject.IsNull && null != stand.Value)
                            {
                                Pointer<TechnoClass> pStand = stand.Key.OwnerObject;
                                StandData standData = stand.Value;
                                if (!standData.Immune && pStand != pTechno
                                    && sourcePos.DistanceFrom(pStand.Ref.Base.Base.GetCoords()) <= dist
                                    && CheckTarget(pStand))
                                {
                                    pTarget = pStand.Convert<AbstractClass>();
                                    break;
                                }
                            }
                        }
                    }
                    // 检索到一个目标
                    if (!pTarget.IsNull)
                    {
                        pTechno.Ref.SetTarget(pTarget);
                        pTechno.Convert<MissionClass>().Ref.QueueMission(Mission.Attack, true);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CheckTarget(Pointer<TechnoClass> pTarget)
        {
            bool pick = false;
            Pointer<HouseClass> pHouse = pTechno.Ref.Owner;
            Pointer<HouseClass> pTargetHouse = pTarget.Ref.Owner;
            if (!pTarget.IsDeadOrInvisibleOrCloaked()
                && !pTarget.Ref.Type.Ref.Base.Insignificant
                && (!pTarget.Ref.Base.IsDisguised() || pTarget.Ref.IsClearlyVisibleTo(pHouse) || !(pTargetHouse = pTarget.Ref.Base.GetDisguiseHouse(true)).IsNull)
                && !pTargetHouse.Ref.IsAlliedWith(pHouse))
            {
                pick = true;
                // 检查平民
                if (pTarget.Ref.Owner.IsCivilian())
                {
                    // Ares 的平民敌对目标
                    pick = Ini.GetSection(Ini.RulesDependency, pTarget.Ref.Type.Ref.Base.Base.ID).Get("CivilianEnemy", false);
                    // Ares 的反击平民
                    if (!pick && pHouse.AutoRepel() && !pTarget.Ref.Target.IsNull && pTarget.Ref.Target.CastToTechno(out Pointer<TechnoClass> pTargetTarget))
                    {
                        pick = pHouse.Ref.IsAlliedWith(pTargetTarget.Ref.Owner);
                    }
                }
            }
            if (pick)
            {
                // 能否对其进行攻击
                pick = pTechno.CanAttack(pTarget, true);
            }
            return pick;
        }

    }
}
