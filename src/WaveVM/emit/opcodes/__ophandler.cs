namespace wave.emit.opcodes
{
    using System;
    [AttributeUsage(AttributeTargets.Field)]
    internal class __ophandler : Attribute
    {
        private readonly Type _typeOfOpCode;

        public __ophandler(Type typeOfOpCode) => _typeOfOpCode = typeOfOpCode;

        public Type GetOpCodeType() => _typeOfOpCode;
    }
}