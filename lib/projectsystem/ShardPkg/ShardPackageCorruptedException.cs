namespace vein.project.shards;

using System;

public class ShardPackageCorruptedException : Exception
{
    public ShardPackageCorruptedException(Exception e) : base("See inner exception", e)  { }
    public ShardPackageCorruptedException(string e) : base(e)  { }
}
