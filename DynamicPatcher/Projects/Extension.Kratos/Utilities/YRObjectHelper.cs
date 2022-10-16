using System;
using System.Collections.Generic;
using System.Linq;
using DynamicPatcher;
using PatcherYRpp;
using PatcherYRpp.Utilities;
using Extension.Ext;
using Extension.INI;
using Extension.Script;
using Extension.Utilities;

namespace Extension.Utilities
{
    public static partial class ExHelper
    {

        public static CoordStruct ToCoordStruct(this BulletVelocity bulletVelocity)
        {
            return new CoordStruct(bulletVelocity.X, bulletVelocity.Y, bulletVelocity.Z);
        }

        public static CoordStruct ToCoordStruct(this SingleVector3D vector3D)
        {
            return new CoordStruct(vector3D.X, vector3D.Y, vector3D.Z);
        }

        public static BulletVelocity ToBulletVelocity(this CoordStruct coord)
        {
            return new BulletVelocity(coord.X, coord.Y, coord.Z);
        }

        public static BulletVelocity ToBulletVelocity(this SingleVector3D vector3D)
        {
            return new BulletVelocity(vector3D.X, vector3D.Y, vector3D.Z);
        }

        public static SingleVector3D ToSingleVector3D(this CoordStruct coord)
        {
            return new SingleVector3D(coord.X, coord.Y, coord.Z);
        }

        public static Point2D ToClientPos(this CoordStruct coord)
        {
            return TacticalClass.Instance.Ref.CoordsToClient(coord);
        }

        public static SingleVector3D ToSingleVector3D(this BulletVelocity bulletVelocity)
        {
            return new SingleVector3D(bulletVelocity.X, bulletVelocity.Y, bulletVelocity.Z);
        }

        public static ColorStruct ToColorAdd(this ColorStruct color)
        {
            int B = color.B >> 3;
            int G = color.G >> 2;
            int R = color.R >> 3;
            return new ColorStruct(R, G, B);
        }

        public static uint Add2RGB565(this ColorStruct colorAdd)
        {
            string R2 = Convert.ToString(colorAdd.R, 2).PadLeft(5, '0');
            string G2 = Convert.ToString(colorAdd.G, 2).PadLeft(6, '0');
            string B2 = Convert.ToString(colorAdd.B, 2).PadLeft(5, '0');
            string c2 = R2 + G2 + B2;
            return Convert.ToUInt32(c2, 2);
        }

        public static int GetRandomValue(this Point2D point, int defVal)
        {
            int min = point.X;
            int max = point.Y;
            if (min > max)
            {
                min = max;
                max = point.X;
            }
            if (max > 0)
            {
                return MathEx.Random.Next(min, max);
            }
            return defVal;
        }

    }

}