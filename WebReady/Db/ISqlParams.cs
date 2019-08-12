using System;

namespace WebReady.Db
{
    /// <summary>
    /// To set SQL parameters.  
    /// </summary>
    public interface ISqlParams : ISink
    {
        ISqlParams SetNull();

        ISqlParams Set(bool v);

        ISqlParams Set(char v);

        ISqlParams Set(short v);

        ISqlParams Set(uint v);

        ISqlParams Set(int v);

        ISqlParams Set(long v);

        ISqlParams Set(double v);

        ISqlParams Set(decimal v);

        ISqlParams Set(JNumber v);

        ISqlParams Set(DateTime v);

        ISqlParams Set(string v);

        ISqlParams Set(ArraySegment<byte> v);

        ISqlParams Set(byte[] v);

        ISqlParams Set(short[] v);

        ISqlParams Set(int[] v);

        ISqlParams Set(long[] v);

        ISqlParams Set(string[] v);

        ISqlParams Set(JObj v);

        ISqlParams Set(JArr v);

        ISqlParams Set(IData v, byte proj = 0x0f);

        ISqlParams Set<D>(D[] v, byte proj = 0x0f) where D : IData;

        ISqlParams SetIn(string[] v);

        ISqlParams SetIn(short[] v);

        ISqlParams SetIn(int[] v);

        ISqlParams SetIn(long[] v);
    }
}