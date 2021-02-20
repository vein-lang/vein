#pragma once
#include "compatibility.types.hpp"
#include "WaveImage.hpp"
#include "WaveMethodHeader.hpp"
#include "WaveMethodPInvokeInfo.hpp"
#include "WaveMethodSignature.hpp"
#include "collections/list_t.hpp"
#include "emit/MethodFlags.hpp"
#include "emit/WaveMember.hpp"

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
	unsigned char StackSize; 
	unsigned char LocalsSize; 

	union {
		MetaMethodHeader* header;
		WaveMethodPInvokeInfo* piinfo;
	} data;

    WaveMethod(const wstring& name,
		MethodFlags flags, WaveType* retType,
		WaveClass* owner, list_t<WaveArgumentRef*>* args) : WaveMethodBase(name, flags, args)
    {
        ReturnType = retType;
		Owner = owner;
    }
};
