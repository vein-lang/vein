namespace vein.project.shards;

using System;

public class ShardPackageCorruptedException : Exception
{
    public ShardPackageCorruptedException(Exception e) : base("", e)
    {
        
    }
}
