
namespace GART.X3D
{
    public class MathHelper
    {
        public static readonly float PiOver2 = 1.570796f;

        public static float ToDegrees(float radians)
        {
            return (radians * 57.29578f);
        }

        public static float ToRadians(float degrees)
        {
            return (degrees * 0.01745329f);
        }

    }
}
