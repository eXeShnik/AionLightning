namespace AionLightning.Commons.Utils
{
    public class Rnd
    {
        private static readonly MTRandom RndGenerator = new MTRandom();

        public static float Get() => RndGenerator.NextFloat();

        public static int Get(int n) => RndGenerator.Next(n);

        public static int Get(int min, int max) => RndGenerator.Next(min, max);

        public static bool Chance(int chance)
        {
            return chance >= 1 && (chance > 99 || RndGenerator.Next(99) + 1 <= chance);
        }

        public static bool Chance(double chance) => RndGenerator.NextDouble() <= chance / 100.0;

        public static int NextInt(int n) => RndGenerator.Next(n);

        public static int NextInt() => RndGenerator.Next();

        public static double NextDouble() => RndGenerator.NextDouble();
    }
}
