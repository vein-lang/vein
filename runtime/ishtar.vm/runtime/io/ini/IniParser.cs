namespace ishtar.runtime.io.ini;

using collections;
using gc;

public enum IniDataType : byte
{
    Number,
    String,
    Bool,
    Null,
    ArrayNumber,
    ArrayString,
    ArrayBool
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct IniValue_Union
{
    [FieldOffset(0)]
    public bool vb;
    [FieldOffset(0)]
    public long vl;
    [FieldOffset(0)]
    public SlicedString vs;
    [FieldOffset(0)]
    public IniArray array;
}

public unsafe struct IniArray
{
    public IniDataType type;
    public IniValue_Union* arr;
    public uint size;
}

public unsafe struct IniValue
{
    public IniDataType type;
    public IniValue_Union value;
}

public unsafe struct IniKeyValue
{
    public SlicedString key;
    public IniValue value;
}

public unsafe struct IniGroup
{
    public SlicedString groupKey;
    public IniKeyValue* kvs;
    public uint size;


    public bool GetFlag(string key)
    {
        if (kvs is null) return false;

        fixed (char* k = key)
        {
            var sliced = new SlicedString(k, (uint)key.Length);

            for (int i = 0; i < size; i++)
            {
                if (kvs[i].value.type != IniDataType.Bool)
                    continue;

                if (kvs[i].key.SlicedStringEquals(sliced))
                    return kvs[i].value.value.vb;
            }
        }

        return false;
    }

    public long GetInt(string key, long defaultValue)
    {
        if (kvs is null) return defaultValue;

        fixed (char* k = key)
        {
            var sliced = new SlicedString(k, (uint)key.Length);

            for (int i = 0; i < size; i++)
            {
                if (kvs[i].value.type != IniDataType.Number)
                    continue;

                if (kvs[i].key.SlicedStringEquals(sliced))
                    return kvs[i].value.value.vl;
            }
        }

        return defaultValue;
    }

    public SlicedString GetString(string key)
    {
        if (kvs is null) return default;
        fixed (char* k = key)
        {
            var sliced = new SlicedString(k, (uint)key.Length);

            for (int i = 0; i < size; i++)
            {
                if (kvs[i].value.type != IniDataType.String)
                    continue;

                if (kvs[i].key.SlicedStringEquals(sliced))
                    return kvs[i].value.value.vs;
            }
        }

        return default;
    }
}

public unsafe struct IniRoot
{
    public IniGroup* groups;
    public uint size;
    public char* _cache_sliced;


    public IniGroup GetGroup(string key)
    {
        fixed (char* k = key)
        {
            var sliced = new SlicedString(k, (uint)key.Length);

            for (int i = 0; i < size; i++)
            {
                if (groups[i].groupKey.SlicedStringEquals(sliced))
                    return groups[i];
            }
        }

        return default;
    }
}


public unsafe struct IniParser(SlicedString source, AllocatorBlock allocator)
{
    private uint _currentPosition = 0;

    public IniRoot* Parse()
    {
        var root = IshtarGC.AllocateImmortal<IniRoot>(null);

        *root = new IniRoot
        {
            size = 0,
            groups = IshtarGC.AllocateImmortal<IniGroup>(5, null)
        };

        while (_currentPosition < source.Size)
        {
            SkipWhitespaceAndNewlines();

            if (_currentPosition < source.Size && source.Ptr[_currentPosition] == '[')
            {
                if (root->size >= 5)
                    root->groups = allocator.realloc<IniGroup>(root->groups, root->size + 5);

                ParseSection(&root->groups[root->size]);
                root->size++;
            }
            else if (_currentPosition < source.Size && IsAlpha(source.Ptr[_currentPosition]))
            {
                if (root->size > 0)
                    ParseKeyValue(&root->groups[root->size - 1]);
            }
            else
                _currentPosition++;
        }

        return root;
    }

    private void ParseSection(IniGroup* group)
    {
        group->size = 0;
        group->kvs = IshtarGC.AllocateImmortal<IniKeyValue>(10, null);

        _currentPosition++; // Skip '['
        uint start = _currentPosition;
        while (_currentPosition < source.Size && source.Ptr[_currentPosition] != ']')
        {
            _currentPosition++;
        }
        group->groupKey = source.Slice(start, _currentPosition);
        _currentPosition++; // Skip ']'
    }

    private void ParseKeyValue(IniGroup* group)
    {
        var keyStart = _currentPosition;
        var isArray = false;

        while (_currentPosition < source.Size && source.Ptr[_currentPosition] != '=')
        {
            if (_currentPosition < source.Size - 2 && source.Ptr[_currentPosition] == '[' && source.Ptr[_currentPosition + 1] == ']')
            {
                isArray = true;
                break;
            }
            _currentPosition++;
        }

        var key = source.Slice(keyStart, _currentPosition);
        _currentPosition++; // Skip '='

        uint valueStart = _currentPosition;

        if (isArray)
            valueStart += 2;

        while (_currentPosition < source.Size && source.Ptr[_currentPosition] != '\n')
        {
            _currentPosition++;
        }
        IniKeyValue kv = FindOrCreateKeyValue(group, key, isArray);

        if (valueStart == _currentPosition)
        {
            kv.value.type = IniDataType.Null;
            kv.key = key;
            ref var g= ref group->kvs[group->size++];
            g.key = key;
            g.value = kv.value;
            return;
        }

        var value = source.Slice(valueStart, _currentPosition);


        if (isArray)
            AddValueToArray(ref kv, value);
        else
            kv.value = ParseValue(value);

        if (isArray || kv.key.Ptr != null)
            return;
        kv.key = key;
        ref var grp= ref group->kvs[group->size++];
        grp.key = key;
        grp.value = kv.value;
    }

    private IniKeyValue FindOrCreateKeyValue(IniGroup* group, SlicedString key, bool isArray)
    {
        for (uint i = 0; i < group->size; i++)
            if (group->kvs[i].key.SlicedStringEquals(key))
                return group->kvs[i];

        if (!isArray)
            return default;
        if (group->size >= 10)
            group->kvs = allocator.realloc<IniKeyValue>(group->kvs, group->size + 10);

        group->kvs[group->size].key = key;
        group->kvs[group->size].value.type = IniDataType.ArrayNumber;
        group->kvs[group->size].value.value.array = new IniArray()
        {
            arr = IshtarGC.AllocateImmortal<IniValue_Union>(10, null),
            size = 0,
            type = IniDataType.Number
        };
        group->size++;
        return group->kvs[group->size - 1];

    }

    private void AddValueToArray(ref IniKeyValue kv, SlicedString value)
    {
        if (kv.value.type != IniDataType.ArrayNumber &&
            kv.value.type != IniDataType.ArrayString &&
            kv.value.type != IniDataType.ArrayBool)
            throw new Exception("Key is not an array");

        var array = kv.value.value.array;
        
        if (array.size >= 10)
        {
            array.arr = allocator.realloc<IniValue_Union>(array.arr, array.size + 10);
        }

        var parsedValue = ParseValue(value);
        if (array.size == 0)
        {
            array.type = parsedValue.type;
        }
        else if (array.type != parsedValue.type)
            throw new Exception("Inconsistent array value types");
        if (parsedValue.type == IniDataType.Number)
            array.arr[array.size++].vl = parsedValue.value.vl;
        else if (parsedValue.type == IniDataType.Bool)
            array.arr[array.size++].vb = parsedValue.value.vb;
        else if (parsedValue.type == IniDataType.String)
            array.arr[array.size++].vs = parsedValue.value.vs;
        else
            throw new Exception("Not Supported type");
    }

    private IniValue ParseValue(SlicedString valueStr)
    {
        var iniValue = new IniValue();

        if (valueStr.Size == 0)
        {
            iniValue.type = IniDataType.Null;
        }
        else switch (valueStr.Ptr[0])
        {
            case '"':
                if (valueStr.Ptr[1] == '"')
                {
                    iniValue.type = IniDataType.String;
                    iniValue.value.vs = default;
                    break;
                }
                iniValue.type = IniDataType.String;
                iniValue.value.vs = new SlicedString(valueStr, 1, valueStr.Size - 1);
                break;
            case 't':
            case 'f':
                iniValue.type = IniDataType.Bool;
                iniValue.value.vb = valueStr.Ptr[0] == 't';
                break;
            default:
                iniValue.type = IniDataType.Number;
                iniValue.value.vl = ParseLong(valueStr);
                break;
        }
        return iniValue;
    }


    private long ParseLong(SlicedString valueStr)
    {
        long result = 0L;
        bool hasStarted = false; 

        for (uint i = 0U; i < valueStr.Size; i++)
        {
            char currentChar = valueStr.Ptr[i];

            if (currentChar is >= '0' and <= '9')
            {
                hasStarted = true;
                result = result * 10 + (currentChar - '0');
            }
            else if (hasStarted)
            {
                if (currentChar is ' ' or '\t')
                    continue;
                break;
            }
            else if (currentChar is ' ' or '\t')
                continue;
            else
                throw new Exception("Invalid character in number");
        }

        return result;
    }

    private void SkipWhitespaceAndNewlines()
    {
        while (_currentPosition < source.Size &&
               (source.Ptr[_currentPosition] == ' ' ||
                source.Ptr[_currentPosition] == '\n' ||
                source.Ptr[_currentPosition] == '\r'))
            _currentPosition++;
    }

    private bool IsAlpha(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
}
