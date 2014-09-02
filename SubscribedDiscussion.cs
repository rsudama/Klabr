using System;
using System.Collections.Generic;
using System.Reflection;

using Gemcom.Klabr.SPWebLib;

namespace Gemcom.Klabr
{
    public class SPDiscussion : SPDiscussion
    {
        public List<SPTopic> Topics { get; set; }

        public SPDiscussion()
        {
            this.Topics = new List<SPTopic>();
        }

        public SPDiscussion(SPDiscussion discussion) : this()
        {
            /*
            this.Communities = discussion.Communities;
            this.Created = discussion.Created;
            this.Description = discussion.Description;
            this.IsUnsubscribed = discussion.IsUnsubscribed;
            this.ItemCount = discussion.ItemCount;
            this.LastModified = discussion.LastModified;
            this.LastViewed = discussion.LastViewed;
            this.Moderators = discussion.Moderators;
            this.Site = discussion.Site;
            this.Title = discussion.Title;
            this.Topics = new List<SPTopic>();
            this.UniqueID = discussion.UniqueID;
            this.Url = discussion.Url;
            */

            foreach (PropertyInfo info in discussion.GetType().GetProperties())
            {
                info.SetValue(this, info.GetValue(discussion, null), null);
            }
        }
    }
}
