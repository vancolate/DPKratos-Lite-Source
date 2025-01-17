using System;
using System.Linq;
using DynamicPatcher;
using PatcherYRpp;
using Extension.Ext;
using Extension.INI;
using Extension.Script;
using Extension.Utilities;

namespace Extension.Ext
{

    public partial class AttachEffectData
    {
        public InfoData InfoData;

        private void ReadInfoData(IConfigReader reader)
        {
            InfoData data = new InfoData(reader);
            if (data.Enable)
            {
                this.InfoData = data;
                this.Enable = true;
            }
        }
    }

    [Serializable]
    public enum InfoMode
    {
        NONE = 0, TEXT = 1, SHP = 2, IMAGE = 3
    }

    public class InfoModeParser : KEnumParser<InfoMode>
    {
        public override bool ParseInitials(string t, ref InfoMode buffer)
        {
            switch (t)
            {
                case "T":
                    buffer = InfoMode.TEXT;
                    return true;
                case "S":
                    buffer = InfoMode.SHP;
                    return true;
                case "I":
                    buffer = InfoMode.IMAGE;
                    return true;
                default:
                    buffer = InfoMode.NONE;
                    return true;
            }
        }
    }

    [Serializable]
    public enum SortType
    {
        FIRST = 0, MIN = 1, MAX = 2
    }
    public class SortTypeParser : KEnumParser<SortType>
    {
        public override bool Parse(string val, ref SortType buffer)
        {
            if (!string.IsNullOrEmpty(val))
            {
                string t = val.Substring(0, 3).ToUpper();
                return ParseInitials(t, ref buffer);
            }
            return false;
        }

        public override bool ParseInitials(string t, ref SortType buffer)
        {
            switch (t)
            {
                case "MIN":
                    buffer = SortType.MIN;
                    return true;
                case "MAX":
                    buffer = SortType.MAX;
                    return true;
                default:
                    buffer = SortType.FIRST;
                    return true;
            }
        }
    }

    [Serializable]
    public class InfoEntity : PrintTextData
    {
        static InfoEntity()
        {
            new InfoModeParser().Register();
            new SortTypeParser().Register();
        }

        public string Watch;

        public InfoMode Mode;
        public bool ShowEnemy;
        public bool OnlySelected;

        public SortType Sort;

        public InfoEntity() : base()
        {
            this.Watch = null;

            this.Mode = InfoMode.NONE;
            this.ShowEnemy = true;
            this.OnlySelected = false;

            this.Sort = SortType.FIRST;

            this.Align = PrintTextAlign.CENTER;
            this.Color = new ColorStruct(0, 252, 0);
        }

        public override void Read(ISectionReader reader, string title)
        {
            Read(reader, title, Watch);
        }

        public void Read(ISectionReader reader, string title, string watch)
        {
            base.Read(reader, title);
            this.Watch = reader.Get(title + "Watch", watch); // 默认值由外部传入
            this.Mode = reader.Get(title + "Mode", Mode);
            switch (Mode)
            {
                case InfoMode.SHP:
                    this.UseSHP = true;
                    break;
                case InfoMode.IMAGE:
                    this.UseSHP = true;
                    this.SHPDrawStyle = SHPDrawStyle.PROGRESS;
                    break;
            }
            this.ShowEnemy = reader.Get(title + "ShowEnemy", ShowEnemy);
            this.OnlySelected = reader.Get(title + "OnlySelected", OnlySelected);

            this.Sort = reader.Get(title + "Sort", Sort);
        }
    }

    [Serializable]
    public class InfoData : EffectData
    {

        public const string TITLE = "Info.";

        public InfoEntity Duration;
        public InfoEntity Delay;
        public InfoEntity InitDelay;
        public InfoEntity Stack;

        public InfoEntity Health;
        public InfoEntity Ammo;
        public InfoEntity Reload;
        public InfoEntity ROF;

        public InfoEntity ID;
        public InfoEntity Armor;
        public InfoEntity Mission;

        public InfoEntity Target;
        public InfoEntity Dest;
        public InfoEntity Location;
        public InfoEntity Cell;
        public InfoEntity BodyDir;
        public InfoEntity TurretDir;

        public InfoData()
        {
            this.Duration = new InfoEntity();
            this.Delay = new InfoEntity();
            this.InitDelay = new InfoEntity();
            this.Stack = new InfoEntity();

            this.Health = new InfoEntity();
            this.Ammo = new InfoEntity();
            this.Reload = new InfoEntity();
            this.ROF = new InfoEntity();

            this.ID = new InfoEntity();
            this.Armor = new InfoEntity();
            this.Mission = new InfoEntity();

            this.Target = new InfoEntity();
            this.Target.Color = new ColorStruct(252, 0, 0);
            this.Dest = new InfoEntity();
            this.Dest.Color = new ColorStruct(252, 0, 0);
            this.Location = new InfoEntity();
            this.Location.Color = new ColorStruct(0, 252, 0);
            this.Cell = new InfoEntity();
            this.Cell.Color = new ColorStruct(0, 252, 0);
            this.BodyDir = new InfoEntity();
            this.BodyDir.Color = new ColorStruct(0, 252, 0);
            this.TurretDir = new InfoEntity();
            this.TurretDir.Color = new ColorStruct(0, 0, 252);
        }

        public InfoData(IConfigReader reader) : this()
        {
            Read(reader);
        }

        public override void Read(IConfigReader reader)
        {
            base.Read(reader, TITLE);

            string watch = reader.Get<string>(TITLE + "Watch", null);

            this.Duration.Read(reader, TITLE + "Duration.", watch);
            this.Delay.Read(reader, TITLE + "Delay.", watch);
            this.InitDelay.Read(reader, TITLE + "InitDelay.", watch);
            this.Stack.Read(reader, TITLE + "Stack.", watch);

            this.Health.Read(reader, TITLE + "Health.", watch);
            this.Ammo.Read(reader, TITLE + "Ammo.", watch);
            this.Reload.Read(reader, TITLE + "Reload.", watch);
            this.ROF.Read(reader, TITLE + "ROF.", watch);

            this.ID.Read(reader, TITLE + "ID.", watch);
            this.Armor.Read(reader, TITLE + "Armor.", watch);
            this.Mission.Read(reader, TITLE + "Mission.", watch);

            this.Target.Read(reader, TITLE + "Target.", watch);
            this.Dest.Read(reader, TITLE + "Dest.", watch);
            this.Location.Read(reader, TITLE + "Location.", watch);
            this.Cell.Read(reader, TITLE + "Cell.", watch);
            this.BodyDir.Read(reader, TITLE + "BodyDir.", watch);
            this.TurretDir.Read(reader, TITLE + "TurretDir.", watch);

            this.Enable = Duration.Mode != InfoMode.NONE
                        || Delay.Mode != InfoMode.NONE
                        || InitDelay.Mode != InfoMode.NONE
                        || Stack.Mode != InfoMode.NONE

                        || Health.Mode != InfoMode.NONE
                        || Ammo.Mode != InfoMode.NONE
                        || Reload.Mode != InfoMode.NONE
                        || ROF.Mode != InfoMode.NONE

                        || ID.Mode != InfoMode.NONE
                        || Armor.Mode != InfoMode.NONE
                        || Mission.Mode != InfoMode.NONE

                        || Target.Mode != InfoMode.NONE
                        || Dest.Mode != InfoMode.NONE
                        || Location.Mode != InfoMode.NONE
                        || Cell.Mode != InfoMode.NONE
                        || BodyDir.Mode != InfoMode.NONE
                        || TurretDir.Mode != InfoMode.NONE
                        ;
        }

    }


}
