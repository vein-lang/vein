namespace ishtar.jit;

using registers;

public static class _cpu
{
    private static _rip _rip;
    private static _seg[] _seg;
    private static _gp[] _gpbLo;
    private static _gp[] _gpbHi;
    private static _gp[] _gpw;
    private static _gp[] _gpd;
    private static _gp[] _gpq;
    private static _fp[] _fp;
    private static _mm[] _mm;
    private static _k[] _k;
    private static _xmm[] _xmm;
    private static _ymm[] _ymm;
    private static _zmm[] _zmm;


    static _cpu()
    {
        _rip = new _rip();
        _seg = new _seg[7].init_with(i => new _seg(i));
        _gpbLo = new _gp[16].init_with(i => new _gp(GP_REGISTER_TYPE.GpbLo, i));
        _gpbHi = new _gp[4].init_with(i => new _gp(GP_REGISTER_TYPE.GpbHi, i));
        _gpw = new _gp[16].init_with(i => new _gp(GP_REGISTER_TYPE.Gpw, i));
        _gpd = new _gp[16].init_with(i => new _gp(GP_REGISTER_TYPE.Gpd, i));
        _gpq = new _gp[16].init_with(i => new _gp(GP_REGISTER_TYPE.Gpq, i));
        _fp = new _fp[8].init_with(i => new _fp(i));
        _mm = new _mm[8].init_with(i => new _mm(i));
        _k = new _k[8].init_with(i => new _k(i));
        _xmm = new _xmm[32].init_with(i => new _xmm(i));
        _ymm = new _ymm[32].init_with(i => new _ymm(i));
        _zmm = new _zmm[32].init_with(i => new _zmm(i));
    }

    public static class registers
    {
        public static _rip RIP => _rip;

        public static _seg RS => _seg[1];

        public static _seg CS => _seg[2];

        public static _seg SS => _seg[3];

        public static _seg DS => _seg[4];

        public static _seg FS => _seg[5];

        public static _seg Gs => _seg[6];

        public static _gp AL => _gpbLo[0];

        public static _gp CL => _gpbLo[1];

        public static _gp DL => _gpbLo[2];

        public static _gp BL => _gpbLo[3];

        public static _gp SPL => _gpbLo[4];

        public static _gp BPL => _gpbLo[5];

        public static _gp SIL => _gpbLo[6];

        public static _gp DIL => _gpbLo[7];

        public static _gp R8B => _gpbLo[8];

        public static _gp R9B => _gpbLo[9];

        public static _gp R10B => _gpbLo[10];

        public static _gp R11B => _gpbLo[11];

        public static _gp R12B => _gpbLo[12];

        public static _gp R13B => _gpbLo[13];

        public static _gp R14B => _gpbLo[14];

        public static _gp R15B => _gpbLo[15];

        public static _gp AH => _gpbHi[0];

        public static _gp CH => _gpbHi[1];

        public static _gp DH => _gpbHi[2];

        public static _gp BH => _gpbHi[3];

        public static _gp AX => _gpw[0];

        public static _gp CX => _gpw[1];

        public static _gp DX => _gpw[2];

        public static _gp BX => _gpw[3];

        public static _gp SP => _gpw[4];

        public static _gp BP => _gpw[5];

        public static _gp SI => _gpw[6];

        public static _gp DI => _gpw[7];

        public static _gp R8W => _gpw[8];

        public static _gp R9W => _gpw[9];

        public static _gp R10W => _gpw[10];

        public static _gp R11W => _gpw[11];

        public static _gp R12W => _gpw[12];

        public static _gp R13W => _gpw[13];

        public static _gp R14W => _gpw[14];

        public static _gp R15W => _gpw[15];

        public static _gp EAX => _gpd[0];

        public static _gp ECX => _gpd[1];

        public static _gp EDX => _gpd[2];

        public static _gp EBX => _gpd[3];

        public static _gp ESP => _gpd[4];

        public static _gp EBP => _gpd[5];

        public static _gp ESI => _gpd[6];

        public static _gp EDI => _gpd[7];

        public static _gp R8D => _gpd[8];

        public static _gp R9D => _gpd[9];

        public static _gp R10D => _gpd[10];

        public static _gp R11D => _gpd[11];

        public static _gp R12D => _gpd[12];

        public static _gp R13D => _gpd[13];

        public static _gp R14D => _gpd[14];

        public static _gp R15D => _gpd[15];

        public static _gp RAX => _gpq[0];

        public static _gp RCX => _gpq[1];

        public static _gp RDX => _gpq[2];

        public static _gp RBX => _gpq[3];

        public static _gp RSP => _gpq[4];

        public static _gp RBP => _gpq[5];

        public static _gp RSI => _gpq[6];

        public static _gp RDI => _gpq[7];

        public static _gp R8 => _gpq[8];

        public static _gp R9 => _gpq[9];

        public static _gp R10 => _gpq[10];

        public static _gp R11 => _gpq[11];

        public static _gp R12 => _gpq[12];

        public static _gp R13 => _gpq[13];

        public static _gp R14 => _gpq[14];

        public static _gp R15 => _gpq[15];

        public static _fp FP0 => _fp[0];

        public static _fp FP1 => _fp[1];

        public static _fp FP2 => _fp[2];

        public static _fp FP3 => _fp[3];

        public static _fp FP4 => _fp[4];

        public static _fp FP5 => _fp[5];

        public static _fp FP6 => _fp[6];

        public static _fp FP7 => _fp[7];

        public static _mm MM0 => _mm[0];

        public static _mm MM1 => _mm[1];

        public static _mm MM2 => _mm[2];

        public static _mm MM3 => _mm[3];

        public static _mm MM4 => _mm[4];

        public static _mm MM5 => _mm[5];

        public static _mm MM6 => _mm[6];

        public static _mm MM7 => _mm[7];

        public static _k K0 => _k[0];

        public static _k K1 => _k[1];

        public static _k K2 => _k[2];

        public static _k K3 => _k[3];

        public static _k K4 => _k[4];

        public static _k K5 => _k[5];

        public static _k K6 => _k[6];

        public static _k K7 => _k[7];
    }
}
