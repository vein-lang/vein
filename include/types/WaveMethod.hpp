#pragma once
#include "compatibility.types.hpp"
#include "WaveImage.hpp"
#include "WaveMethodHeader.hpp"
#include "WaveMethodPInvokeInfo.hpp"
#include "WaveMethodSignature.hpp"
#include "WaveType.hpp"
#include "collections/list_t.hpp"
#include "emit/MethodFlags.hpp"
#include "emit/WaveArgumentRef.hpp"
#include "emit/WaveMember.hpp"

struct WaveModule;
class WaveArgumentRef;
using namespace std;

class WaveMethodBase : public WaveMember
{
protected:
    WaveMethodBase(const wstring& name, MethodFlags flags, list_t<WaveArgumentRef*>* args)
    : WaveMember(name)
    {
		Flags = flags;
		Arguments = args;
    }
public:
	MethodFlags Flags;
	list_t<WaveArgumentRef*>* Arguments;
	
    int ArgLen() const noexcept
    {
	    if (Arguments == nullptr)
			return 0;
		return Arguments->size();
	}
	WaveMemberKind GetKind() override
	{
	    return WaveMemberKind::Method;
	}

	[[nodiscard]]
	virtual bool IsStatic() const noexcept
	{
	    return (static_cast<int>(Flags) & static_cast<int>(MethodStatic)) != 0;
	}

	[[nodiscard]]
	virtual bool IsPrivate() const noexcept
	{
	    return (static_cast<int>(Flags) & static_cast<int>(MethodPrivate)) != 0;
	}

    [[nodiscard]]
    virtual bool IsExtern() const noexcept
	{
	    return (static_cast<int>(Flags) & static_cast<int>(MethodExtern)) != 0;
	}
};

class WaveMethod : public WaveMethodBase
{
public:
	WaveType* ReturnType;
	WaveClass* Owner;
	unsigned char StackSize = 8; 
	unsigned char LocalsSize = 0; 

	union {
		MetaMethodHeader* header;
		WaveMethodPInvokeInfo* piinfo;
	} data;

	int vtable_offset;
	
    WaveMethod(const wstring& name,
		MethodFlags flags, WaveType* retType,
		WaveClass* owner, list_t<WaveArgumentRef*>* args) : WaveMethodBase(name, flags, args)
    {
        ReturnType = retType;
		Owner = owner;
    }

	void SetILCode(uint32_t* code)
    {
        if ((Flags & MethodExtern) != 0)
			throw MethodHasExternException();
		data.header = new MetaMethodHeader();
		data.header->code = &*code;
		data.header->code_size = sizeof(code) / sizeof(uint32_t);
    }

	void SetExternalLink(wpointer ref)
    {
        if ((Flags & MethodExtern) == 0)
			throw InvalidOperationException("Cannot set native reference, method is not extern.");
		data.piinfo = new WaveMethodPInvokeInfo();
		data.piinfo->addr = ref;
    }
};