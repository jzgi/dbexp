using System;

namespace WebReady.Db
{
    /// <summary>
    /// To set SQL parameters.  
    /// </summary>
    public interface IDbParameters : ISink
    {
        IDbParameters SetNull();

        IDbParameters Set(bool v);

        IDbParameters Set(char v);

        IDbParameters Set(byte v);

        IDbParameters Set(short v);

        IDbParameters Set(int v);

        IDbParameters Set(long v);

        IDbParameters Set(uint v);

        IDbParameters Set(float v);

        IDbParameters Set(double v);

        IDbParameters Set(decimal v);

        IDbParameters Set(JNumber v);

        IDbParameters Set(DateTime v);

        IDbParameters Set(Guid v);

        IDbParameters Set(string v);

        IDbParameters Set(bool[] v);

        IDbParameters Set(char[] v);

        IDbParameters Set(byte[] v);

        IDbParameters Set(ArraySegment<byte> v);

        IDbParameters Set(short[] v);

        IDbParameters Set(int[] v);

        IDbParameters Set(long[] v);

        IDbParameters Set(uint[] v);

        IDbParameters Set(float[] v);

        IDbParameters Set(double[] v);

        IDbParameters Set(DateTime[] v);

        IDbParameters Set(Guid[] v);

        IDbParameters Set(string[] v);

        IDbParameters Set(JObj v);

        IDbParameters Set(JArr v);

        IDbParameters Set(IData v, byte proj = 0x0f);

        IDbParameters Set<D>(D[] v, byte proj = 0x0f) where D : IData;

        IDbParameters SetIn(string[] v);

        IDbParameters SetIn(short[] v);

        IDbParameters SetIn(int[] v);

        IDbParameters SetIn(long[] v);
    }
}