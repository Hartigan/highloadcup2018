using System;
using System.Collections.Generic;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Pooling;

namespace AspNetCoreWebApi.Processing.Responses
{
    public struct GroupEntry
    {
        public GroupEntry(Group group, int count)
        {
            Group = group;
            Count = count;
        }

        public Group Group { get; }
        public int Count { get; }
    }

    public class GroupResponse : IClearable
    {
        public GroupResponse()
        {
        }

        public List<GroupEntry> Entries { get; } = new List<GroupEntry>();

        public int Limit { get; set; }

        public void Clear()
        {
            Entries.Clear();
            Limit = 0;
        }
    }
}