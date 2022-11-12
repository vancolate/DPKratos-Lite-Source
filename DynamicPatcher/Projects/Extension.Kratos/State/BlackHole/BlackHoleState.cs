using System;
using System.Collections.Generic;
using System.Linq;
using DynamicPatcher;
using PatcherYRpp;
using Extension.Ext;
using Extension.INI;
using Extension.Script;
using Extension.Utilities;

namespace Extension.Ext
{
    public interface IBlackHoleVictim
    {
        void SetBlackHole(Pointer<ObjectClass> pBlackHole, BlackHoleData data);
    }

    [Serializable]
    public class BlackHoleState : State<BlackHoleData>
    {
        private bool isElite;

        private int count;
        private int delay;
        private TimerStruct delayTimer;

        public override void OnEnable()
        {
            this.count = 0;
        }

        private void Reload(int delay)
        {
            this.delay = delay;
            if (delay > 0)
            {
                this.delayTimer.Start(delay);
            }
            count++;
            if (IsDone())
            {
                Disable();
            }
        }

        public bool IsReady()
        {
            return IsActive() && !IsDone() && Timeup();
        }

        private bool Timeup()
        {
            return this.delay <= 0 || delayTimer.Expired();
        }

        private bool IsDone()
        {
            return Data.TriggeredTimes > 0 && count >= Data.TriggeredTimes;
        }

        private BlackHole GetData()
        {
            if (isElite)
            {
                return Data.EliteData;
            }
            return Data.Data;
        }

        public void StartCapture(Pointer<ObjectClass> pBlackHole, Pointer<HouseClass> pHouse)
        {
            this.isElite = pBlackHole.CastToTechno(out Pointer<TechnoClass> pTechno) && pTechno.Ref.Veterancy.IsElite();
            BlackHole data = GetData();
            if (null != data && data.Range > 0)
            {
                Reload(data.Rate);
                // 检查平民
                if (Data.DeactiveWhenCivilian && pHouse.IsCivilian())
                {
                    return;
                }
                CoordStruct location = pBlackHole.Ref.Base.GetCoords();
                if (Data.AffectBullet)
                {
                    // 查找所有的抛射体
                    FinderHelper.FindBulletOnMark((pTarget, aem) =>
                    {
                        if (pTarget.TryGetStatus(out BulletStatusScript bulletStatus) && !bulletStatus.LifeData.IsDetonate
                            && (Data.AffectBlackHole || !bulletStatus.BlackHoleState.IsActive()) // 黑洞不能捕获另一个黑洞
                        )
                        {
                            // Logger.Log($"{Game.CurrentFrame} 黑洞 [{pObject.Ref.Type.Ref.Base.ID}]{pObject} 捕获抛射体 [{pTarget.Ref.Type.Ref.Base.Base.ID}]{pTarget}");
                            bulletStatus.SetBlackHole(pBlackHole, Data);
                        }
                        return false;
                    }, location, data.Range, 0, pHouse, Data, pBlackHole);
                }
                if (Data.AffectTechno)
                {
                    // 查找所有的单位
                    FinderHelper.FindTechnoOnMark((pTarget, aem) =>
                    {
                        if ((Data.Weight <= 0 || pTarget.Ref.Type.Ref.Weight <= Data.Weight) // 排除质量较大的对象
                            && pTarget.TryGetStatus(out TechnoStatusScript targetStatus)
                            && (Data.AffectBlackHole || !targetStatus.BlackHoleState.IsActive()) // 黑洞不能捕获另一个黑洞
                        )
                        {
                            // Logger.Log($"{Game.CurrentFrame} 黑洞 [{pObject.Ref.Type.Ref.Base.ID}]{pObject} 捕获单位 [{id}]{pTarget}");
                            targetStatus.SetBlackHole(pBlackHole, Data);
                        }
                        return false;
                    }, location, data.Range, 0, pHouse, Data, pBlackHole);
                }
            }
        }

        public bool IsOutOfRange(double distance)
        {
            BlackHole data = GetData();
            return null == data || data.Range <= 0 || distance > data.Range * 256;
        }

        public bool IsOnMark(Pointer<ObjectClass> pTarget)
        {
            return null == Data.OnlyAffectMarks || !Data.OnlyAffectMarks.Any()
                || (pTarget.TryGetAEManager(out AttachEffectScript aem)
                    && aem.TryGetMarks(out HashSet<string> marks)
                    && (Data.OnlyAffectMarks.Intersect(marks).Count() > 0)
                );
        }

    }

}
