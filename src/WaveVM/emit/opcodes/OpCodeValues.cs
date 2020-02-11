namespace wave.emit.opcodes
{
    public enum OpCodeValues : byte
    {
        [__ophandler(typeof(NOP))]
        nop = 0x0,
        [__ophandler(typeof(F_DROP))]
        drop = 0x1,
        entrypoint = 0x2,
        [__ophandler(typeof(F_LABEL))]
        label = 0x3,
        [__ophandler(typeof(F_MV))]
        mv = 0x4,

        /// <summary>
        /// Allocate space from the local memory pool
        /// </summary>
        localloc = 0x5,
        /// <summary>
        /// fixed local stack size (local max stack)
        /// </summary>
        mxsloc = 0x6,
        /// <summary>
        /// Pop a value from stack into local variable
        /// </summary>
        stloc = 0x7,
        /// <summary>
        /// Load local variable onto stack
        /// </summary>
        ldloc = 0x8,
        /// <summary>
        /// Load given int value into stack
        /// </summary>
        ldc_iX = 0x9,

        /// <summary>
        /// call signature label
        /// </summary>
        call = 0xA,

        [__ophandler(typeof(F_IDIV))]
        idiv = 0xA0,
        [__ophandler(typeof(F_IMUL))]
        imul = 0xA1,
        isub = 0xA2,
        iadd = 0xA3,

        fdiv = 0xB0,
        fmul = 0xB1,
        fsub = 0xB2,
        fadd = 0xB3
    }
}