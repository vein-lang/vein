#pragma once
#include <string>
#include <vector>
using namespace std;
struct TypeName
{
    string FullName;
};
enum ClassFlags
{
    None        = 0 << 0,
    Public      = 1 << 0,
    Static      = 1 << 1,
    Internal    = 1 << 2,
    Protected   = 1 << 3,
    Private     = 1 << 4
};
enum WaveTypeCode
{
    TYPE_NONE = 0x0,
    TYPE_VOID,
    TYPE_OBJECT,
    TYPE_BOOLEAN,
    TYPE_CHAR,
    TYPE_I1, /* sbyte  */
    TYPE_U1, /* byte   */
    TYPE_I2, /* short  */
    TYPE_U2, /* ushort */
    TYPE_I4, /* int32  */
    TYPE_U4, /* uint32 */
    TYPE_I8, /* long   */
    TYPE_U8, /* ulong  */
    TYPE_R4, /* float  */
    TYPE_R8, /* double */
    TYPE_R16, /* decimal */
    TYPE_STRING,
    TYPE_CLASS,
    TYPE_ARRAY
};
struct WaveMember
{
    
};
class WaveType
{
public:
    virtual string GetNamespace() = 0;
    vector<WaveMember> Members;
    WaveType* Parent;
    ClassFlags classFlags;
    WaveTypeCode TypeCode;

    virtual TypeName GetFullName() = 0;
};

/*
 *
 * public abstract class WaveType : WaveMember
    {
        public string Namespace => FullName.Namespace;
        public List<WaveMember> Members { get; } = new();
        protected internal ClassFlags? classFlags { get; set; }
        public WaveTypeCode TypeCode { get; protected set; }
        public WaveType Parent { get; protected set; }

        public override string Name
        {
            get => FullName.Name;
            protected set => throw new NotImplementedException();
        }


        public abstract TypeName FullName { get; }

        public virtual bool IsArray { get; protected set; } = false;
        public virtual bool IsSealed { get; protected set; } = false;
        public virtual bool IsClass { get; protected set; } = false;
        public virtual bool IsPublic { get; protected set; } = false;
        public virtual bool IsStatic { get; protected set; } = false;
        public virtual bool IsPrivate { get; protected set; } = false;
        public virtual bool IsPrimitive { get; protected set; } = false;
        
        public override WaveMemberKind Kind => WaveMemberKind.Type;


        public static implicit operator WaveType(string typeName) =>
            new WaveTypeImpl(typeName, WaveTypeCode.TYPE_CLASS);
        
        
        public static WaveType ByName(TypeName name) => 
            WaveCore.Types.All.FirstOrDefault(x => x.FullName == name);
        
        
        public override string ToString() => $"[{FullName}]";
    }
 */