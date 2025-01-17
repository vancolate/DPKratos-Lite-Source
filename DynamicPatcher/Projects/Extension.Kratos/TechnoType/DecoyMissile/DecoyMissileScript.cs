using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using DynamicPatcher;
using PatcherYRpp;
using PatcherYRpp.Utilities;
using Extension.Ext;
using Extension.INI;
using Extension.Utilities;

namespace Extension.Script
{


    [Serializable]
    [GlobalScriptable(typeof(TechnoExt))]
    public class DecoyMissileScript : TechnoScriptable
    {
        public DecoyMissileScript(TechnoExt owner) : base(owner) { }

        public DecoyMissile decoyMissile;

        public override void Awake()
        {
            DecoyMissileData data = Ini.GetConfig<DecoyMissileData>(Ini.RulesDependency, section).Data;
            if (!data.Enable)
            {
                GameObject.RemoveComponent(this);
                return;
            }
            Pointer<WeaponTypeClass> pWeapon = IntPtr.Zero;
            Pointer<WeaponTypeClass> pEliteWeapon = IntPtr.Zero;
            if (!data.Weapon.IsNullOrEmptyOrNone())
            {
                pWeapon = WeaponTypeClass.ABSTRACTTYPE_ARRAY.Find(data.Weapon);
                pEliteWeapon = pWeapon;
            }
            if (!data.EliteWeapon.IsNullOrEmptyOrNone())
            {
                pEliteWeapon = WeaponTypeClass.ABSTRACTTYPE_ARRAY.Find(data.EliteWeapon);
            }
            if (pWeapon.IsNull && pEliteWeapon.IsNull)
            {
                GameObject.RemoveComponent(this);
                return;
            }
            if (default == data.Velocity && pWeapon.Ref.Projectile.Ref.Arcing)
            {
                data.Velocity = data.FLH;
            }
            decoyMissile = new DecoyMissile(data, pWeapon, pEliteWeapon, pTechno.Ref.Veterancy.IsElite());
        }

        public override void OnUpdate()
        {
            if (null != decoyMissile && decoyMissile.Enable && !decoyMissile.Weapon.IsNull && !decoyMissile.EliteWeapon.IsNull)
            {
                CoordStruct location = pTechno.Ref.Base.Base.GetCoords();
                Pointer<WeaponTypeClass> pWeapon = decoyMissile.FindWeapon(pTechno.Ref.Veterancy.IsElite());
                int distance = pWeapon.Ref.Range;
                // remove dead decoy
                decoyMissile.ClearDecoy();

                // Fire decoy
                if (decoyMissile.Fire)
                {
                    if (decoyMissile.DropOne())
                    {
                        FacingStruct facing = pTechno.Ref.GetRealFacing();

                        CoordStruct flhL = decoyMissile.Data.FLH;
                        if (flhL.Y > 0)
                        {
                            flhL.Y = -flhL.Y;
                        }
                        CoordStruct flhR = decoyMissile.Data.FLH;
                        if (flhR.Y < 0)
                        {
                            flhR.Y = -flhR.Y;
                        }

                        CoordStruct portL = FLHHelper.GetFLH(location, flhL, facing.target());
                        CoordStruct portR = FLHHelper.GetFLH(location, flhR, facing.target());

                        CoordStruct targetFLHL = flhL + new CoordStruct(0, -distance * 2, 0);
                        CoordStruct targetFLHR = flhR + new CoordStruct(0, distance * 2, 0);
                        CoordStruct targetL = FLHHelper.GetFLH(location, targetFLHL, facing.target());
                        CoordStruct targetR = FLHHelper.GetFLH(location, targetFLHR, facing.target());

                        CoordStruct vL = decoyMissile.Data.Velocity;
                        if (vL.Y > 0)
                        {
                            vL.Y = -vL.Y;
                        }
                        vL.Z *= 2;
                        CoordStruct vR = decoyMissile.Data.Velocity;
                        if (vR.Y < 0)
                        {
                            vR.Y = -vR.Y;
                        }
                        vR.Z *= 2;
                        CoordStruct velocityL = FLHHelper.GetFLH(new CoordStruct(), vL, facing.target());
                        CoordStruct velocityR = FLHHelper.GetFLH(new CoordStruct(), vR, facing.target());
                        for (int i = 0; i < 2; i++)
                        {
                            CoordStruct initTarget = targetL;
                            CoordStruct port = portL;
                            BulletVelocity velocity = new BulletVelocity(velocityL.X, velocityL.Y, velocityL.Z);
                            if (i > 0)
                            {
                                initTarget = targetR;
                                port = portR;
                                velocity = new BulletVelocity(velocityR.X, velocityR.Y, velocityR.Z);
                            }
                            Pointer<CellClass> pCell = MapClass.Instance.GetCellAt(initTarget);
                            Pointer<BulletClass> pBullet = pWeapon.Ref.Projectile.Ref.CreateBullet(pCell.Convert<AbstractClass>(), pTechno, pWeapon.Ref.Damage, pWeapon.Ref.Warhead, pWeapon.Ref.Speed, pWeapon.Ref.Bright);
                            pBullet.Ref.WeaponType = pWeapon;
                            pBullet.Ref.MoveTo(port, velocity);
                            decoyMissile.AddDecoy(pBullet, port, decoyMissile.Data.Life);
                        }
                    }
                    // 将来袭导弹目标设定到最近的诱饵上
                    pTechno.FindBulletTargetMe((pBullet) =>
                    {
                        CoordStruct pos = pBullet.Ref.Base.Base.GetCoords();
                        Pointer<BulletClass> pDecoy = decoyMissile.CloseEnoughDecoy(pos, location.DistanceFrom(pos));
                        if (!pDecoy.IsDeadOrInvisible()
                            && pDecoy.Ref.Base.Base.GetCoords().DistanceFrom(pBullet.Ref.Base.Base.GetCoords()) <= distance * 2)
                        {
                            pBullet.Ref.SetTarget(pDecoy.Convert<AbstractClass>());
                            return true;
                        }
                        return false;
                    });
                }
                else if (pTechno.InAir())
                {
                    // 检查到有一发朝向自己发射的导弹时，启动热诱弹发射
                    pTechno.FindBulletTargetMe((pBullet) =>
                    {
                        if (location.DistanceFrom(pBullet.Ref.Base.Base.GetCoords()) <= distance)
                        {
                            decoyMissile.Fire = true; // 开始抛洒诱饵
                            CoordStruct pos = pBullet.Ref.Base.Base.GetCoords();
                            Pointer<BulletClass> pDecoy = decoyMissile.CloseEnoughDecoy(pos, location.DistanceFrom(pos));
                            if (!pDecoy.IsDeadOrInvisible())
                            {
                                pBullet.Ref.SetTarget(pDecoy.Convert<AbstractClass>());
                            }
                            return true;
                        }
                        return false;
                    });
                }
            }
        }
    }
}