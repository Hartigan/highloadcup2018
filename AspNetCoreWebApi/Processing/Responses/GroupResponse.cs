using System;
using System.Collections.Generic;
using AspNetCoreWebApi.Domain;

namespace AspNetCoreWebApi.Processing.Responses
{
    public class GroupEntry
    {
        public GroupEntry(Group group, int count)
        {
            Group = group;
            Count = count;
        }

        public Group Group { get; }
        public int Count { get; }
    }

    public class GroupResponse
    {
        public GroupResponse(IReadOnlyList<GroupEntry> entries)
        {
            Entries = entries;
        }

        public IReadOnlyList<GroupEntry> Entries { get; }
    }
}