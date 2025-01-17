using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DynamicPatcher;
using PatcherYRpp;
using PatcherYRpp.Utilities;
using Extension.Ext;
using Extension.INI;
using Extension.Script;
using Extension.Utilities;

namespace Extension.Utilities
{

    public static class ObjectTypeHelper
    {
        #region 类型装换
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CastToBullet(this Pointer<ObjectClass> pObject, out Pointer<BulletClass> pBullet)
        {
            try
            {
                return pObject.CastIf(AbstractType.Bullet, out pBullet);
            }
            catch (Exception) { }
            pBullet = IntPtr.Zero;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CastToBuilding(this Pointer<ObjectClass> pObject, out Pointer<BuildingClass> pBuilding)
        {
            try
            {
                return pObject.CastIf(AbstractType.Building, out pBuilding);
            }
            catch (Exception) { }
            pBuilding = IntPtr.Zero;
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CastToBuilding(this Pointer<TechnoClass> pTechno, out Pointer<BuildingClass> pBuilding)
        {
            return pTechno.Convert<ObjectClass>().CastToBuilding(out pBuilding);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CastToInfantry(this Pointer<ObjectClass> pObject, out Pointer<InfantryClass> pInfantry)
        {
            try
            {
                return pObject.CastIf(AbstractType.Infantry, out pInfantry);
            }
            catch (Exception) { }
            pInfantry = IntPtr.Zero;
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CastToInfantry(this Pointer<TechnoClass> pTechno, out Pointer<InfantryClass> pInfantry)
        {
            return pTechno.Convert<ObjectClass>().CastToInfantry(out pInfantry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CastToUnit(this Pointer<ObjectClass> pObject, out Pointer<UnitClass> pUnit)
        {
            try
            {
                return pObject.CastIf(AbstractType.Unit, out pUnit);
            }
            catch (Exception) { }
            pUnit = IntPtr.Zero;
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CastToUnit(this Pointer<TechnoClass> pTechno, out Pointer<UnitClass> pUnit)
        {
            return pTechno.Convert<ObjectClass>().CastToUnit(out pUnit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CastToAircraft(this Pointer<ObjectClass> pObject, out Pointer<AircraftClass> pAircraft)
        {
            try
            {
                return pObject.CastIf(AbstractType.Aircraft, out pAircraft);
            }
            catch (Exception) { }
            pAircraft = IntPtr.Zero;
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CastToAircraft(this Pointer<TechnoClass> pTechno, out Pointer<AircraftClass> pAircraft)
        {
            return pTechno.Convert<ObjectClass>().CastToAircraft(out pAircraft);
        }
        #endregion

        #region 状态检查
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDead(this Pointer<TechnoClass> pTechno)
        {
            return pTechno.IsNull || pTechno.Convert<ObjectClass>().IsDead() || pTechno.Ref.IsCrashing || pTechno.Ref.IsSinking;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDead(this Pointer<BulletClass> pBullet)
        {
            return pBullet.IsNull || pBullet.Convert<ObjectClass>().IsDead() || (pBullet.TryGetStatus(out BulletStatusScript status) && status.LifeData.IsDetonate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDead(this Pointer<ObjectClass> pObject)
        {
            return pObject.IsNull || pObject.Ref.Type.IsNull || pObject.Ref.Health <= 0 || !pObject.Ref.IsAlive;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInvisible(this Pointer<TechnoClass> pTechno)
        {
            return pTechno.IsNull || pTechno.Convert<ObjectClass>().IsInvisible();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInvisible(this Pointer<ObjectClass> pObject)
        {
            return pObject.IsNull || pObject.Ref.InLimbo; // || !pObject.Ref.IsVisible;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCloaked(this Pointer<TechnoClass> pTechno, bool includeCloaking = true)
        {
            return pTechno.IsNull || pTechno.Ref.CloakStates == CloakStates.Cloaked || !includeCloaking || pTechno.Ref.CloakStates == CloakStates.Cloaking;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDeadOrInvisible(this Pointer<TechnoClass> pTechno)
        {
            return pTechno.IsDead() || pTechno.IsInvisible();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDeadOrInvisible(this Pointer<BulletClass> pBullet)
        {
            return pBullet.IsDead() || pBullet.Convert<ObjectClass>().IsInvisible();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDeadOrInvisible(this Pointer<ObjectClass> pObject)
        {
            return pObject.IsDead() || pObject.IsInvisible();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDeadOrInvisibleOrCloaked(this Pointer<TechnoClass> pTechno)
        {
            return pTechno.IsDeadOrInvisible() || pTechno.IsCloaked();
        }

        public static bool InAir(this Pointer<TechnoClass> pTechno)
        {
            return pTechno.Ref.Base.Base.IsInAir();
        }

        public static bool IsImmune(this Pointer<TechnoClass> pTechno, bool checkStand = false)
        {
            bool immune = pTechno.Ref.Base.Type.Ref.Immune;
            if (!immune && checkStand && pTechno.AmIStand(out StandData standData))
            {
                immune = standData.Immune;
            }
            if (!immune)
            {
                immune = pTechno.Ref.Base.IsIronCurtained() || pTechno.Ref.IsForceShilded;
            }
            return immune;
        }
        #endregion

        public static bool IsBulletAndGetAttackAndHouse(this Pointer<ObjectClass> pObject, out Pointer<TechnoClass> pOwner, out Pointer<HouseClass> pHouse)
        {
            bool isBullet = false;
            if (isBullet = pObject.CastToBullet(out Pointer<BulletClass> pBullet))
            {
                pOwner = pBullet.Ref.Owner;
                pHouse = pBullet.GetHouse();
            }
            else
            {
                Pointer<TechnoClass> pTechno = pObject.Convert<TechnoClass>();
                pOwner = pTechno;
                pHouse = pTechno.Ref.Owner;
            }
            return isBullet;
        }

        public static void ClearAllTarget(this Pointer<TechnoClass> pTechno)
        {
            if (!pTechno.IsNull)
            {
                // 自身
                pTechno.Ref.Target = IntPtr.Zero;
                pTechno.Ref.SetTarget(IntPtr.Zero);
                pTechno.Convert<MissionClass>().Ref.QueueMission(Mission.Stop, true);
                // 子机管理器
                if (!pTechno.Ref.SpawnManager.IsNull)
                {
                    pTechno.Ref.SpawnManager.Ref.Destination = IntPtr.Zero;
                    pTechno.Ref.SpawnManager.Ref.Target = IntPtr.Zero;
                    pTechno.Ref.SpawnManager.Ref.SetTarget(IntPtr.Zero);
                }
                // 超时空传送
                if (!pTechno.Ref.TemporalImUsing.IsNull)
                {
                    pTechno.Ref.TemporalImUsing.Ref.LetGo();
                }
            }
        }

    }
}
